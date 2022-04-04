//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2022 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Helpers;
using ChromeHtmlToPdfLib.Protocol;
using ChromeHtmlToPdfLib.Protocol.Network;
using ChromeHtmlToPdfLib.Settings;
using Microsoft.Extensions.Logging;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    ///     Handles all the communication tasks with Chrome remote dev tools
    /// </summary>
    /// <remarks>
    ///     See https://chromedevtools.github.io/devtools-protocol/
    /// </remarks>
    internal class Browser : IDisposable
    {
        #region Fields
        /// <summary>
        ///     Used to make the logging thread safe
        /// </summary>
        private readonly object _lock = new object();
        
        /// <summary>
        ///     When set then logging is written to this ILogger instance
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        ///     A connection to the browser (Chrome)
        /// </summary>
        private readonly Connection _browserConnection;

        /// <summary>
        ///     A connection to a page
        /// </summary>
        private readonly Connection _pageConnection;

        private string _instanceId;
        #endregion

        #region Properties
        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId
        {
            get => _instanceId;
            set
            {
                _instanceId = value;
                _browserConnection.InstanceId = value;
                _pageConnection.InstanceId = value;
            }
        }
        #endregion

        #region Constructor & destructor
        /// <summary>
        ///     Makes this object and sets the Chrome remote debugging url
        /// </summary>
        /// <param name="browser">The websocket to the browser</param>
        /// <param name="logger">When set then logging is written to this ILogger instance for all conversions at the Information log level</param>
        internal Browser(Uri browser, ILogger logger)
        {
            _logger = logger;

            // Open a websocket to the browser
            _browserConnection = new Connection(browser.ToString(), logger);
            _browserConnection.OnError += OnOnError;

            var message = new Message {Method = "Target.createTarget"};
            message.Parameters.Add("url", "about:blank");

            var result = _browserConnection.SendAsync(message).GetAwaiter().GetResult();
            var page = Protocol.Page.Page.FromJson(result);
            var pageUrl = $"{browser.Scheme}://{browser.Host}:{browser.Port}/devtools/page/{page.Result.TargetId}";

            // Open a websocket to the page
            _pageConnection = new Connection(pageUrl, logger);
            _pageConnection.OnError += OnOnError;
        }
        #endregion

        #region OnError
        private void OnOnError(object sender, string error)
        {
            WriteToLog($"An error occurred: '{error}'");
        }
        #endregion

        #region NavigateTo
        /// <summary>
        ///     Instructs Chrome to navigate to the given <paramref name="uri" />
        /// </summary>
        /// <param name="safeUrls">A list with URL's that are safe to load</param>
        /// <param name="useCache">When <c>true</c> then caching will be enabled</param>
        /// <param name="uri"></param>
        /// <param name="html"></param>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        ///     the method will raise an <see cref="ConversionTimedOutException"/> if the 
        ///     <see cref="CountdownTimer"/> reaches zero before finishing navigation</param>
        /// <param name="mediaLoadTimeout">When set a timeout will be started after the DomContentLoaded
        ///     event has fired. After a timeout the NavigateTo method will exit as if the page
        ///     has been completely loaded</param>
        /// <param name="urlBlacklist">A list with URL's that need to be blocked (use * as a wildcard)</param>
        /// <param name="logNetworkTraffic">When enabled network traffic is also logged</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer"/> reaches zero</exception>
        internal void NavigateTo(
            List<string> safeUrls,
            bool useCache,
            Uri uri = null,
            string html = null,
            CountdownTimer countdownTimer = null,
            int? mediaLoadTimeout = null,
            List<string> urlBlacklist = null,
            bool logNetworkTraffic = false)
        {
            var waitEvent = new ManualResetEvent(false);
            var mediaLoadTimeoutCancellationTokenSource = new CancellationTokenSource();
            var navigationError = string.Empty;
            var waitForNetworkIdle = false;
            var mediaTimeoutTaskSet = false;
            var absoluteUri = uri?.AbsoluteUri.Substring(0, uri.AbsoluteUri.LastIndexOf('/') + 1);

            #region Message handler
            var messageHandler = new EventHandler<string>(delegate(object sender, string data)
            {
                //System.IO.File.AppendAllText("d:\\logs.txt", $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff} - {data}{Environment.NewLine}");
                var message = Base.FromJson(data);

                switch (message.Method)
                {
                    case "Network.requestWillBeSent":
                        var requestWillBeSent = RequestWillBeSent.FromJson(data);
                        WriteToLog($"Request sent with request id '{requestWillBeSent.Params.RequestId}' " +
                                   $"for url '{requestWillBeSent.Params.Request.Url}' " +
                                   $"with method '{requestWillBeSent.Params.Request.Method}' " +
                                   $"and type '{requestWillBeSent.Params.Type}'");
                        break;

                    case "Network.dataReceived":
                        var dataReceived = DataReceived.FromJson(data);
                        WriteToLog($"Data received for request id '{dataReceived.Params.RequestId}' " +
                                   $"with length '{dataReceived.Params.DataLength}'");
                        break;

                    case "Network.responseReceived":
                        var responseReceived = ResponseReceived.FromJson(data);
                        var response = responseReceived.Params.Response;

                        var logMessage = $"{(response.FromDiskCache ? "Cached response" : "Response")} received for request id '{responseReceived.Params.RequestId}' and url '{response.Url}'";

                        if (!string.IsNullOrWhiteSpace(response.RemoteIpAddress))
                            logMessage += $" from ip '{response.RemoteIpAddress}' on port '{response.RemotePort}' with status '{response.Status}{(!string.IsNullOrWhiteSpace(response.StatusText) ? $" ({response.StatusText})" : string.Empty)}'";
                        WriteToLog(logMessage);
                        break;

                    case "Network.loadingFinished":
                        var loadingFinished = LoadingFinished.FromJson(data);
                        WriteToLog($"Loading finished for request id '{loadingFinished.Params.RequestId}' " +
                                   $"{(loadingFinished.Params.EncodedDataLength > 0 ? $"with encoded data length '{loadingFinished.Params.EncodedDataLength}'" : string.Empty)}");
                        break;

                    case "Network.loadingFailed":
                        var loadingFailed = LoadingFailed.FromJson(data);
                        WriteToLog($"Loading failed for request id '{loadingFailed.Params.RequestId}' " +
                                   $"and type '{loadingFailed.Params.Type}' " +
                                   $"with error '{loadingFailed.Params.ErrorText}'");
                        break;

                    case "Network.requestServedFromCache":
                        var requestServedFromCache = RequestServedFromCache.FromJson(data);
                        WriteToLog($"The request with id '{requestServedFromCache.Params.RequestId}' is served from cache");
                        break;

                    case "Fetch.requestPaused":
                    {
                        var fetch = Fetch.FromJson(data);
                        var requestId = fetch.Params.RequestId;
                        var url = fetch.Params.Request.Url;
                        var isSafeUrl = safeUrls.Contains(url);
                        var isAbsoluteFileUri = absoluteUri != null &&
                                                url.StartsWith("file://", StringComparison.InvariantCultureIgnoreCase) &&
                                                url.StartsWith(absoluteUri, StringComparison.InvariantCultureIgnoreCase);

                        if (!RegularExpression.IsRegExMatch(urlBlacklist, url, out var matchedPattern) || isAbsoluteFileUri || isSafeUrl)
                        {
                            if (isSafeUrl)
                                WriteToLog($"The url '{url}' has been allowed because it is on the safe url list");
                            else if (isAbsoluteFileUri)
                                WriteToLog($"The file url '{url}' has been allowed because it start with the absolute uri '{absoluteUri}'");
                            else
                                WriteToLog($"The url '{url}' has been allowed because it did not match anything on the url blacklist");

                            var fetchContinue = new Message {Method = "Fetch.continueRequest"};
                            fetchContinue.Parameters.Add("requestId", requestId);
                            _pageConnection.Send(fetchContinue);
                        }
                        else
                        {
                            WriteToLog($"The url '{url}' has been blocked by url blacklist pattern '{matchedPattern}'");

                            var fetchFail = new Message {Method = "Fetch.failRequest"};
                            fetchFail.Parameters.Add("requestId", requestId);

                            // Failed, Aborted, TimedOut, AccessDenied, ConnectionClosed, ConnectionReset, ConnectionRefused,
                            // ConnectionAborted, ConnectionFailed, NameNotResolved, InternetDisconnected, AddressUnreachable,
                            // BlockedByClient, BlockedByResponse
                            fetchFail.Parameters.Add("errorReason", "BlockedByClient");
                            _pageConnection.Send(fetchFail);
                        }

                        break;
                    }

                    default:
                    {
                        var page = Protocol.Page.Event.FromJson(data);

                        switch (page.Method)
                        {
                            // The DOMContentLoaded event is fired when the document has been completely loaded and parsed, without
                            // waiting for stylesheets, images, and sub frames to finish loading (the load event can be used to
                            // detect a fully-loaded page).
                            case "Page.lifecycleEvent" when page.Params?.Name == "DOMContentLoaded":
                                
                                WriteToLog("The 'Page.lifecycleEvent' with param name 'DomContentLoaded' has been fired, the dom content is now loaded and parsed, waiting for stylesheets, images and sub frames to finish loading");
                                
                                if (mediaLoadTimeout.HasValue && !mediaTimeoutTaskSet)
                                {
                                    try
                                    {
                                        WriteToLog($"Media load timeout has a value of {mediaLoadTimeout.Value} milliseconds, setting media load timeout task");

                                        Task.Run(async delegate
                                        {
                                            await Task.Delay(mediaLoadTimeout.Value, mediaLoadTimeoutCancellationTokenSource.Token);
                                            WriteToLog($"Media load timeout task timed out after {mediaLoadTimeout.Value} milliseconds");
                                            waitEvent?.Set();
                                        }, mediaLoadTimeoutCancellationTokenSource.Token);

                                        mediaTimeoutTaskSet = true;
                                    }
                                    catch 
                                    {
                                        // Ignore
                                    }
                                }

                                break;

                            case "Page.frameNavigated":
                                WriteToLog("The 'Page.frameNavigated' event has been fired, waiting for the 'Page.lifecycleEvent' with name 'networkIdle'");
                                waitForNetworkIdle = true;
                                break;

                            case "Page.lifecycleEvent" when page.Params?.Name == "networkIdle" && waitForNetworkIdle:
                                WriteToLog("The 'Page.lifecycleEvent' event with name 'networkIdle' has been fired, the page is now fully loaded");
                                waitEvent?.Set();
                                break;

                            default:
                                var pageNavigateResponse = Protocol.Page.NavigateResponse.FromJson(data);
                                if (!string.IsNullOrEmpty(pageNavigateResponse.Result?.ErrorText) &&
                                    !pageNavigateResponse.Result.ErrorText.Contains("net::ERR_BLOCKED_BY_CLIENT"))
                                {
                                    navigationError = $"{pageNavigateResponse.Result.ErrorText} occurred when navigating to the page '{uri}'";
                                    waitEvent?.Set();
                                }

                                break;
                        }

                        break;
                    }
                }
            });
            #endregion

            if (logNetworkTraffic)
            {
                WriteToLog("Enabling network traffic logging");
                var networkMessage = new Message {Method = "Network.enable"};
                _pageConnection.SendAsync(networkMessage).GetAwaiter().GetResult();
            }

            WriteToLog(useCache ? "Enabling caching" : "Disabling caching");

            var cacheMessage = new Message {Method = "Network.setCacheDisabled"};
            cacheMessage.Parameters.Add("cacheDisabled", !useCache);
            _pageConnection.SendAsync(cacheMessage).GetAwaiter().GetResult();

            // Enables issuing of requestPaused events. A request will be paused until client calls one of failRequest, fulfillRequest or continueRequest/continueWithAuth
            if (urlBlacklist?.Count > 0)
            {
                WriteToLog("Enabling Fetch to block url's that are in the url blacklist'");
                _pageConnection.SendAsync(new Message {Method = "Fetch.enable"}).GetAwaiter().GetResult();
            }

            // Enables page domain notifications
            _pageConnection.SendAsync(new Message {Method = "Page.enable"}).GetAwaiter().GetResult();

            var lifecycleEventEnabledMessage = new Message {Method = "Page.setLifecycleEventsEnabled"};
            lifecycleEventEnabledMessage.AddParameter("enabled", true);
            _pageConnection.SendAsync(lifecycleEventEnabledMessage).GetAwaiter().GetResult();

            _pageConnection.MessageReceived += messageHandler;
            _pageConnection.Closed += (sender, args) => waitEvent?.Set();

            if (uri != null)
            {
                // Navigates current page to the given URL
                var pageNavigateMessage = new Message { Method = "Page.navigate" };
                pageNavigateMessage.AddParameter("url", uri.ToString());
                _pageConnection.Send(pageNavigateMessage);
            }
            else if (!string.IsNullOrWhiteSpace(html))
            {
                WriteToLog("Getting page frame tree");
                var pageGetFrameTree = new Message { Method = "Page.getFrameTree" };
                var frameTree = _pageConnection.SendAsync(pageGetFrameTree).GetAwaiter().GetResult();
                var frameResult = Protocol.Page.FrameTree.FromJson(frameTree);

                WriteToLog("Setting document content");

                var pageSetDocumentContent = new Message { Method = "Page.setDocumentContent" };
                pageSetDocumentContent.AddParameter("frameId", frameResult.Result.FrameTree.Frame.Id);
                pageSetDocumentContent.AddParameter("html", html);
                _pageConnection.SendAsync(pageSetDocumentContent).GetAwaiter().GetResult();
                // When using setDocumentContent a Page.frameNavigated event is never fired so we have to set the waitForNetworkIdle to true our self
                waitForNetworkIdle = true;

                WriteToLog("Document content set");
            }
            else
                throw new ArgumentException("Uri and html are both null");

            if (countdownTimer != null)
            {
                waitEvent.WaitOne(countdownTimer.MillisecondsLeft);
                if (countdownTimer.MillisecondsLeft == 0)
                    throw new ConversionTimedOutException($"The {nameof(NavigateTo)} method timed out");
            }
            else
                waitEvent.WaitOne();

            if (mediaTimeoutTaskSet)
            {
                mediaLoadTimeoutCancellationTokenSource.Cancel();
                mediaLoadTimeoutCancellationTokenSource.Dispose();
            }

            var lifecycleEventDisabledMessage = new Message {Method = "Page.setLifecycleEventsEnabled"};
            lifecycleEventDisabledMessage.AddParameter("enabled", false);

            // Disables page domain notifications
            _pageConnection.SendAsync(lifecycleEventDisabledMessage).GetAwaiter().GetResult();
            _pageConnection.SendAsync(new Message {Method = "Page.disable"}).GetAwaiter().GetResult();

            // Disables the fetch domain
            if (urlBlacklist?.Count > 0)
            {
                WriteToLog("Disabling Fetch");
                _pageConnection.SendAsync(new Message {Method = "Fetch.disable"}).GetAwaiter().GetResult();
            }

            if (logNetworkTraffic)
            {
                WriteToLog("Disabling network traffic logging");
                var networkMessage = new Message {Method = "Network.disable"};
                _pageConnection.SendAsync(networkMessage).GetAwaiter().GetResult();
            }

            _pageConnection.MessageReceived -= messageHandler;

            waitEvent.Dispose();
            waitEvent = null;

            if (!string.IsNullOrEmpty(navigationError))
            {
                WriteToLog(navigationError);
                throw new ChromeNavigationException(navigationError);
            }
        }
        #endregion

        #region WaitForWindowStatus
        /// <summary>
        ///     Wait until the javascript window.status is returning the given <paramref name="status" />
        /// </summary>
        /// <param name="status">The case insensitive status</param>
        /// <param name="timeout">Continue after reaching the set timeout in milliseconds</param>
        /// <returns><c>true</c> when window status matched, <c>false</c> when timing out</returns>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public bool WaitForWindowStatus(string status, int timeout = 60000)
        {
            var message = new Message {Method = "Runtime.evaluate"};
            message.AddParameter("expression", "window.status;");
            message.AddParameter("silent", true);
            message.AddParameter("returnByValue", true);

            var waitEvent = new ManualResetEvent(false);
            var match = false;

            void MessageReceived(object sender, string data)
            {
                var evaluate = Evaluate.FromJson(data);
                if (evaluate.Result?.Result?.Value != status) return;
                match = true;
                waitEvent.Set();
            }

            _pageConnection.MessageReceived += MessageReceived;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (!match)
            {
                _pageConnection.Send(message);
                waitEvent.WaitOne(10);
                if (stopWatch.ElapsedMilliseconds >= timeout) break;
            }

            stopWatch.Stop();
            _pageConnection.MessageReceived -= MessageReceived;

            return match;
        }
        #endregion
        
        #region RunJavascript
        /// <summary>
        ///     Runs the given javascript after the page has been fully loaded
        /// </summary>
        /// <param name="script">The javascript to run</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public void RunJavascript(string script)
        {
            var message = new Message {Method = "Runtime.evaluate"};
            message.AddParameter("expression", script);
            message.AddParameter("silent", false);
            message.AddParameter("returnByValue", false);

            var errorDescription = string.Empty;
            var result = _pageConnection.SendAsync(message).GetAwaiter().GetResult();
            var evaluateError = EvaluateError.FromJson(result);

            if (evaluateError.Result?.ExceptionDetails != null)
                errorDescription = evaluateError.Result.ExceptionDetails.Exception.Description;

            if (!string.IsNullOrEmpty(errorDescription))
                throw new ChromeException(errorDescription);
        }
        #endregion

        #region CaptureSnapshot
        /// <summary>
        ///     Instructs Chrome to capture a snapshot from the loaded page
        /// </summary>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        /// the method will raise an <see cref="ConversionTimedOutException"/> in the 
        /// <see cref="CountdownTimer"/> reaches zero before finishing the printing to pdf</param>        
        /// <remarks>
        ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page#method-captureSnapshot
        /// </remarks>
        /// <returns></returns>
        internal async Task<SnapshotResponse> CaptureSnapshot(CountdownTimer countdownTimer = null)
        {
            var message = new Message { Method = "Page.captureSnapshot" };

            var result = countdownTimer == null
                ? await _pageConnection.SendAsync(message)
                : await _pageConnection.SendAsync(message).Timeout(countdownTimer.MillisecondsLeft);

            return SnapshotResponse.FromJson(result);
        }
        #endregion

        #region PrintToPdf
        /// <summary>
        ///     Instructs Chrome to print the page
        /// </summary>
        /// <param name="pageSettings"><see cref="PageSettings" /></param>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        /// the method will raise an <see cref="ConversionTimedOutException"/> in the 
        /// <see cref="CountdownTimer"/> reaches zero before finishing the printing to pdf</param>        
        /// <remarks>
        ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF
        /// </remarks>
        /// <exception cref="ConversionException">Raised when Chrome returns an empty string</exception>
        /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer"/> reaches zero</exception>
        internal async Task<PrintToPdfResponse> PrintToPdf(
            PageSettings pageSettings, 
            CountdownTimer countdownTimer = null)
        {
            var message = new Message {Method = "Page.printToPDF"};
            message.AddParameter("landscape", pageSettings.Landscape);
            message.AddParameter("displayHeaderFooter", pageSettings.DisplayHeaderFooter);
            message.AddParameter("printBackground", pageSettings.PrintBackground);
            message.AddParameter("scale", pageSettings.Scale);
            message.AddParameter("paperWidth", pageSettings.PaperWidth);
            message.AddParameter("paperHeight", pageSettings.PaperHeight);
            message.AddParameter("marginTop", pageSettings.MarginTop);
            message.AddParameter("marginBottom", pageSettings.MarginBottom);
            message.AddParameter("marginLeft", pageSettings.MarginLeft);
            message.AddParameter("marginRight", pageSettings.MarginRight);
            message.AddParameter("pageRanges", pageSettings.PageRanges ?? string.Empty);
            message.AddParameter("ignoreInvalidPageRanges", pageSettings.IgnoreInvalidPageRanges);
            if (!string.IsNullOrEmpty(pageSettings.HeaderTemplate))
                message.AddParameter("headerTemplate", pageSettings.HeaderTemplate);
            if (!string.IsNullOrEmpty(pageSettings.FooterTemplate))
                message.AddParameter("footerTemplate", pageSettings.FooterTemplate);
            message.AddParameter("preferCSSPageSize", pageSettings.PreferCSSPageSize);

            var result = countdownTimer == null
                ? await _pageConnection.SendAsync(message)
                : await _pageConnection.SendAsync(message).Timeout(countdownTimer.MillisecondsLeft);

            var printToPdfResponse = PrintToPdfResponse.FromJson(result);

            if (string.IsNullOrEmpty(printToPdfResponse.Result?.Data))
                throw new ConversionException("Conversion failed");

            return printToPdfResponse;
        }
        #endregion

        #region CaptureScreenshot
        /// <summary>
        ///     Instructs Chrome to take a screenshot of the page
        /// </summary>
        /// <exception cref="ConversionException">Raised when Chrome returns an empty string</exception>
        /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer"/> reaches zero</exception>
        internal async Task<CaptureScreenshotResponse> CaptureScreenshot(CountdownTimer countdownTimer = null)
        {
            var message = new Message { Method = "Page.captureScreenshot" };
            var result = countdownTimer == null
                ? await _pageConnection.SendAsync(message)
                : await _pageConnection.SendAsync(message).Timeout(countdownTimer.MillisecondsLeft);

            var captureScreenshotResponse = CaptureScreenshotResponse.FromJson(result);

            if (string.IsNullOrEmpty(captureScreenshotResponse.Result?.Data))
                throw new ConversionException("Screenshot capture failed");

            return captureScreenshotResponse;
        }
        #endregion

        #region Close
        /// <summary>
        ///     Instructs Chrome to close
        /// </summary>
        public void Close()
        {
            var message = new Message {Method = "Browser.close"};
            _browserConnection.SendAsync(message).GetAwaiter().GetResult();
        }
        #endregion

        #region WriteToLog
        /// <summary>
        ///     Writes a line to the <see cref="_logger" />
        /// </summary>
        /// <param name="message">The message to write</param>
        internal void WriteToLog(string message)
        {
            lock (_lock)
            {
                try
                {
                    if (_logger == null) return;
                    using (_logger.BeginScope(InstanceId))
                        _logger.LogInformation(message);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore
                }
            }
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _pageConnection.OnError -= OnOnError;
            _pageConnection?.Dispose();

            _browserConnection.OnError -= OnOnError;
            _browserConnection?.Dispose();
        }
        #endregion
    }
}