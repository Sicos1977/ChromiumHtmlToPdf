//
// Connection.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2014-2020 Magic-Sessions. (www.magic-sessions.com)
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
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Protocol;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// A connection to a page (tab) in Chrome
    /// </summary>
    internal class Connection : IDisposable
    {
        #region Events
        /// <summary>
        /// Triggered when a connection to the <see cref="_webSocket"/> is closed
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Triggered when a new message is received on the <see cref="_webSocket"/>
        /// </summary>
        public event EventHandler<string> MessageReceived;
        #endregion

        #region Fields
        private int _messageId;
        private TaskCompletionSource<string> _response;

        /// <summary>
        /// The websocket
        /// </summary>
        private readonly WebSocket _webSocket;
        #endregion

        #region Constructor
        /// <summary>
        /// Makes this object and sets all it's needed properties
        /// </summary>
        /// <param name="url">The url</param>
        internal Connection(string url)
        {
            _webSocket = new WebSocket(url);
            _webSocket.MessageReceived += WebSocketOnMessageReceived;
            _webSocket.Error += WebSocketOnError;
            _webSocket.Closed += WebSocketOnClosed;
            OpenWebSocket();
        }
        #endregion

        #region OpenWebSocket
        private void OpenWebSocket()
        {
            if (_webSocket.State == WebSocketState.Open)
                return;

            var connected = false;
            var i = 0;

            _webSocket.Opened += delegate { connected = true; };
            _webSocket.Open();

            while (!connected)
            {
                Thread.Sleep(1);
                i += 1;

                if (i == 30000)
                    throw new ChromeException("Websocket connection timed out after 30 seconds");
            }
        }
        #endregion

        #region Websocket events
        private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var response = e.Message;

            CheckForError(response);

            var messageBase = MessageBase.FromJson(response);

            if (_messageId == messageBase.Id)
                _response.SetResult(response);

            MessageReceived?.Invoke(this, response);
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            if (_response.Task.Status != TaskStatus.RanToCompletion)
                _response.SetResult(string.Empty);
            throw new ChromeException(e.Exception.Message);
        }

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            if (_response.Task.Status != TaskStatus.RanToCompletion)
                _response.SetResult(string.Empty);
            Closed?.Invoke(this, e);
        }
        #endregion

        #region SendAsync
        /// <summary>
        /// Sends a message asynchronously to the <see cref="_webSocket"/>
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns></returns>
        internal async Task<string> SendAsync(Message message)
        {
            _messageId += 1;
            message.Id = _messageId;
            _response = new TaskCompletionSource<string>();
            OpenWebSocket();
            _webSocket.Send(message.ToJson());            
            return await _response.Task;
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

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (error.InnerError != null && error.InnerError.Code != 0 &&
                !string.IsNullOrEmpty(error.InnerError.Message))
                throw new ChromeException(error.InnerError.Message);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _webSocket.MessageReceived -= WebSocketOnMessageReceived;
            _webSocket.Error -= WebSocketOnError;
            _webSocket.Closed -= WebSocketOnClosed;

            if(_webSocket.State == WebSocketState.Open)
                _webSocket.Close();

            _webSocket.Dispose();
        }
        #endregion
    }
}
