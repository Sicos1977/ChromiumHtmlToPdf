using System;
using System.Text;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib.Exceptions;
using ChromeHtmlToPdfLib.Protocol;
using WebSocketSharp;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// A connection to a page (tab) in Chrome
    /// </summary>
    internal class Connection
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
        public WebSocketSharp.WebSocket WebSocket { get; set; }
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
                EnableRedirection = true
            };

            WebSocket.OnMessage += Websocket_OnMessage;
            WebSocket.OnClose += Websocket_OnClose;
            WebSocket.OnError += Websocket_OnError;
            WebSocket.Connect();
        }
        #endregion

        private void Websocket_OnError(object sender, ErrorEventArgs e)
        {
            _response.SetResult(string.Empty);
            throw new ChromeException(e.Message, e.Exception);
        }

        private void Websocket_OnClose(object sender, CloseEventArgs e)
        {
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
            //var bytes = Encoding.UTF8.GetBytes(message.ToJson());
            WebSocket.Send(message.ToJson());
            //WebSocket.Send(bytes);
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

        #region Create
        /// <summary>
        /// Creates a new connection to a page (tab) in Chrome
        /// </summary>
        /// <param name="url">The url to the websocket</param>
        /// <returns></returns>
        internal static Connection Create(string url)
        {
            return new Connection(url);
        }
        #endregion
    }
}
