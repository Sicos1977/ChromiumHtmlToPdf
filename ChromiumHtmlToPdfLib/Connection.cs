//
// Connection.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2023 Magic-Sessions. (www.magic-sessions.com)
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
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChromiumHtmlToPdfLib.Event;
using ChromiumHtmlToPdfLib.Exceptions;
using ChromiumHtmlToPdfLib.Helpers;
using ChromiumHtmlToPdfLib.Protocol;
using Microsoft.Extensions.Logging;
using ErrorEventArgs = ChromiumHtmlToPdfLib.Event.ErrorEventArgs;

namespace ChromiumHtmlToPdfLib;

/// <summary>
///     A connection to a page (tab) in Chromium
/// </summary>
internal class Connection : IDisposable
{
    #region Events
    /// <summary>
    ///     Triggered when a connection to the <see cref="_webSocket" /> is closed
    /// </summary>
    public event EventHandler Closed;

    /// <summary>
    ///     Triggered when a new message is received on the <see cref="_webSocket" />
    /// </summary>
    public event EventHandler<string> MessageReceived;

    /// <summary>
    ///     Triggered when a <see cref="_webSocket" /> error occurs
    /// </summary>
    public event EventHandler<string> OnError;
    #endregion

    #region Fields
    private const int ReceiveBufferSize = 8192;
    private readonly CancellationTokenSource _receiveLoopCts;

    /// <summary>
    ///     Used to make the logging thread safe
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    ///     When set then logging is written to this ILogger instance
    /// </summary>
    private readonly ILogger _logger;

    private readonly string _url;
    private int _messageId;
    private TaskCompletionSource<string> _response;

    /// <summary>
    ///     The websocket
    /// </summary>
    private readonly ClientWebSocket _webSocket;
    #endregion

    #region Properties
    /// <summary>
    ///     An unique id that can be used to identify the logging of the converter when
    ///     calling the code from multiple threads and writing all the logging to the same file
    /// </summary>
    public string InstanceId { get; set; }
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets all it's needed properties
    /// </summary>
    /// <param name="url">The url</param>
    /// <param name="logger">
    ///     When set then logging is written to this ILogger instance for all conversions at the Information
    ///     log level
    /// </param>
    internal Connection(string url, ILogger logger)
    {
        _url = url;
        _logger = logger;
        WriteToLog($"Creating new websocket connection to url '{url}'");
        _webSocket = new ClientWebSocket();
        _receiveLoopCts = new CancellationTokenSource();
        OpenWebSocket();
        Task.Factory.StartNew(ReceiveLoop, _receiveLoopCts.Token, TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }
    #endregion

    #region ReceiveLoop
    private async Task ReceiveLoop()
    {
        var loopToken = _receiveLoopCts.Token;
        MemoryStream outputStream = null;
        var buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);
        try
        {
            while (!loopToken.IsCancellationRequested)
            {
                outputStream = new MemoryStream(ReceiveBufferSize);
                WebSocketReceiveResult receiveResult;
                do
                {
                    receiveResult = await _webSocket.ReceiveAsync(buffer, _receiveLoopCts.Token);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) continue;
                    if (buffer.Array != null)
                        outputStream.Write(buffer.Array, 0, receiveResult.Count);
                } while (!receiveResult.EndOfMessage);

                if (receiveResult.MessageType == WebSocketMessageType.Close) break;

                outputStream.Position = 0;
                string response;
                using (var reader = new StreamReader(outputStream))
                {
                    response = await reader.ReadToEndAsync();
                }

                WebSocketOnMessageReceived(new MessageReceivedEventArgs(response));
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            WebSocketOnError(new ErrorEventArgs(e));
        }
        finally
        {
            outputStream?.Dispose();
        }
    }
    #endregion

    #region OpenWebSocket
    private void OpenWebSocket()
    {
        OpenWebSocketAsync().GetAwaiter().GetResult();
    }

