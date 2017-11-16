using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using ChromeHtmlToPdf.Protocol;
using ChromeHtmlToPdf.Settings;
using Newtonsoft.Json.Linq;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using WebSocket = WebSocket4Net.WebSocket;

namespace ChromeHtmlToPdf
{
    /// <summary>
    /// Handles all the communication task with Chrome remote devtools
    /// </summary>
    internal class Communicator : IDisposable
    {
        #region Fields
        /// <summary>
        /// The websocket need to communicate with Chrome
        /// </summary>
        private readonly WebSocket _webSocket;

        /// <summary>
        /// Used as a signel that we received a message on the <see cref="_webSocket"/>
        /// </summary>
        private ManualResetEvent _webSocketWaitEvent;

        /// <summary>
        /// The command that we are sending to Chrome
        /// </summary>
        private string _request;

        /// <summary>
        /// The response from Chrome
        /// </summary>
        private string _response;

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
            //_webSocket.MessageReceived += WebSocketMessageReceived;
            _webSocket.Error += WebSocketOnError;

            _webSocketWaitEvent = new ManualResetEvent(false);
            _webSocket.Opened += (sender, args) => { _webSocketWaitEvent.Set(); };
            _webSocket.Open();
            _webSocketWaitEvent.WaitOne();
        }

        ~Communicator()
        {
            Dispose();
        }
        #endregion

        #region WebSocket events
        /// <summary>
        /// Raised when a <see cref="_webSocket"/> error occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorEventArgs"></param>
        private void WebSocketOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            throw new WebSocketException($"An error occured while sending the JSON command '{_request}' to Chrome", errorEventArgs.Exception);
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
                        var streamReader = new StreamReader(responseStream ?? throw new InvalidOperationException());
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
        public void NavigateTo(Uri uri)
        {
            _webSocket.Send(new Message { Id = MessageId, Method = "Page.enable" }.ToJson());

            var localFile = uri.Scheme == "file";

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

                if (!localFile)
                {
                    if (page.Method == "Page.lifecycleEvent" && page.Params.Name == "DOMContentLoaded")
                        loaded = true;
                }
                else if (page.Method == "Page.loadEventFired")
                    loaded = true;
            };

            _webSocket.Send(message.ToJson());

            while (!loaded)
                Thread.Sleep(1);

            _webSocket.Send(new Message { Id = MessageId, Method = "Page.disable" }.ToJson());
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

            _webSocket.Send(message.ToJson());

            while (!converted)
                Thread.Sleep(1);

            return response;
        }
        #endregion

        #region SetPageEnable
        /// <summary>
        /// Let's Chrome know that we want to receive page event notifications
        /// </summary>
        /// <param name="value"></param>
        private void SetPageEnable(bool value)
        {
            var message = value ? new Message {Method = "Page.enable"} : new Message {Method = "Page.disable"};
            SendCommand(message.ToJson());
        }
        #endregion

        #region SendCommand
        /// <summary>
        /// Sends the given JSON <paramref name="request"/> to Chrome
        /// </summary>
        /// <param name="request">The JSON command</param>
        /// <returns>The response on the <paramref name="request"/></returns>
        /// <exception cref="WebSocketException">Thrown when an error occurs while communicating over websocket</exception>
        private string SendCommand(string request)
        {
            _webSocketWaitEvent = new ManualResetEvent(false);
            _request = request;
            _webSocket.Send(request);
            _webSocketWaitEvent.WaitOne();
            var temp = _response;
            _response = null;

            return temp;
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