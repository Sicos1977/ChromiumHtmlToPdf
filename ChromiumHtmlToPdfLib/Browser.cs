//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2026 Magic-Sessions. (www.magic-sessions.com)
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
internal class Browser : IDisposable
#else
internal class Browser : IDisposable, IAsyncDisposable
#endif
{
    #region Fields
    /// <summary>
    ///     A connection to the browser (Chrome or Edge)
    /// </summary>
    private readonly Connection _browserConnection;

    /// <summary>
    ///     A connection to a page
    /// </summary>
    private readonly Connection _pageConnection;

    /// <summary>
    ///     <see cref="Logger"/>
    /// </summary>
    private readonly Logger? _logger;

    /// <summary>
    ///     Keeps track is we already disposed our resources
    /// </summary>
    private bool _disposed;
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets the Chromium remote debugging url
    /// </summary>
    /// <param name="logger"><see cref="Logger"/></param>
    /// <param name="browserConnection">The websocket connection to the browser</param>
    /// <param name="pageConnection">The websocket connection to devtools page</param>
    private Browser(Logger? logger, Connection browserConnection, Connection pageConnection)
    {
        _logger = logger;
        _browserConnection = browserConnection;
        _pageConnection = pageConnection;
    }

    /// <summary>
    ///     Makes this object and sets the Chromium remote debugging url
    /// </summary>
    /// <param name="browser">The websocket to the browser</param>
    /// <param name="timeout">Websocket open timeout in milliseconds</param>
    /// <param name="logger"><see cref="Logger"/></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public static async Task<Browser> Create(Uri browser, int timeout, Logger? logger, CancellationToken cancellationToken)
    {
        // Open a websocket to the browser
        var browserConnection = await Connection.Create(browser.ToString(), timeout, logger, cancellationToken).ConfigureAwait(false);

        var message = new Message { Method = "Target.createTarget" };
        message.Parameters.Add("url", "about:blank");

        var result = await browserConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
        var page = Page.FromJson(result);
        var pageUrl = $"{browser.Scheme}://{browser.Host}:{browser.Port}/devtools/page/{page.Result.TargetId}";

        // Open a websocket to the page
        var pageConnection = await Connection.Create(pageUrl, timeout, logger, cancellationToken).ConfigureAwait(false);

        return new Browser(logger, browserConnection, pageConnection);
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
    public async Task NavigateToAsync(
        List<string> safeUrls,
        bool useCache,
        ConvertUri? uri,
        string? html,
        int? mediaLoadTimeout,
        List<string>? urlBlacklist,
        bool logNetworkTraffic,
        bool waitForNetworkIdle,
        CancellationToken cancellationToken)
    {
        var navigationError = string.Empty;
        var navigationErrorTemplate = string.Empty;
        object?[] navigationErrorArgs = [];
        var absoluteUri = uri?.AbsoluteUri.Substring(0, uri.AbsoluteUri.LastIndexOf('/') + 1);

        if (uri?.RequestHeaders?.Count > 0)
        {
            _logger?.Info("Setting request headers");
            var networkMessage = new Message { Method = "Network.setExtraHTTPHeaders" };
            networkMessage.AddParameter("headers", uri.RequestHeaders);
            await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
        }

        if (logNetworkTraffic)
        {
            _logger?.Info("Enabling network traffic logging");
            var networkMessage = new Message { Method = "Network.enable" };
            await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
        }

        _logger?.Info(useCache ? "Enabling caching" : "Disabling caching");

        var cacheMessage = new Message { Method = "Network.setCacheDisabled" };
        cacheMessage.Parameters.Add("cacheDisabled", !useCache);
        await _pageConnection.SendForResponseAsync(cacheMessage, cancellationToken).ConfigureAwait(false);

        // Enables issuing of requestPaused events. A request will be paused until client calls one of failRequest, fulfillRequest or continueRequest/continueWithAuth
        if (urlBlacklist?.Count > 0)
        {
            _logger?.Info("Enabling Fetch to block url's that are in the url blacklist");
            await _pageConnection.SendForResponseAsync(new Message { Method = "Fetch.enable" }, cancellationToken).ConfigureAwait(false);
        }

        // Enables page domain notifications
        _logger?.Info("Enabling page domain notifications");
        await _pageConnection.SendForResponseAsync(new Message { Method = "Page.enable" }, cancellationToken).ConfigureAwait(false);

        // When HTML content is used via Page.setDocumentContent and the converter is reused, Chrome fires
        // synthetic lifecycle events (including networkIdle) for the already-loaded page when
        // Page.setLifecycleEventsEnabled is re-enabled. These synthetic events share the same loaderId as
        // the previous content, and Chrome does not re-fire networkIdle for Page.setDocumentContent when
        // it considers the frame already idle. Navigating to about:blank first resets the frame to a clean
        // state with a new loaderId, ensuring Chrome fires fresh lifecycle events for the new content.
        if (!string.IsNullOrWhiteSpace(html))
        {
            _logger?.Info("Navigating to about:blank to reset frame state before setting HTML content");
            var navigateToBlankMessage = new Message { Method = "Page.navigate" };
            navigateToBlankMessage.AddParameter("url", "about:blank");
            await _pageConnection.SendForResponseAsync(navigateToBlankMessage, cancellationToken).ConfigureAwait(false);
        }

        var lifecycleEventEnabledMessage = new Message { Method = "Page.setLifecycleEventsEnabled" };
        lifecycleEventEnabledMessage.AddParameter("enabled", true);
        _logger?.Info("Enabling page lifecycle events");
        await _pageConnection.SendForResponseAsync(lifecycleEventEnabledMessage, cancellationToken).ConfigureAwait(false);

        var waitForMessage = new SemaphoreSlim(1);
        var messagePump = new ConcurrentQueue<string>();
        var messageReceived = new EventHandler<string>(delegate(object? _, string data)
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
                _logger?.Info("Navigating to url '{uri}'", uri);
                await _pageConnection.SendAsync(pageNavigateMessage, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(html))
            {
                _logger?.Info("Getting page frame tree");
                var pageGetFrameTree = new Message { Method = "Page.getFrameTree" };
                var frameTree = await _pageConnection.SendForResponseAsync(pageGetFrameTree, cancellationToken).ConfigureAwait(false);
                var frameResult = FrameTree.FromJson(frameTree);

                _logger?.Info("Setting document content");

                var pageSetDocumentContent = new Message { Method = "Page.setDocumentContent" };
                pageSetDocumentContent.AddParameter("frameId", frameResult.Result.FrameTree.Frame.Id);
                pageSetDocumentContent.AddParameter("html", html!);
                await _pageConnection.SendAsync(pageSetDocumentContent, cancellationToken).ConfigureAwait(false);
                // When using setDocumentContent a Page.frameNavigated event is never fired, so we have to set the waitForNetworkIdle to true our self
                pageLoadingState = PageLoadingState.WaitForNetworkIdle;
                _logger?.Info("Document content set");
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
                        _logger?.Info("Request sent with request id {requestId} for url '{url}' with method {method} and type {type}",
                            requestWillBeSent.Params.RequestId,
                            requestWillBeSent.Params.Request.Url,
                            requestWillBeSent.Params.Request.Method,
                            requestWillBeSent.Params.Type);
                        break;

                    case "Network.dataReceived":
                        var dataReceived = DataReceived.FromJson(data);
                        _logger?.Info("Data received for request id {requestId} with a length of {length}",
                            dataReceived.Params.RequestId,
                            FileManager.GetFileSizeString(dataReceived.Params.DataLength, CultureInfo.InvariantCulture));
                        break;

                    case "Network.responseReceived":
                        var responseReceived = ResponseReceived.FromJson(data);
                        var response = responseReceived.Params.Response;

                        if (string.IsNullOrWhiteSpace(response.RemoteIpAddress))
                            _logger?.Info((response.FromDiskCache ? "Cached response" : "Response") + " received for request id {requestId} and url '{url}'",
                                responseReceived.Params.RequestId,
                                response.Url);
                        else if (string.IsNullOrWhiteSpace(response.StatusText))
                            _logger?.Info((response.FromDiskCache ? "Cached response" : "Response") + " received for request id {requestId} and url '{url}' from ip {ip} on port {port} with status {status}",
                                responseReceived.Params.RequestId,
                                response.Url,
                                response.RemoteIpAddress,
                                response.RemotePort,
                                response.Status);
                        else
                            _logger?.Info((response.FromDiskCache ? "Cached response" : "Response") + " received for request id {requestId} and url '{url}' from ip {ip} on port {port} with status {status}  and response text '{statusText}'",
                                responseReceived.Params.RequestId,
                                response.Url,
                                response.RemoteIpAddress,
                                response.RemotePort,
                                response.Status,
                                response.StatusText);

                        break;

                    case "Network.loadingFinished":
                        var loadingFinished = LoadingFinished.FromJson(data);

                        if (loadingFinished.Params.EncodedDataLength > 0)
                            _logger?.Info("Loading finished for request id {requestId} with an encoded data length of {length}",
                                loadingFinished.Params.RequestId,
                                FileManager.GetFileSizeString(loadingFinished.Params.EncodedDataLength, CultureInfo.InvariantCulture));
                        else
                            _logger?.Info("Loading finished for request id {requestId}", loadingFinished.Params.RequestId);
                        break;

                    case "Network.loadingFailed":
                        var loadingFailed = LoadingFailed.FromJson(data);
                        _logger?.Error("Loading failed for request id {requestId} and type {type} with error '{error}'",
                            loadingFailed.Params.RequestId,
                            loadingFailed.Params.Type,
                            loadingFailed.Params.ErrorText);
                        break;

                    case "Network.requestServedFromCache":
                        var requestServedFromCache = RequestServedFromCache.FromJson(data);
                        _logger?.Info("The request with id {requestId} is served from cache", requestServedFromCache.Params.RequestId);
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
                                _logger?.Info("The url '{url}' has been allowed because it is on the safe url list", url);
                            else if (isAbsoluteFileUri)
                                _logger?.Info("The file url '{url}' has been allowed because it start with the absolute uri '{absoluteUri}'", url, absoluteUri);
                            else
                                _logger?.Info("The url '{url}' has been allowed because it did not match anything on the url blacklist", url);

                            var fetchContinue = new Message { Method = "Fetch.continueRequest" };
                            fetchContinue.Parameters.Add("requestId", requestId);
                            
                            // Propagate request headers to subsequent calls
                            if (uri?.RequestHeaders?.Count > 0)
                            {
                                var headers = new List<Dictionary<string, string>>();
                                foreach (var header in uri.RequestHeaders)
                                {
                                    headers.Add(new Dictionary<string, string>
                                    {
                                        { "name", header.Key },
                                        { "value", header.Value }
                                    });
                                }
                                fetchContinue.Parameters.Add("headers", headers);
                            }
                            
                            await _pageConnection.SendForResponseAsync(fetchContinue, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger?.Warn("The url '{url}' has been blocked by url blacklist pattern '{matchedPattern}'", url, matchedPattern);

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
                            _logger?.Info("The 'Page.loadEventFired' event has been fired, the page is now fully loaded");
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

                                _logger?.Info("The '{method}' with param name '{paramName}' has been fired, the dom content is now loaded and parsed, waiting for stylesheets, images and sub frames to finish loading", page.Method, page.Params?.Name);

                                if (mediaLoadTimeout.HasValue && !mediaLoadTimeoutStopwatch.IsRunning)
                                {
                                    _logger?.Info("Media load timeout has a value of {timeout} milliseconds, starting stopwatch", mediaLoadTimeout.Value);
                                    mediaLoadTimeoutStopwatch.Start();
                                }

                                break;

                            case "Page.frameNavigated":
                                _logger?.Info("The '{method}' event has been fired, waiting for the 'Page.lifecycleEvent' with name 'networkIdle'", page.Method);
                                pageLoadingState = PageLoadingState.WaitForNetworkIdle;
                                break;

                            case "Page.lifecycleEvent" when page.Params?.Name == "networkIdle" && pageLoadingState == PageLoadingState.WaitForNetworkIdle:
                                _logger?.Info("The '{method}' event with name '{eventName}' has been fired, the page is now fully loaded and the network is idle", page.Method, page.Params?.Name);
                                pageLoadingState = PageLoadingState.Done;
                                break;

                            default:
                                var pageNavigateResponse = NavigateResponse.FromJson(data);
                                if (!string.IsNullOrEmpty(pageNavigateResponse.Result?.ErrorText) &&
                                    !pageNavigateResponse.Result!.ErrorText!.Contains("net::ERR_BLOCKED_BY_CLIENT"))
                                {
                                    navigationError = $"{pageNavigateResponse.Result.ErrorText} occurred when navigating to the page '{uri}'";
                                    navigationErrorTemplate = "{error} occurred when navigating to the page '{uri}'";
                                    navigationErrorArgs = [pageNavigateResponse.Result.ErrorText, uri];
                                    pageLoadingState = PageLoadingState.BlockedByClient;
                                }

                                break;
                        }

                        break;
                    }
                }

                if (mediaLoadTimeoutStopwatch.IsRunning &&
                    mediaLoadTimeoutStopwatch.ElapsedMilliseconds >= mediaLoadTimeout!.Value)
                {
                    _logger?.Warn("Media load timeout of {timeout} milliseconds reached, stopped loading page", mediaLoadTimeout.Value);
                    mediaLoadTimeoutStopwatch.Stop();
                    pageLoadingState = PageLoadingState.MediaLoadTimeout;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            if (pageLoadingState == PageLoadingState.MediaLoadTimeout)
            {
                _logger?.Warn("Stopping loading the rest of the page with injecting javascript 'window.stop()'");
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
                _logger?.Info("Disabling Fetch");
                await _pageConnection.SendForResponseAsync(new Message { Method = "Fetch.disable" }, cancellationToken).ConfigureAwait(false);
            }

            if (logNetworkTraffic)
            {
                _logger?.Info("Disabling network traffic logging");
                var networkMessage = new Message { Method = "Network.disable" };
                await _pageConnection.SendForResponseAsync(networkMessage, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(navigationError))
            {
                _logger?.Error(navigationErrorTemplate, navigationErrorArgs);
                throw new ChromiumNavigationException(navigationError);
            }
        }
        finally
        {
            _pageConnection.MessageReceived -= messageReceived;
            _pageConnection.Closed -= PageConnectionClosed;
        }

        return;

        void PageConnectionClosed(object? o, EventArgs eventArgs)
        {
            pageLoadingState = PageLoadingState.Closed;
        }
    }
    #endregion

    #region WaitForWindowStatusAsync
    /// <summary>
    ///     Waits until the javascript window.status is returning the given <paramref name="status" />
    /// </summary>
    /// <param name="status">The case-insensitive status</param>
    /// <param name="timeout">Continue after reaching the set timeout in milliseconds</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><c>true</c> when window status matched, <c>false</c> when timing out</returns>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public async Task<bool> WaitForWindowStatusAsync(string status, int timeout, CancellationToken cancellationToken)
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

    #region RunJavascriptAsync
    /// <summary>
    ///     Runs the given javascript after the page has been fully loaded
    /// </summary>
    /// <param name="script">The javascript to run</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ChromiumException">Raised when an error is returned by Chromium</exception>
    public async Task RunJavascriptAsync(string script, CancellationToken cancellationToken)
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
            throw new ChromiumException(errorDescription!);

        var evaluate = Evaluate.FromJson(result);
        var internalResult = evaluate.Result?.Result?.ToString();

        if (!string.IsNullOrEmpty(internalResult))
            _logger?.Info($"Javascript result:{Environment.NewLine}{{result}}", internalResult);
        else
            _logger?.Info("Javascript did not return any result");
    }
    #endregion

    #region CaptureSnapshotAsync
    /// <summary>
    ///     Instructs Chromium to capture a snapshot from the loaded page
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <remarks>
    ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page#method-captureSnapshot
    /// </remarks>
    /// <returns></returns>
    public async Task<SnapshotResponse> CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        var message = new Message { Method = "Page.captureSnapshot" };

        var result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);

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
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <remarks>
    ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF
    /// </remarks>
    /// <exception cref="ConversionException">Raised when Chromium returns an empty string</exception>
    public async Task PrintToPdfAsync(Stream outputStream, PageSettings pageSettings, CancellationToken cancellationToken)
    {
        var message = new Message { Method = "Page.printToPDF" };
        message.AddParameter("landscape", pageSettings.Landscape);
        message.AddParameter("displayHeaderFooter", pageSettings.DisplayHeaderFooter);
        message.AddParameter("printBackground", pageSettings.PrintBackground);
        message.AddParameter("scale", pageSettings.Scale);
        
        // Chrome DevTools Protocol expects portrait dimensions (width < height) and handles rotation internally
        // when landscape=true. If user provides landscape dimensions with landscape=true, swap them back to portrait.
        var paperWidth = pageSettings.PaperWidth;
        var paperHeight = pageSettings.PaperHeight;
        if (pageSettings.Landscape && paperWidth > paperHeight)
        {
            (paperWidth, paperHeight) = (paperHeight, paperWidth);
        }
        
        message.AddParameter("paperWidth", paperWidth);
        message.AddParameter("paperHeight", paperHeight);
        message.AddParameter("marginTop", pageSettings.MarginTop);
        message.AddParameter("marginBottom", pageSettings.MarginBottom);
        message.AddParameter("marginLeft", pageSettings.MarginLeft);
        message.AddParameter("marginRight", pageSettings.MarginRight);
        message.AddParameter("pageRanges", pageSettings.PageRanges ?? string.Empty);
        if (!string.IsNullOrEmpty(pageSettings.HeaderTemplate)) message.AddParameter("headerTemplate", pageSettings.HeaderTemplate!);
        if (!string.IsNullOrEmpty(pageSettings.FooterTemplate)) message.AddParameter("footerTemplate", pageSettings.FooterTemplate!);
        message.AddParameter("preferCSSPageSize", pageSettings.PreferCSSPageSize);
        message.AddParameter("transferMode", "ReturnAsStream");
        message.AddParameter("generateTaggedPDF", pageSettings.TaggedPdf);
        message.AddParameter("generateDocumentOutline", pageSettings.GenerateOutline);

        _logger?.Info("Sending PDF request to Chromium");

        var result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(result))
            throw new ConversionException("Conversion failed ... did not get the expected response from Chromium");

        var printToPdfResponse = PrintToPdfResponse.FromJson(result);

        if (printToPdfResponse.Error != null || string.IsNullOrEmpty(printToPdfResponse.Result?.Stream))
        {
            var errorMessage = printToPdfResponse.Error?.Message ?? result;
            throw new ConversionException($"Conversion failed ... did not get the expected response from Chromium, response '{errorMessage}'");
        }

        if (!outputStream.CanWrite)
            throw new ConversionException("The output stream is not writable, please provide a writable stream");

        _logger?.Info("Resetting output stream to position 0");

        message = new Message { Method = "IO.read" };
        message.AddParameter("handle", printToPdfResponse.Result!.Stream!);
        message.AddParameter("size", 1048576); // Get the pdf in chunks of 1MB

        _logger?.Info("Reading generated PDF from IO stream with handle id {stream}", printToPdfResponse.Result.Stream);
        outputStream.Position = 0;

        while (true)
        {
            result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);

            var ioReadResponse = IoReadResponse.FromJson(result);
            var bytes = ioReadResponse.Result.Bytes;
            var length = bytes.Length;

            if (length > 0)
            {
                _logger?.Info("PDF chunk received with id {id} and length {length}, writing it to output stream", ioReadResponse.Id, FileManager.GetFileSizeString(length, CultureInfo.InvariantCulture));
                await outputStream.WriteAsync(bytes, 0, length, cancellationToken).ConfigureAwait(false);
            }

            if (!ioReadResponse.Result.Eof) continue;

            _logger?.Info("Last chunk received");
            _logger?.Info("Closing stream with id {stream}", printToPdfResponse.Result.Stream);
            message = new Message { Method = "IO.close" };
            message.AddParameter("handle", printToPdfResponse.Result.Stream!);
            await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);
            _logger?.Info("Stream closed");
            break;
        }
    }
    #endregion

    #region CaptureScreenshotAsync
    /// <summary>
    ///     Instructs Chromium to take a screenshot from the page
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    /// <exception cref="ConversionException">Raised when Chromium returns an empty string</exception>
    public async Task<CaptureScreenshotResponse> CaptureScreenshotAsync(CancellationToken cancellationToken)
    {
        var message = new Message { Method = "Page.captureScreenshot" };
        var result = await _pageConnection.SendForResponseAsync(message, cancellationToken).ConfigureAwait(false);

        var captureScreenshotResponse = CaptureScreenshotResponse.FromJson(result);

        if (string.IsNullOrEmpty(captureScreenshotResponse.Result.Data))
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
    private async Task CloseAsync(CancellationToken cancellationToken)
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

        _pageConnection.Dispose();
        _browserConnection.Dispose();

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

        await _pageConnection.DisposeAsync().ConfigureAwait(false);

        await _browserConnection.DisposeAsync().ConfigureAwait(false);

        _disposed = true;
    }
#endif
    #endregion
}