    private async Task OpenWebSocketAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State == WebSocketState.Open) return;

        WriteToLog("Opening websocket connection with a timeout of 30 seconds");

        try
        {
            await _webSocket.ConnectAsync(new Uri(_url), cancellationToken);
        }
        catch (Exception exception)
        {
            WebSocketOnError(new ErrorEventArgs(exception));
        }

        var i = 0;

        while (_webSocket.State != WebSocketState.Open)
        {
            Thread.Sleep(1);
            i++;

            if (i != 30000) continue;
            var message = $"Websocket connection timed out after 30 seconds with the state '{_webSocket.State}'";
            WriteToLog(message);
            throw new ChromiumException(message);
        }

        WriteToLog("Websocket opened");
    }
    #endregion

    #region Websocket events
    private void WebSocketOnMessageReceived(MessageReceivedEventArgs e)
    {
        var response = e.Message;

        var error = CheckForError(response);
        if (!string.IsNullOrEmpty(error))
            OnError?.Invoke(this, error);

        var messageBase = MessageBase.FromJson(response);

        if (_messageId == messageBase.Id)
            _response?.SetResult(response);

        MessageReceived?.Invoke(this, response);
    }

    private void WebSocketOnError(ErrorEventArgs e)
    {
        if (_response?.Task.Status != TaskStatus.RanToCompletion)
            _response?.SetResult(string.Empty);

        OnError?.Invoke(this, ExceptionHelpers.GetInnerException(e.Exception));
    }

    private void WebSocketOnClosed(EventArgs e)
    {
        if (_response?.Task.Status != TaskStatus.RanToCompletion)
            _response?.SetResult(string.Empty);

        Closed?.Invoke(this, e);
    }
    #endregion

    #region SendAsync
    /// <summary>
    ///     Sends a message asynchronously to the <see cref="_webSocket" /> and returns response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Response given by <see cref="_webSocket" /></returns>
    internal async Task<string> SendForResponseAsync(Message message, CancellationToken cancellationToken = default)
    {
        _messageId += 1;
        message.Id = _messageId;
        await OpenWebSocketAsync(cancellationToken);

        var tcs = new TaskCompletionSource<string>();
        var receivedHandler = new EventHandler<string>((sender, data) =>
        {
            var messageBase = MessageBase.FromJson(data);
            if (messageBase.Id == message.Id) tcs.SetResult(data);
        });

        MessageReceived += receivedHandler;

        try
        {
            await _webSocket.SendAsync(MessageToBytes(message), WebSocketMessageType.Text, true, default);
        }
        catch (Exception e)
        {
            WebSocketOnError(new ErrorEventArgs(e));
        }

        var response = tcs.Task.Result;

        MessageReceived -= receivedHandler;

        return response;
    }

    /// <summary>
    ///     Sends a message to the <see cref="_webSocket" /> and awaits no response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    internal async Task SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        _messageId += 1;
        message.Id = _messageId;
        _response = null;
        await OpenWebSocketAsync(cancellationToken);

        try
        {
            await _webSocket.SendAsync(MessageToBytes(message), WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (Exception e)
        {
            WebSocketOnError(new ErrorEventArgs(e));
        }
    }
    #endregion

    #region CheckForError
    /// <summary>
    ///     Checks if <paramref name="message" /> contains an error and if so raises an exception
    /// </summary>
    /// <param name="message"></param>
    private string CheckForError(string message)
    {
        var error = Error.FromJson(message);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (error.InnerError != null && error.InnerError.Code != 0 &&
            !string.IsNullOrEmpty(error.InnerError.Message))
            return error.InnerError.Message;

        return null;
    }
    #endregion

    #region WriteToLog
    /// <summary>
    ///     Writes a line to the <see cref="_logger" />
    /// </summary>
    /// <param name="message">The message to write</param>
    internal void WriteToLog(string message)
    {
        lock (_lock)
        {
            try
            {
                if (_logger == null) return;
                using (_logger.BeginScope(InstanceId))
                {
                    _logger.LogInformation(message);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }
    }
    #endregion

    #region Dispose
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        WriteToLog($"Disposing websocket connection to url '{_url}'");

        try
        {
            WebSocketOnClosed(EventArgs.Empty);
        }
        catch (Exception exception)
        {
            WriteToLog($"Exception while disposing websocket connection to url '{_url}': {exception.Message}");
        }

        if (_webSocket.State == WebSocketState.Open)
        {
            WriteToLog("Closing websocket");
            _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", default);
            _receiveLoopCts.Cancel();
        }

        _webSocket.Dispose();
        WriteToLog("Websocket connection disposed");
    }
    #endregion

    #region MessageToBytes
    private static ArraySegment<byte> MessageToBytes(MessageBase message)
    {
        return new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.ToJson()));
    }
    #endregion
}