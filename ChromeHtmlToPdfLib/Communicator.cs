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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using ChromeHtmlToPdfLib.Protocol;
using ChromeHtmlToPdfLib.Settings;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using WebSocket = WebSocket4Net.WebSocket;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// Handles all the communication task with Chrome remote devtools
    /// </summary>
    /// <remarks>
    /// See https://chromium.googlesource.com/v8/v8/+/master/src/inspector/js_protocol.json
    /// </remarks>
    internal class Communicator : IDisposable
    {
        #region Fields
        /// <summary>
        /// The websocket need to communicate with Chrome
        /// </summary>
        private readonly WebSocket _webSocket;

        /// <summary>
        /// The message that we are sending to Chrome
        /// </summary>
        private string _message;

        /// <summary>
        /// The Chrome message id
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
        /// Makes this object and sets the Chrome remote debugging url
        /// </summary>
        /// <param name="remoteDebuggingUri"></param>
        internal Communicator(Uri remoteDebuggingUri)
        {
            var sessions = GetAvailableSessions(remoteDebuggingUri);
            if (sessions.Count == 0)
                throw new Exception("Could not retrieve remote sessions from Chrome");

            _webSocket = new WebSocket(sessions[0].WebSocketDebuggerUrl.Replace("ws://localhost", "ws://127.0.0.1"));
            _webSocket.Error += WebSocketOnError;

            var waitEvent = new ManualResetEvent(false);
            _webSocket.Opened += (sender, args) => { waitEvent.Set(); };
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
        /// Uses the <see cref="_webSocket"/> to send a message to Chrome
        /// </summary>
        /// <param name="message"></param>
        private void WebSocketSend(string message)
        {
            _message = message;
            _webSocket.Send(message);
        }

        /// <summary>
        /// Raised when a <see cref="_webSocket"/> error occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorEventArgs"></param>
        private void WebSocketOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            throw new WebSocketException($"An error occured while sending the JSON message '{_message}' to Chrome", errorEventArgs.Exception);
        }
        #endregion

        #region GetAvailableSessions
        /// <summary>
        /// Returns all the available Chrome remote debugging sesssions
        /// </summary>
        /// <param name="remoteDebuggingUri"></param>
        /// <returns></returns>
        internal List<RemoteSessionsResponse> GetAvailableSessions(Uri remoteDebuggingUri)
        {
            // Sometimes Chrome starts to slow and we try to make a connection to quick
            // in this case Chrome isn't already listening for incomming connections. That is
            // why we have a retry loop overhere
            var i = 0;

            while (true)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(remoteDebuggingUri + "json");
                    using (var response = request.GetResponse())
                    using (var responseStream = response.GetResponseStream())
                    {
                        if (responseStream == null) throw new InvalidOperationException();
                        var streamReader = new StreamReader(responseStream);
                        return RemoteSessionsResponse.FromJson(streamReader.ReadToEnd());
                    }
                }
                catch
                {
                    i++;

                    if (i < 5)
                    {
                        Thread.Sleep(i * 10);
                    }
                    else
                        throw;
                }
            }
        }
        #endregion

        #region NavigateTo
        /// <summary>
        /// Instructs Chrome to navigate to the given <paramref name="uri"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="waitForNetworkIdle">Wait until all external sources are loaded</param>
        /// <exception cref="ChromeException">Raised when an error is returned by Chrome</exception>
        public void NavigateTo(Uri uri, bool waitForNetworkIdle)
        {
            WebSocketSend(new Message { Id = MessageId, Method = "Page.enable" }.ToJson());

            var message = new Message
            {
                Id = MessageId,
                Method = "Page.navigate"
            };

            message.AddParameter("url", uri.ToString());

            var loaded = false;

            _webSocket.MessageReceived += (sender, args) =>
            {
                //File.AppendAllText("d:\\trace.txt", args.Message + Environment.NewLine);
                var page = PageEvent.FromJson(args.Message);

                if (!uri.IsFile)
                {
                    if (waitForNetworkIdle)
                    {
                        if (page.Params?.Name == "networkIdle")
                            loaded = true;
                    }
                    else if (page.Method == "Page.lifecycleEvent" && page.Params.Name == "DOMContentLoaded")
                        loaded = true;
                }
                else if (page.Method == "Page.loadEventFired")
                    loaded = true;
            };

            WebSocketSend(message.ToJson());

            while (!loaded)
                Thread.Sleep(1);

            WebSocketSend(new Message { Id = MessageId, Method = "Page.disable" }.ToJson());
        }
        #endregion

        #region WaitForWindowStatus
        /// <summary>
        /// Wait until the javascript window.status is returning the given <paramref name="status"/>
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

            var match = false;

            _webSocket.MessageReceived += (sender, args) =>
            {
                var evaluate = Evaluate.FromJson(args.Message);
                if (evaluate.Result?.Result?.Value == status)
                    match = true;
            };

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (!match)
            {
                WebSocketSend(message.ToJson());
                Thread.Sleep(10);
                if (stopWatch.ElapsedMilliseconds >= timeout) break;
            }

            stopWatch.Stop();

            return match;
        }
        #endregion

        #region PrintToPdf
        /// <summary>
        /// Instructs Chrome to print the page
        /// </summary>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <remarks>
        /// See https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-printToPDF
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
            message.AddParameter("pageRanges", pageSettings.PageRanges);
            message.AddParameter("ignoreInvalidPageRanges", pageSettings.IgnoreInvalidPageRanges);

            var converted = false;
            PrintToPdfResponse response = null;

            _webSocket.MessageReceived += (sender, args) =>
            {
                response = PrintToPdfResponse.FromJson(args.Message);
                if (response.Result?.Data != null)
                    converted = true;
            };

            WebSocketSend(message.ToJson());

            while (!converted)
                Thread.Sleep(1);

            return response;
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