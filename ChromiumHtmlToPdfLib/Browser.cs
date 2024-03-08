//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2024 Magic-Sessions. (www.magic-sessions.com)
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ChromiumHtmlToPdfLib.Exceptions;
using ChromiumHtmlToPdfLib.Helpers;
using ChromiumHtmlToPdfLib.Loggers;
using ChromiumHtmlToPdfLib.Protocol;
using ChromiumHtmlToPdfLib.Protocol.Network;
using ChromiumHtmlToPdfLib.Protocol.Page;
using ChromiumHtmlToPdfLib.Settings;
using Base = ChromiumHtmlToPdfLib.Protocol.Network.Base;
using Stream = System.IO.Stream;

// ReSharper disable UnusedMember.Global
// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace ChromiumHtmlToPdfLib;

/// <summary>
///     Handles all the communication tasks with Chromium remote dev tools
/// </summary>
/// <remarks>
///     See https://chromedevtools.github.io/devtools-protocol/
/// </remarks>
#if (NETSTANDARD2_0)
public class Browser : IDisposable
#else
internal class Browser : IDisposable, IAsyncDisposable
#endif
{
    #region Fields
    /// <summary>
    ///     A connection to the browser (Chrome or Edge)
    /// </summary>
    private Connection _browserConnection;

    /// <summary>
    ///     A connection to a page
    /// </summary>
    private Connection _pageConnection;

    /// <summary>
    ///     <see cref="Logger"/>
    /// </summary>
    private readonly Logger _logger;

    /// <summary>
    ///     Keeps track is we already disposed our resources
    /// </summary>
    private bool _disposed;
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets the Chromium remote debugging url
    /// </summary>
    /// <param name="browser">The websocket to the browser</param>
    /// <param name="timeout">Websocket open timeout in milliseconds</param>
    /// <param name="logger"><see cref="Logger"/></param>
    internal Browser(Uri browser, int timeout, Logger logger)
    {
        _logger = logger;
        // Open a websocket to the browser
        _browserConnection = new Connection(browser.ToString(), timeout, logger);

        var message = new Message { Method = "Target.createTarget" };
        message.Parameters.Add("url", "about:blank");

        var result = _browserConnection.SendForResponseAsync(message).GetAwaiter().GetResult();
        var page = Page.FromJson(result);
        var pageUrl = $"{browser.Scheme}://{browser.Host}:{browser.Port}/devtools/page/{page.Result.TargetId}";

        // Open a websocket to the page
        _pageConnection = new Connection(pageUrl, timeout, logger);
    }
    #endregion

    #region NavigateToAsync
    #region Enum PageLoadingState
    /// <summary>
    ///     An enum to keep track of the page loading state
    /// </summary>
    private enum PageLoadingState
    {
        /// <summary>
        ///     The page is loading
        /// </summary>
        Loading,
        
        /// <summary>
        ///     Waiting for the network to be idle
        /// </summary>
        WaitForNetworkIdle,

        /// <summary>
        ///     The loading of the media on the page timed out
        /// </summary>
        MediaLoadTimeout,

        /// <summary>
        ///     The page is blocked
        /// </summary>
        BlockedByClient,

        /// <summary>
        ///     The page is closed
        /// </summary>
        Closed,

        /// <summary>
        ///     The page finished loading normally
        /// </summary>
        Done
    }
    #endregion

    /// <summary>
    ///     Instructs Chromium to navigate to the given <paramref name="uri" />
    /// </summary>
    /// <param name="safeUrls">A list with URL's that are safe to load</param>
    /// <param name="useCache">When <c>true</c> then caching will be enabled</param>
    /// <param name="uri"></param>
    /// <param name="html"></param>
    /// <param name="countdownTimer">
    ///     If a <see cref="CountdownTimer" /> is set then
    ///     the method will raise an <see cref="ConversionTimedOutException" /> if the
    ///     <see cref="CountdownTimer" /> reaches zero before finishing navigation
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page
    ///     has been completely loaded
    /// </param>
    /// <param name="urlBlacklist">A list with URL's that need to be blocked (use * as a wildcard)</param>
    /// <param name="logNetworkTraffic">When enabled network traffic is also logged</param>
    /// <param name="waitForNetworkIdle">When enabled the method will wait for the network to be idle</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer" /> reaches zero</exception>
    internal async Task NavigateToAsync(
        List<string> safeUrls,
        bool useCache,
        ConvertUri uri = null,
        string html = null,
        CountdownTimer countdownTimer = null,
        int? mediaLoadTimeout = null,
        List<string> urlBlacklist = null,
        bool logNetworkTraffic = false,
        bool waitForNetworkIdle = false,
        CancellationToken cancellationToken = default)
    {
        var navigationError = string.Empty;
        var absoluteUri = uri?.AbsoluteUri.Substring(0, uri.AbsoluteUri.LastIndexOf('/') + 1);

        if (uri?.RequestHeaders?.Count > 0)
        {
            _logger?.WriteToLog("Setting request headers");
            var networkMessage = new Message { Method = "Network.setExtraHTTPHeaders" };
            networkMessage.AddParameter("headers", uri.RequestHeaders);
            await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
        }

        if (logNetworkTraffic)
        {
            _logger?.WriteToLog("Enabling network traffic logging");
            var networkMessage = new Message { Method = "Network.enable" };
            await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
        }

        _logger?.WriteToLog(useCache ? "Enabling caching" : "Disabling caching");

        var cacheMessage = new Message { Method = "Network.setCacheDisabled" };
        cacheMessage.Parameters.Add("cacheDisabled", !useCache);
        await _pageConnection.SendForResponseAsync(cacheMessage, cancellationToken).ConfigureAwait(false);

        // Enables issuing of requestPaused events. A request will be paused until client calls one of failRequest, fulfillRequest or continueRequest/continueWithAuth
        if (urlBlacklist?.Count > 0)
        {
            _logger?.WriteToLog("Enabling Fetch to block url's that are in the url blacklist");
            await _pageConnection.SendForResponseAsync(new Message { Method = "Fetch.enable" }, cancellationToken).ConfigureAwait(false);
        }

        // Enables page domain notifications
        _logger?.WriteToLog("Enabling page domain notifications");
        await _pageConnection.SendForResponseAsync(new Message { Method = "Page.enable" }, cancellationToken).ConfigureAwait(false);

        var lifecycleEventEnabledMessage = new Message { Method = "Page.setLifecycleEventsEnabled" };
        lifecycleEventEnabledMessage.AddParameter("enabled", true);
        _logger?.WriteToLog("Enabling page lifecycle events");
        await _pageConnection.SendForResponseAsync(lifecycleEventEnabledMessage, cancellationToken).ConfigureAwait(false);

        var waitForMessage = new SemaphoreSlim(1);
        var messagePump = new ConcurrentQueue<string>();
        var messageReceived = new EventHandler<string>(delegate(object _, string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return;
            messagePump.Enqueue(data);
            waitForMessage.Release();
        });

        _pageConnection.MessageReceived += messageReceived;
        _pageConnection.Closed += PageConnectionClosed;
        var pageLoadingState = PageLoadingState.Loading;

        try
        {
            if (uri != null)
            {
                // Navigates current page to the given URL
                var pageNavigateMessage = new Message { Method = "Page.navigate" };
                pageNavigateMessage.AddParameter("url", uri.ToString());
                _logger?.WriteToLog($"Navigating to url '{uri}'");
                await _pageConnection.SendAsync(pageNavigateMessage).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(html))
            {
                _logger?.WriteToLog("Getting page frame tree");
                var pageGetFrameTree = new Message { Method = "Page.getFrameTree" };
                var frameTree = await _pageConnection.SendForResponseAsync(pageGetFrameTree, cancellationToken).ConfigureAwait(false);
                var frameResult = FrameTree.FromJson(frameTree);

                _logger?.WriteToLog("Setting document content");

                var pageSetDocumentContent = new Message { Method = "Page.setDocumentContent" };
                pageSetDocumentContent.AddParameter("frameId", frameResult.Result.FrameTree.Frame.Id);
                pageSetDocumentContent.AddParameter("html", html);
                await _pageConnection.SendAsync(pageSetDocumentContent).ConfigureAwait(false);
                // When using setDocumentContent a Page.frameNavigated event is never fired so we have to set the waitForNetworkIdle to true our self
                pageLoadingState = PageLoadingState.WaitForNetworkIdle;
                _logger?.WriteToLog("Document content set");
            }
            else
                throw new ArgumentException("Uri and html are both null");

            var mediaLoadTimeoutStopwatch = new Stopwatch();

            while (pageLoadingState != PageLoadingState.MediaLoadTimeout && 
                   pageLoadingState != PageLoadingState.BlockedByClient &&
                   // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                   pageLoadingState != PageLoadingState.Closed &&
                   pageLoadingState != PageLoadingState.Done &&
                   !cancellationToken.IsCancellationRequested)
            {
                if (!messagePump.TryDequeue(out var data))
                {
                    await waitForMessage.WaitAsync(100, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                //System.IO.File.AppendAllText("d:\\logs.txt", $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff} - {data}{Environment.NewLine}");
                var message = Base.FromJson(data);

                switch (message.Method)
                {
                    case "Network.requestWillBeSent":
                        var requestWillBeSent = RequestWillBeSent.FromJson(data);
                        _logger?.WriteToLog($"Request sent with request id {requestWillBeSent.Params.RequestId} for url '{requestWillBeSent.Params.Request.Url}' with method {requestWillBeSent.Params.Request.Method} and type {requestWillBeSent.Params.Type}");
                        break;

                    case "Network.dataReceived":
                        var dataReceived = DataReceived.FromJson(data);
                        _logger?.WriteToLog($"Data received for request id {dataReceived.Params.RequestId} with a length of {FileManager.GetFileSizeString(dataReceived.Params.DataLength, CultureInfo.InvariantCulture)}");
                        break;

                    case "Network.responseReceived":
                        var responseReceived = ResponseReceived.FromJson(data);
                        var response = responseReceived.Params.Response;

                        var logMessage = $"{(response.FromDiskCache ? "Cached response" : "Response")} received for request id {responseReceived.Params.RequestId} and url '{response.Url}'";

                        if (!string.IsNullOrWhiteSpace(response.RemoteIpAddress))
                            logMessage += $" from ip {response.RemoteIpAddress} on port {response.RemotePort} with status {response.Status}{(!string.IsNullOrWhiteSpace(response.StatusText) ? $" and response text '{response.StatusText}'" : string.Empty)}";

                        _logger?.WriteToLog(logMessage);
                        break;

                    case "Network.loadingFinished":
                        var loadingFinished = LoadingFinished.FromJson(data);
                        _logger?.WriteToLog($"Loading finished for request id {loadingFinished.Params.RequestId} " +
                                             $"{(loadingFinished.Params.EncodedDataLength > 0 ? $"with an encoded data length of {FileManager.GetFileSizeString(loadingFinished.Params.EncodedDataLength, CultureInfo.InvariantCulture)}" : string.Empty)}");
                        break;

                    case "Network.loadingFailed":
                        var loadingFailed = LoadingFailed.FromJson(data);
                        _logger?.WriteToLog($"Loading failed for request id {loadingFailed.Params.RequestId} " +
                                             $"and type {loadingFailed.Params.Type} " +
                                             $"with error '{loadingFailed.Params.ErrorText}'");
                        break;

                    case "Network.requestServedFromCache":
                        var requestServedFromCache = RequestServedFromCache.FromJson(data);
                        _logger?.WriteToLog($"The request with id {requestServedFromCache.Params.RequestId} is served from cache");
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

                        if (!RegularExpression.IsRegExMatch(urlBlacklist, url, out var matchedPattern) ||
                            isAbsoluteFileUri || isSafeUrl)
                        {
                            if (isSafeUrl)
                                _logger?.WriteToLog($"The url '{url}' has been allowed because it is on the safe url list");
                            else if (isAbsoluteFileUri)
                                _logger?.WriteToLog($"The file url '{url}' has been allowed because it start with the absolute uri '{absoluteUri}'");
                            else
                                _logger?.WriteToLog($"The url '{url}' has been allowed because it did not match anything on the url blacklist");

                            var fetchContinue = new Message { Method = "Fetch.continueRequest" };
                            fetchContinue.Parameters.Add("requestId", requestId);
                            await _pageConnection.SendForResponseAsync(fetchContinue, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger?.WriteToLog($"The url '{url}' has been blocked by url blacklist pattern '{matchedPattern}'");

                            var fetchFail = new Message { Method = "Fetch.failRequest" };
                            fetchFail.Parameters.Add("requestId", requestId);

                            // Failed, Aborted, TimedOut, AccessDenied, ConnectionClosed, ConnectionReset, ConnectionRefused,
                            // ConnectionAborted, ConnectionFailed, NameNotResolved, InternetDisconnected, AddressUnreachable,
                            // BlockedByClient, BlockedByResponse
                            fetchFail.Parameters.Add("errorReason", "BlockedByClient");
                            await _pageConnection.SendForResponseAsync(fetchFail, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                    }

                    case "Page.loadEventFired":
                        if (!waitForNetworkIdle)
                        {
                            _logger?.WriteToLog("The 'Page.loadEventFired' event has been fired, the page is now fully loaded");
                            pageLoadingState = PageLoadingState.Done;
                        }

                        break;

                    default:
                    {
                        var page = Protocol.Page.Event.FromJson(data);

                        switch (page.Method)
                        {
                            // The DOMContentLoaded event is fired when the document has been completely loaded and parsed, without
                            // waiting for stylesheets, images, and sub frames to finish loading (the load event can be used to
                            // detect a fully-loaded page).
                            case "Page.lifecycleEvent" when page.Params?.Name == "DOMContentLoaded":

                                _logger?.WriteToLog("The 'Page.lifecycleEvent' with param name 'DomContentLoaded' has been fired, the dom content is now loaded and parsed, waiting for stylesheets, images and sub frames to finish loading");

                                if (mediaLoadTimeout.HasValue && !mediaLoadTimeoutStopwatch.IsRunning)
                                {
                                    _logger?.WriteToLog($"Media load timeout has a value of {mediaLoadTimeout.Value} milliseconds, starting stopwatch");
                                    mediaLoadTimeoutStopwatch.Start();
                                }

                                break;

                            case "Page.frameNavigated":
                                _logger?.WriteToLog("The 'Page.frameNavigated' event has been fired, waiting for the 'Page.lifecycleEvent' with name 'networkIdle'");
                                pageLoadingState = PageLoadingState.WaitForNetworkIdle;
                                break;

                            case "Page.lifecycleEvent" when page.Params?.Name == "networkIdle" && pageLoadingState == PageLoadingState.WaitForNetworkIdle:
                                _logger?.WriteToLog("The 'Page.lifecycleEvent' event with name 'networkIdle' has been fired, the page is now fully loaded and the network is idle");
                                pageLoadingState = PageLoadingState.Done;
                                break;

                            default:
                                var pageNavigateResponse = NavigateResponse.FromJson(data);
                                if (!string.IsNullOrEmpty(pageNavigateResponse.Result?.ErrorText) &&
                                    !pageNavigateResponse.Result.ErrorText.Contains("net::ERR_BLOCKED_BY_CLIENT"))
                                {
                                    navigationError = $"{pageNavigateResponse.Result.ErrorText} occurred when navigating to the page '{uri}'";
                                    pageLoadingState = PageLoadingState.BlockedByClient;
                                }

                                break;
                        }

                        break;
                    }
                }
                
                if (mediaLoadTimeoutStopwatch.IsRunning &&
                    mediaLoadTimeoutStopwatch.ElapsedMilliseconds >= mediaLoadTimeout.Value)
                {
                    _logger?.WriteToLog($"Media load timeout of {mediaLoadTimeout.Value} milliseconds reached, stopped loading page");
                    mediaLoadTimeoutStopwatch.Stop();
                    pageLoadingState = PageLoadingState.MediaLoadTimeout;
                }

                if (countdownTimer is { MillisecondsLeft: 0 })
                        throw new ConversionTimedOutException($"The {nameof(NavigateToAsync)} method timed out");
            }

            if (pageLoadingState == PageLoadingState.MediaLoadTimeout)
            {
                _logger?.WriteToLog("Stopping loading the rest of the page with injecting javascript 'window.stop()'");
                await RunJavascriptAsync("window.stop();", cancellationToken).ConfigureAwait(false);
            }

            // Do some cleanup

            var lifecycleEventDisabledMessage = new Message { Method = "Page.setLifecycleEventsEnabled" };
            lifecycleEventDisabledMessage.AddParameter("enabled", false);

            // Disables page domain notifications
            await _pageConnection.SendForResponseAsync(lifecycleEventDisabledMessage, cancellationToken).ConfigureAwait(false);
            await _pageConnection.SendForResponseAsync(new Message { Method = "Page.disable" }, cancellationToken).ConfigureAwait(false);

            // Disables the fetch domain
            if (urlBlacklist?.Count > 0)
            {
                _logger?.WriteToLog("Disabling Fetch");
                await _pageConnection.SendForResponseAsync(new Message { Method = "Fetch.disable" }, cancellationToken).ConfigureAwait(false);
            }

            if (logNetworkTraffic)
            {
                _logger?.WriteToLog("Disabling network traffic logging");
                var networkMessage = new Message { Method = "Network.disable" };
                await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(navigationError))
            {
                _logger?.WriteToLog(navigationError);
                throw new ChromiumNavigationException(navigationError);
            }
        }
        finally
        {
            _pageConnection.MessageReceived -= messageReceived;
            _pageConnection.Closed -= PageConnectionClosed;
        }

        return;

        void PageConnectionClosed(object o, EventArgs eventArgs)
        {
            pageLoadingState = PageLoadingState.Closed;
        }
    }
    #endregion

    #region WaitForWindowStatus
    /// <summary>
    ///     Waits until the javascript window.status is returning the given <paramref name="status" />
    /// </summary>
    /// <param name="status">The case insensitive status</param>
    /// <param name="timeout">Continue after reaching the set timeout in milliseconds</param>
    /// <returns><c>true</c> when window status matched, <c>false</c> when timing out</returns>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public bool WaitForWindowStatus(string status, int timeout = 60000)
    {
        return WaitForWindowStatusAsync(status, timeout).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    #endregion

    #region WaitForWindowStatusAsync
    /// <summary>
    ///     Waits until the javascript window.status is returning the given <paramref name="status" />
    /// </summary>
    /// <param name="status">The case insensitive status</param>
    /// <param name="timeout">Continue after reaching the set timeout in milliseconds</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><c>true</c> when window status matched, <c>false</c> when timing out</returns>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public async Task<bool> WaitForWindowStatusAsync(string status, int timeout = 60000, CancellationToken cancellationToken = default)
    {
        var message = new Message { Method = "Runtime.evaluate" };
        message.AddParameter("expression", "window.status;");
        message.AddParameter("silent", true);
        message.AddParameter("returnByValue", true);

        var match = false;

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        while (true)
        {
            var result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
            var evaluate = Evaluate.FromJson(result);

            if (evaluate.Result?.Result?.Value == status)
            {
                match = true;
                break;
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            if (stopWatch.ElapsedMilliseconds >= timeout) 
                break;
        }

        stopWatch.Stop();
        return match;
    }
    #endregion

    #region RunJavascript
    /// <summary>
    ///     Runs the given javascript after the page has been fully loaded
    /// </summary>
    /// <param name="script">The javascript to run</param>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public void RunJavascript(string script)
    {
        RunJavascriptAsync(script).GetAwaiter().GetResult();
    }
    #endregion

    #region RunJavascriptAsync
    /// <summary>
    ///     Runs the given javascript after the page has been fully loaded
    /// </summary>
    /// <param name="script">The javascript to run</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public async Task RunJavascriptAsync(string script, CancellationToken cancellationToken = default)
    {
        var message = new Message { Method = "Runtime.evaluate" };
        message.AddParameter("expression", script);
        message.AddParameter("silent", false);
        message.AddParameter("returnByValue", false);

        var errorDescription = string.Empty;
        var result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
        var evaluateError = EvaluateError.FromJson(result);

        if (evaluateError.Result?.ExceptionDetails != null)
            errorDescription = evaluateError.Result.ExceptionDetails.Exception.Description;

        if (!string.IsNullOrEmpty(errorDescription))
            throw new ChromiumException(errorDescription);

        var evaluate = Evaluate.FromJson(result);
        var internalResult = evaluate.Result?.Result?.ToString();

        _logger?.WriteToLog(!string.IsNullOrEmpty(internalResult)
            ? $"Javascript result:{Environment.NewLine}{internalResult}"
            : "Javascript did not return any result");
    }
    #endregion

    #region CaptureSnapshotAsync
    /// <summary>
    ///     Instructs Chromium to capture a snapshot from the loaded page
    /// </summary>
    /// <param name="countdownTimer">
    ///     If a <see cref="CountdownTimer" /> is set then
    ///     the method will raise an <see cref="ConversionTimedOutException" /> in the
    ///     <see cref="CountdownTimer" /> reaches zero before finishing the printing to pdf
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <remarks>
    ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page#method-captureSnapshot
    /// </remarks>
    /// <returns></returns>
    internal async Task<SnapshotResponse> CaptureSnapshotAsync(
        CountdownTimer countdownTimer = null, 
        CancellationToken cancellationToken = default)
    {
        var message = new Message { Method = "Page.captureSnapshot" };

        var result = countdownTimer == null
            ? await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false)
            : await _pageConnection.SendForResponseAsync(message, new CancellationTokenSource(countdownTimer.MillisecondsLeft).Token).ConfigureAwait(false);

        return SnapshotResponse.FromJson(result);
    }
    #endregion

    #region PrintToPdfAsync
    /// <summary>
    ///     Instructs Chromium to print the page
    /// </summary>
    /// <param name="outputStream">The generated PDF gets written to this stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="countdownTimer">
    ///     If a <see cref="CountdownTimer" /> is set then
    ///     the method will raise an <see cref="ConversionTimedOutException" /> in the
    ///     <see cref="CountdownTimer" /> reaches zero before finishing the printing to pdf
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <remarks>
    ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF
    /// </remarks>
    /// <exception cref="ConversionException">Raised when Chromium returns an empty string</exception>
    /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer" /> reaches zero</exception>
    internal async Task PrintToPdfAsync(Stream outputStream,
        PageSettings pageSettings,
        CountdownTimer countdownTimer = null,
        CancellationToken cancellationToken = default)
    {
        var message = new Message { Method = "Page.printToPDF" };
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
        message.AddParameter("transferMode", "ReturnAsStream");
        message.AddParameter("generateTaggedPDF", pageSettings.TaggedPDF);
        message.AddParameter("generateDocumentOutline", pageSettings.GenerateOutline);

        var result = countdownTimer == null
            ? await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false)
            : await _pageConnection.SendForResponseAsync(message, new CancellationTokenSource(countdownTimer.MillisecondsLeft).Token).ConfigureAwait(false);

        var printToPdfResponse = PrintToPdfResponse.FromJson(result);

        if (string.IsNullOrEmpty(printToPdfResponse.Result?.Stream))
            throw new ConversionException($"Conversion failed ... did not get the expected response from Chromium, response '{result}'");

        if (!outputStream.CanWrite)
            throw new ConversionException("The output stream is not writable, please provide a writable stream");

        _logger?.WriteToLog("Resetting output stream to position 0");
        
        message = new Message { Method = "IO.read" };
        message.AddParameter("handle", printToPdfResponse.Result.Stream);
        message.AddParameter("size", 1048576); // Get the pdf in chunks of 1MB
        
        _logger?.WriteToLog($"Reading generated PDF from IO stream with handle id {printToPdfResponse.Result.Stream}");
        outputStream.Position = 0;

        while (true)
        {
            result = countdownTimer == null
                ? await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false)
                : await _pageConnection.SendForResponseAsync(message, new CancellationTokenSource(countdownTimer.MillisecondsLeft).Token).ConfigureAwait(false);

            var ioReadResponse = IoReadResponse.FromJson(result);
            var bytes = ioReadResponse.Result.Bytes;
            var length = bytes.Length;

            if (length > 0)
            {
                _logger?.WriteToLog($"PDF chunk received with id {ioReadResponse.Id} and length {FileManager.GetFileSizeString(length, CultureInfo.InvariantCulture)}, writing it to output stream");
                await outputStream.WriteAsync(bytes, 0, length, cancellationToken).ConfigureAwait(false);
            }

            if (!ioReadResponse.Result.Eof) continue;

            _logger?.WriteToLog("Last chunk received");
            _logger?.WriteToLog($"Closing stream with id {printToPdfResponse.Result.Stream}");
            message = new Message { Method = "IO.close" };
            message.AddParameter("handle", printToPdfResponse.Result.Stream);
            await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
            _logger?.WriteToLog("Stream closed");
            break;
        }
    }
    #endregion

    #region CaptureScreenshotAsync
    /// <summary>
    ///     Instructs Chromium to take a screenshot from the page
    /// </summary>
    /// <param name="countdownTimer"></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    /// <exception cref="ConversionException">Raised when Chromium returns an empty string</exception>
    /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer" /> reaches zero</exception>
    internal async Task<CaptureScreenshotResponse> CaptureScreenshotAsync(
        CountdownTimer countdownTimer = null, 
        CancellationToken cancellationToken = default)
    {
        var message = new Message { Method = "Page.captureScreenshot" };
        var result = countdownTimer == null
            ? await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false)
            : await _pageConnection.SendForResponseAsync(message, new CancellationTokenSource(countdownTimer.MillisecondsLeft).Token).ConfigureAwait(false);

        var captureScreenshotResponse = CaptureScreenshotResponse.FromJson(result);

        if (string.IsNullOrEmpty(captureScreenshotResponse.Result?.Data))
            throw new ConversionException("Screenshot capture failed");

        return captureScreenshotResponse;
    }
    #endregion

    #region CloseAsync
    /// <summary>
    ///     Instructs the browser to close
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    internal async Task CloseAsync(CancellationToken cancellationToken)
    {
        var message = new Message { Method = "Browser.close" };
        await _browserConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region Dispose
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CloseAsync(CancellationToken.None).GetAwaiter().GetResult();

        if (_pageConnection != null)
        {
            _pageConnection.Dispose();
            _pageConnection = null;
        }

        if (_browserConnection != null)
        {
            _browserConnection.Dispose();
            _browserConnection = null;
        }

        _disposed = true;
    }
    #endregion

    #region DisposeAsync
#if (!NETSTANDARD2_0)    
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await CloseAsync(CancellationToken.None).ConfigureAwait(false);

        if (_pageConnection != null)
        {
            await _pageConnection.DisposeAsync().ConfigureAwait(false);
            _pageConnection = null;
        }

        if (_browserConnection != null)
        {
            await _browserConnection.DisposeAsync().ConfigureAwait(false);
            _browserConnection = null;
        }

        _disposed = true;
    }
#endif
    #endregion
}