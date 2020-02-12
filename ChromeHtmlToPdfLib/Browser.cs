//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2019 Magic-Sessions. (www.magic-sessions.com)
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Helpers;
using ChromeHtmlToPdfLib.Protocol;
using ChromeHtmlToPdfLib.Settings;

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
        ///     A connection to the browser (Chrome)
        /// </summary>
        private readonly Connection _browserConnection;

        /// <summary>
        ///     A connection to a page
        /// </summary>
        private readonly Connection _pageConnection;
        #endregion

        #region Constructor & destructor
        /// <summary>
        ///     Makes this object and sets the Chrome remote debugging url
        /// </summary>
        /// <param name="browser">The websocket to the browser</param>
        internal Browser(Uri browser)
        {
            // Open a websocket to the browser
            _browserConnection = new Connection(browser.ToString());

            var message = new Message
            {
                Method = "Target.createTarget"
            };

            message.Parameters.Add("url", "about:blank");

            var result = _browserConnection.SendAsync(message).Result;

            var page = Page.FromJson(result);
            
            // ReSharper disable CommentTypo
            // ws://localhost:9222/devtools/page/BA386DE8075EB19DDCE459B4B623FBE7
            // ws://127.0.0.1:50841/devtools/browser/9a919bf0-b243-479d-8396-ede653356e12
            var pageUrl = $"{browser.Scheme}://{browser.Host}:{browser.Port}/devtools/page/{page.Result.TargetId}";
            // ReSharper restore CommentTypo
            _pageConnection = new Connection(pageUrl);
        }
        #endregion

        #region IsRegExMatch
        /// <summary>
        /// Returns <c>true</c> when a match in <paramref name="patterns"/> has been
        /// found for <paramref name="value"/>
        /// </summary>
        /// <param name="patterns">A list with regular expression</param>
        /// <param name="value">The string where to find the match</param>
        /// <param name="matchedPattern"></param>
        /// <returns></returns>
        private bool IsRegExMatch(IEnumerable<string> patterns, string value, out string matchedPattern)
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase))
                {
                    matchedPattern = Regex.Unescape(pattern);
                    return true;
                }
            }

            matchedPattern = string.Empty;
            return false;
        }
        #endregion

        #region NavigateTo
        /// <summary>
        ///     Instructs Chrome to navigate to the given <paramref name="uri" />
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        ///     the method will raise an <see cref="ConversionTimedOutException"/> if the 
        ///     <see cref="CountdownTimer"/> reaches zero before finishing navigation</param>
        /// <param name="mediaLoadTimeout">When set a timeout will be started after the DomContentLoaded
        ///     event has fired. After a timeout the NavigateTo method will exit as if the page
        ///     has been completely loaded</param>
        /// <param name="urlBlacklist">A list of URL's that need to be blocked (use * as a wildcard)</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer"/> reaches zero</exception>
        public void NavigateTo(
            Uri uri, 
            CountdownTimer countdownTimer = null, 
            int? mediaLoadTimeout = null,
            List<string> urlBlacklist = null)
        {
            var waitEvent = new ManualResetEvent(false);
            Task mediaLoadTimeoutTask = null;
            CancellationToken mediaLoadTimeoutCancellationToken;

            Logger.WriteToLog("Test");

            async Task MessageReceived(string data)
            {
                //System.IO.File.AppendAllText("e:\\logs.txt", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + data + Environment.NewLine);

                var message = Message.FromJson(data);

                switch (message.Method)
                {
                    case "Fetch.requestPaused":
                    {
                        var fetch = Fetch.FromJson(data);
                        var requestId = fetch.Params.RequestId;
                        var url = fetch.Params.Request.Url;

                        if (!IsRegExMatch(urlBlacklist, url, out var matchedPattern) || string.Equals(uri.AbsoluteUri, url, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.WriteToLog($"The url '{url}' has been allowed");
                            var fetchContinue = new Message {Method = "Fetch.continueRequest"};
                            fetchContinue.Parameters.Add("requestId", requestId);
                            _pageConnection.SendAsync(fetchContinue).GetAwaiter();
                        }
                        else
                        {
                            Logger.WriteToLog($"The url '{url}' has been blocked by url blacklist pattern '{matchedPattern}'");

                            var fetchFail = new Message {Method = "Fetch.failRequest"};
                            fetchFail.Parameters.Add("requestId", requestId);
                            // Failed, Aborted, TimedOut, AccessDenied, ConnectionClosed, ConnectionReset, ConnectionRefused,
                            // ConnectionAborted, ConnectionFailed, NameNotResolved, InternetDisconnected, AddressUnreachable,
                            // BlockedByClient, BlockedByResponse
                            fetchFail.Parameters.Add("errorReason", "BlockedByClient");
                            _pageConnection.SendAsync(fetchFail).GetAwaiter();
                        }

                        break;
                    }

                    default:
                    {
                        var page = PageEvent.FromJson(data);

                        if (!uri.IsFile)
                        {
                            switch (page.Method)
                            {
                                // The DOMContentLoaded event is fired when the document has been completely loaded and parsed, without
                                // waiting for stylesheets, images, and sub frames to finish loading (the load event can be used to
                                // detect a fully-loaded page).
                                case "Page.lifecycleEvent" when page.Params?.Name == "DOMContentLoaded":
                                    if (mediaLoadTimeout.HasValue)
                                    {
                                        mediaLoadTimeoutCancellationToken = new CancellationToken();
                                        mediaLoadTimeoutTask = Task.Delay(mediaLoadTimeout.Value,
                                            mediaLoadTimeoutCancellationToken);
                                        if (mediaLoadTimeoutTask != null)
                                            // ReSharper disable once PossibleNullReferenceException
                                            await mediaLoadTimeoutTask;

                                        Logger.WriteToLog(
                                            $"Media load timed out after {mediaLoadTimeout.Value} milliseconds");

                                        waitEvent.Set();
                                    }
                                    else
                                        waitEvent.Set();

                                    break;

                                case "Page.frameStoppedLoading":
                                    waitEvent.Set();
                                    break;
                            }
                        }
                        else
                        {
                            switch (page.Method)
                            {
                                // The DOMContentLoaded event is fired when the document has been completely loaded and parsed, without
                                // waiting for stylesheets, images, and sub frames to finish loading (the load event can be used to
                                // detect a fully-loaded page).
                                case "Page.domContentEventFired":
                                {
                                    if (mediaLoadTimeout.HasValue)
                                    {
                                        mediaLoadTimeoutCancellationToken = new CancellationToken();
                                        mediaLoadTimeoutTask = Task.Delay(mediaLoadTimeout.Value,
                                            mediaLoadTimeoutCancellationToken);
                                        if (mediaLoadTimeoutTask != null)
                                            // ReSharper disable once PossibleNullReferenceException
                                            await mediaLoadTimeoutTask;

                                        Logger.WriteToLog(
                                            $"Media load timed out after {mediaLoadTimeout.Value} milliseconds");

                                        waitEvent.Set();
                                    }

                                    break;
                                }

                                case "Page.loadEventFired":
                                    waitEvent.Set();
                                    break;
                            }
                        }

                        break;
                    }
                }
            }

            _pageConnection.MessageReceived += async (sender, data) => await MessageReceived(data);

            // Enable Fetch when we want to blacklist certain URL's
            if (urlBlacklist?.Count > 0)
            {
                Logger.WriteToLog("Enabling Fetch to block url's that are in the url blacklist'");
                _pageConnection.SendAsync(new Message {Method = "Fetch.enable"}).GetAwaiter();
            }

            _pageConnection.SendAsync(new Message { Method = "Page.enable" }).GetAwaiter();
            _pageConnection.Closed += (sender, args) => waitEvent.Set();

            var pageNavigateMessage = new Message {Method = "Page.navigate"};
            pageNavigateMessage.AddParameter("url", uri.ToString());

            _pageConnection.SendAsync(pageNavigateMessage).GetAwaiter();

            if (countdownTimer != null)
            {
                waitEvent.WaitOne(countdownTimer.MillisecondsLeft);
                if (countdownTimer.MillisecondsLeft == 0)
                    throw new ConversionTimedOutException($"The {nameof(NavigateTo)} method timed out");
            }
            else
                waitEvent.WaitOne();

            if (mediaLoadTimeoutCancellationToken != null)
                mediaLoadTimeoutTask?.Wait(mediaLoadTimeoutCancellationToken);

            // ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
            _pageConnection.MessageReceived -= async (sender, data) => await MessageReceived(data);

            // Disable Fetch again if it was enabled
            if (urlBlacklist?.Count > 0)
                _pageConnection.SendAsync(new Message {Method = "Fetch.disable"}).GetAwaiter();

            _pageConnection.SendAsync(new Message {Method = "Page.disable"}).GetAwaiter();
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
                _pageConnection.SendAsync(message).GetAwaiter();
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

            void MessageReceived(object sender, string data)
            {
                var evaluateError = EvaluateError.FromJson(data);

                if (evaluateError.Result?.ExceptionDetails != null)
                    errorDescription = evaluateError.Result.ExceptionDetails.Exception.Description;
            }

            _pageConnection.MessageReceived += MessageReceived;
            _pageConnection.SendAsync(message).GetAwaiter().GetResult();
            _pageConnection.MessageReceived -= MessageReceived;

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

        #region Close
        /// <summary>
        ///     Instructs Chrome to close
        /// </summary>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        /// the method will raise an <see cref="ConversionTimedOutException"/> in the 
        /// <see cref="CountdownTimer"/> reaches zero before Chrome responses that it is going to close</param>    
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public void Close(CountdownTimer countdownTimer = null)
        {
            var message = new Message {Method = "Browser.close"};

            if (countdownTimer != null)
                _browserConnection.SendAsync(message).Timeout(countdownTimer.MillisecondsLeft).GetAwaiter();
            else
                _browserConnection.SendAsync(message).GetAwaiter();
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _pageConnection?.Dispose();
            _browserConnection?.Dispose();
        }
        #endregion
    }
}