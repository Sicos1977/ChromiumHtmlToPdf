//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2018 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Helpers;
using ChromeHtmlToPdfLib.Protocol;
using ChromeHtmlToPdfLib.Settings;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    ///     Handles all the communication tasks with Chrome remote devtools
    /// </summary>
    /// <remarks>
    ///     See https://chromium.googlesource.com/v8/v8/+/master/src/inspector/js_protocol.json
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
            _browserConnection = Connection.Create(browser.ToString());

            var message = new Message
            {
                Method = "Target.createTarget"
            };

            message.Parameters.Add("url", "about:blank");

            var result = _browserConnection.SendAsync(message).Result;

            var page = Page.FromJson(result);
            
            // ws://localhost:9222/devtools/page/BA386DE8075EB19DDCE459B4B623FBE7
            // ws://127.0.0.1:50841/devtools/browser/9a919bf0-b243-479d-8396-ede653356e12
            var pageUrl = $"{browser.Scheme}://{browser.Host}:{browser.Port}/devtools/page/{page.Result.TargetId}";
            _pageConnection = Connection.Create(pageUrl);
        }
        #endregion

        #region NavigateTo
        /// <summary>
        ///     Instructs Chrome to navigate to the given <paramref name="uri" />
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="countdownTimer">If a <see cref="CountdownTimer"/> is set then
        /// the method will raise an <see cref="ConversionTimedOutException"/> if the 
        /// <see cref="CountdownTimer"/> reaches zero before finishing navigation</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        /// <exception cref="ConversionTimedOutException">Raised when <paramref name="countdownTimer"/> reaches zero</exception>
        public void NavigateTo(Uri uri, CountdownTimer countdownTimer = null)
        {
            _pageConnection.SendAsync(new Message {Method = "Page.enable"}).GetAwaiter();

            var message = new Message {Method = "Page.navigate"};
            message.AddParameter("url", uri.ToString());

            var waitEvent = new ManualResetEvent(false);

            void MessageReceived(object sender, string data)
            {
                var page = PageEvent.FromJson(data);

                if (!uri.IsFile)
                {
                    switch (page.Method)
                    {
                        case "Page.lifecycleEvent" when page.Params?.Name == "DOMContentLoaded":
                        case "Page.frameStoppedLoading":
                            waitEvent.Set();
                            break;
                    }
                }
                else if (page.Method == "Page.loadEventFired") waitEvent.Set();
            }

            _pageConnection.MessageReceived += MessageReceived;
            _pageConnection.Closed += (sender, args) =>
            {
                waitEvent.Set();
            };
            _pageConnection.SendAsync(message).GetAwaiter();

            if (countdownTimer != null)
            {
                waitEvent.WaitOne(countdownTimer.MillisecondsLeft);
                if (countdownTimer.MillisecondsLeft == 0)
                    throw new ConversionTimedOutException($"The {nameof(NavigateTo)} method timedout");
            }
            else
                waitEvent.WaitOne();

            _pageConnection.MessageReceived -= MessageReceived;

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

            EventHandler<string> messageReceived = (sender, data) =>
            {
                var evaluate = Evaluate.FromJson(data);
                if (evaluate.Result?.Result?.Value == status)
                {
                    match = true;
                    waitEvent.Set();
                }
            };

            _pageConnection.MessageReceived += messageReceived;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (!match)
            {
                _pageConnection.SendAsync(message).GetAwaiter();
                waitEvent.WaitOne(10);
                if (stopWatch.ElapsedMilliseconds >= timeout) break;
            }

            stopWatch.Stop();
            _pageConnection.MessageReceived -= messageReceived;

            return match;
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
        internal PrintToPdfResponse PrintToPdf(PageSettings pageSettings, CountdownTimer countdownTimer = null)
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
                ? _pageConnection.SendAsync(message).GetAwaiter().GetResult()
                : _pageConnection.SendAsync(message).Timeout(countdownTimer.MillisecondsLeft).GetAwaiter().GetResult();

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
        /// <see cref="CountdownTimer"/> reaches zero before Chrome respons that it is going to close</param>    
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