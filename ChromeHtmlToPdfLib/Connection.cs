//
// Connection.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2014-2019 Magic-Sessions. (www.magic-sessions.com)
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
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Protocol;
using WebSocketSharp;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// A connection to a page (tab) in Chrome
    /// </summary>
    internal class Connection : IDisposable
    {
        #region Events
        /// <summary>
        /// Triggered when a connection to the <see cref="WebSocket"/> is closed
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Triggered when a new message is received on the <see cref="WebSocket"/>
        /// </summary>
        public event EventHandler<string> MessageReceived;
        #endregion

        #region Fields
        private int _messageId;
        private TaskCompletionSource<string> _response;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the websocket url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The websocket
        /// </summary>
        public WebSocket WebSocket { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Makes this object and sets all it's needed properties
        /// </summary>
        /// <param name="url">The url</param>
        internal Connection(string url)
        {
            Url = url;
            WebSocket = new WebSocket(url)
            {
                EmitOnPing = false,
                EnableRedirection = true,
                Log = {Output = (_, __) => { }}
            };

            WebSocket.OnMessage += Websocket_OnMessage;
            WebSocket.OnClose += Websocket_OnClose;
            WebSocket.OnError += Websocket_OnError;
            WebSocket.Connect();
        }
        #endregion

        #region Websocket events
        private void Websocket_OnError(object sender, ErrorEventArgs e)
        {
            if (_response.Task.Status != TaskStatus.RanToCompletion)
                _response.SetResult(string.Empty);
            throw new ChromeException(e.Message, e.Exception);
        }

        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
            if (_response.Task.Status != TaskStatus.RanToCompletion)
                _response.SetResult(string.Empty);
            Closed?.Invoke(this, e);
        }

        private void Websocket_OnMessage(object sender, MessageEventArgs e)
        {
            var response = e.Data;

            CheckForError(response);

            var messageBase = MessageBase.FromJson(response);

            if (_messageId == messageBase.Id)
                _response.SetResult(response);

            MessageReceived?.Invoke(this, response);
        }
        #endregion

        #region SendAsync
        /// <summary>
        /// Sends a message asynchronously to the <see cref="WebSocket"/>
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns></returns>
        internal async Task<string> SendAsync(Message message)
        {
            _messageId += 1;
            message.Id = _messageId;
            _response = new TaskCompletionSource<string>();
            WebSocket.Send(message.ToJson());
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
            if (error.InnerError != null && error.InnerError.Code != 0)
                throw new ChromeException(error.InnerError.Message);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            WebSocket.OnMessage -= Websocket_OnMessage;
            WebSocket.OnClose -= Websocket_OnClose;
            WebSocket.OnError -= Websocket_OnError;

            //if (WebSocket.ReadyState == WebSocketState.Open)
            //    WebSocket.Close();
        }
        #endregion
    }
}
