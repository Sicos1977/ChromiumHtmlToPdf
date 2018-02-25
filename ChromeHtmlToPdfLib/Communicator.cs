//
// Communicator.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using ChromeHtmlToPdfLib.Protocol;
using ChromeHtmlToPdfLib.Settings;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using WebSocket = WebSocket4Net.WebSocket;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    ///     Handles all the communication tasks with Chrome remote devtools
    /// </summary>
    /// <remarks>
    ///     See https://chromium.googlesource.com/v8/v8/+/master/src/inspector/js_protocol.json
    /// </remarks>
    internal class Communicator : IDisposable
    {
        #region Fields
        /// <summary>
        ///     The websocket need to communicate with Chrome
        /// </summary>
        private readonly WebSocket _webSocket;

        /// <summary>
        ///     The message that we are sending to Chrome
        /// </summary>
        private string _message;

        /// <summary>
        ///     The Chrome message id
        /// </summary>
        private int _messageId;
        #endregion
        
        #region Properties
        private int MessageId
        {
            get
            {
                _messageId += 1;
                return _messageId;
            }
        }
        #endregion

        #region Constructor & destructor
        /// <summary>
        ///     Makes this object and sets the Chrome remote debugging url
        /// </summary>
        /// <param name="browser">The websocket to the browser</param>
        internal Communicator(string browser)
        {
            var target = CreateTarget(browser);

            var waitEvent = new ManualResetEvent(false);
            
            // ws://localhost:9222/devtools/page/BA386DE8075EB19DDCE459B4B623FBE7
            _webSocket = new WebSocket(target);
            _webSocket.Error += WebSocketOnError;

            _webSocket.Opened += (sender, args) =>
            {
                waitEvent.Set();
            };
            _webSocket.Open();
            waitEvent.WaitOne();
        }

        ~Communicator()
        {
            Dispose();
        }
        #endregion

        #region WebSocket
        /// <summary>
        ///     Uses the <see cref="_webSocket" /> to send a message to Chrome
        /// </summary>
        /// <param name="message"></param>
        private void WebSocketSend(string message)
        {
            _message = message;
            _webSocket.Send(message);
        }

        /// <summary>
        ///     Raised when a <see cref="_webSocket" /> error occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorEventArgs"></param>
        private void WebSocketOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            throw new WebSocketException($"An error occured while sending the JSON message '{_message}' to Chrome",
                errorEventArgs.Exception);
        }
        #endregion

        #region CreateTarget
        /// <summary>
        /// Creates a new page (tab)
        /// </summary>
        /// <param name="browser">The websocket to the browser</param>
        private string CreateTarget(string browser)
        {
            var webSocket = new WebSocket(browser);
            var waitEvent = new ManualResetEvent(false);
            Page page = null;

            webSocket.Opened += (sender, args) => waitEvent.Set();

            webSocket.MessageReceived += (sender, args) =>
            {
                CheckForError(args.Message);
                page = Page.FromJson(args.Message);
                waitEvent.Set();
            };

            webSocket.Open();
            waitEvent.WaitOne();
            waitEvent.Reset();

            var message = new Message
            {
                Id = MessageId,
                Method = "Target.createTarget"
            };
            message.Parameters.Add("url", "about:blank");
            webSocket.Send(message.ToJson());
            waitEvent.WaitOne();
            webSocket.Close();

            // ws://localhost:9222/devtools/page/BA386DE8075EB19DDCE459B4B623FBE7
            // ws://127.0.0.1:50841/devtools/browser/9a919bf0-b243-479d-8396-ede653356e12
            return page.Result.TargetId;
        }
        #endregion

        #region NavigateTo
        /// <summary>
        ///     Instructs Chrome to navigate to the given <paramref name="uri" />
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="waitForNetworkIdle">Wait until all external sources are loaded</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public void NavigateTo(Uri uri, bool waitForNetworkIdle, int? timeout = null)
        {
            WebSocketSend(new Message {Id = MessageId, Method = "Page.enable"}.ToJson());

            var message = new Message
            {
                Id = MessageId,
                Method = "Page.navigate"
            };

            message.AddParameter("url", uri.ToString());

            var waitEvent = new ManualResetEvent(false);

            EventHandler<MessageReceivedEventArgs> messageReceived = (sender, args) =>
            {
                File.AppendAllText("d:\\trace.txt", args.Message + Environment.NewLine);
                CheckForError(args.Message);
                var page = PageEvent.FromJson(args.Message);

                if (!uri.IsFile)
                {
                    if (waitForNetworkIdle && page.Params?.Name == "networkIdle")
                            waitEvent.Set();
                    else if (page.Method == "Page.lifecycleEvent" && page.Params?.Name == "DOMContentLoaded")
                        waitEvent.Set();
                }
                else if (page.Method == "Page.loadEventFired")
                    waitEvent.Set();
            };

            _webSocket.MessageReceived += messageReceived;

            WebSocketSend(message.ToJson());

            if (timeout.HasValue)
                waitEvent.WaitOne(timeout.Value);
            else
                waitEvent.WaitOne();

            _webSocket.MessageReceived -= messageReceived;

            WebSocketSend(new Message {Id = MessageId, Method = "Page.disable"}.ToJson());
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
            var message = new Message
            {
                Id = MessageId,
                Method = "Runtime.evaluate"
            };

            message.AddParameter("expression", "window.status;");
            message.AddParameter("silent", true);
            message.AddParameter("returnByValue", true);

            var waitEvent = new ManualResetEvent(false);
            var match = false;

            EventHandler<MessageReceivedEventArgs> messageReceived = (sender, args) =>
            {
                CheckForError(args.Message);
                var evaluate = Evaluate.FromJson(args.Message);
                if (evaluate.Result?.Result?.Value == status)
                {
                    match = true;
                    waitEvent.Set();
                }
            };

            _webSocket.MessageReceived += messageReceived;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (!match)
            {
                WebSocketSend(message.ToJson());
                waitEvent.WaitOne(10);
                if (stopWatch.ElapsedMilliseconds >= timeout) break;
            }

            stopWatch.Stop();
            _webSocket.MessageReceived -= messageReceived;

            return match;
        }
        #endregion

        #region PrintToPdf
        /// <summary>
        ///     Instructs Chrome to print the page
        /// </summary>
        /// <param name="pageSettings">
        ///     <see cref="PageSettings" />
        /// </param>
        /// <remarks>
        ///     See https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF
        /// </remarks>
        /// <returns>
        ///     <code>
        ///         var uri = new Uri("http://www.google.nl");
        ///         var result = NavigateTo(uri);
        ///         // Succes when value contains the Uri result.InnerResult.Value 
        ///         // contains the requested uri.
        ///         if (result.Result.ExceptionDetails != null)
        ///             throw new Exception("Navigation failed");
        ///     </code>
        /// </returns>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        internal PrintToPdfResponse PrintToPdf(PageSettings pageSettings)
        {
            var message = new Message
            {
                Id = MessageId,
                Method = "Page.printToPDF"
            };

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

            PrintToPdfResponse response = null;

            var waitEvent = new ManualResetEvent(false);

            EventHandler<MessageReceivedEventArgs> messageReceived = (sender, args) =>
            {
                //File.AppendAllText("d:\\trace.txt", args.Message + Environment.NewLine);
                CheckForError(args.Message);
                response = PrintToPdfResponse.FromJson(args.Message);
                if (response.Result?.Data != null)
                    waitEvent.Set();
            };

            _webSocket.MessageReceived += messageReceived;
            WebSocketSend(message.ToJson());
            waitEvent.WaitOne();
            _webSocket.MessageReceived -= messageReceived;

            return response;
        }
        #endregion

        #region CheckForError
        /// <summary>
        ///     Checks if <paramref name="message"/> contains an error and if so raises an exception
        /// </summary>
        /// <param name="message"></param>
        private void CheckForError(string message)
        {
            var error = Error.FromJson(message);
            if (error.InnerError != null && error.InnerError.Code != 0)
                throw new ChromeException(error.InnerError.Message);
        }
        #endregion

        #region Close
        /// <summary>
        ///     Instructs Chrome to close
        /// </summary>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public void Close()
        {
            var message = new Message
            {
                Id = MessageId,
                Method = "Browser.close"
            };

            EventHandler<MessageReceivedEventArgs> messageReceived = (sender, args) =>
            {
                CheckForError(args.Message);
                File.AppendAllText("d:\\trace.txt", args.Message + Environment.NewLine);
            };

            _webSocket.MessageReceived += messageReceived;
            WebSocketSend(message.ToJson());
            _webSocket.MessageReceived -= messageReceived;
        }
        #endregion

        #region Dispose
        /// <summary>
        ///     Disposes the opened <see cref="_webSocket" />
        /// </summary>
        public void Dispose()
        {
            _webSocket?.Dispose();
        }
        #endregion
    }
}