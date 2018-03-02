using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Protocol;

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

        /// <summary>
        /// <c>true</c> when the <see cref="WebSocket"/> is closed
        /// </summary>
        private bool _closed;

        /// <summary>
        /// Stores the response from the close connection task
        /// </summary>
        private readonly TaskCompletionSource<bool> _connectionCloseTask;

        private bool _disposed;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the websocket url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Returns the delay to connect
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// The websocket
        /// </summary>
        public WebSocket WebSocket { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Makes this object and sets all it's needed properties
        /// </summary>
        /// <param name="url">The url to the <paramref name="webSocket"/></param>
        /// <param name="webSocket"></param>
        internal Connection(string url, WebSocket webSocket)
        {
            Url = url;
            WebSocket = webSocket;
            _connectionCloseTask = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(GetResponse);
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
            var encoded = Encoding.UTF8.GetBytes(message.ToJson());
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            _response = new TaskCompletionSource<string>();
            await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, default(CancellationToken));
            return await _response.Task;
        }
        #endregion

        #region Close
        private void Close()
        {
            _closed = true;
            _connectionCloseTask.SetResult(true);
            Closed?.Invoke(this, new EventArgs());
        }
        #endregion

        #region GetResponse
        /// <summary>
        /// Starts listening the socket
        /// </summary>
        /// <returns>The task that listens to the <see cref="WebSocket"/></returns>
        private void GetResponse()
        {
            var buffer = new byte[2048];

            while (true)
            {
                if (_closed)
                    return;

                var endOfMessage = false;
                var response = string.Empty;

                while (!endOfMessage)
                {
                    var socketTask = WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    Task.WhenAny(_connectionCloseTask.Task, socketTask).GetAwaiter();

                    if (_closed)
                        return;

                    var result = socketTask.Result;
 
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            response += Encoding.UTF8.GetString(buffer, 0, result.Count);
                            break;

                        case WebSocketMessageType.Close:
                            Close();
                            return;
                    }

                    endOfMessage = result.EndOfMessage;
                }
                
                if (string.IsNullOrEmpty(response)) continue;

                //System.IO.File.AppendAllText("d:\\tracekees.txt", response + Environment.NewLine);

                CheckForError(response);

                var messageBase = MessageBase.FromJson(response);

                if (_messageId == messageBase.Id)
                    _response.SetResult(response);

                MessageReceived?.Invoke(this, response);
            }
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

        #region Create
        /// <summary>
        /// Creates a new connection to a page (tab) in Chrome
        /// </summary>
        /// <param name="url">The url to the websocket</param>
        /// <param name="keepAliveInterval"></param>
        /// <returns></returns>
        internal static async Task<Connection> Create(string url, int keepAliveInterval = 10)
        {
            var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.KeepAliveInterval = new TimeSpan(0, 0, keepAliveInterval);
            await clientWebSocket.ConnectAsync(new Uri(url), default(CancellationToken)).ConfigureAwait(false);
            return new Connection(url, clientWebSocket);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Disposed this object
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            Close();
            WebSocket.Dispose();
            _disposed = true;
        }
        #endregion
    }
}
