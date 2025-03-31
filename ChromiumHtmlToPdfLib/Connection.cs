//
// Connection.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2025 Magic-Sessions. (www.magic-sessions.com)
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
using ChromiumHtmlToPdfLib.Helpers;
using ChromiumHtmlToPdfLib.Loggers;
using ChromiumHtmlToPdfLib.Protocol;
using ErrorEventArgs = ChromiumHtmlToPdfLib.Event.ErrorEventArgs;

namespace ChromiumHtmlToPdfLib;

/// <summary>
///     A connection to a page (tab) in Chromium
/// </summary>
#if (NETSTANDARD2_0)
public class Connection : IDisposable
#else
public class Connection : IDisposable, IAsyncDisposable
#endif
{
    #region Events
    /// <summary>
    ///     Triggered when a connection to the <see cref="_webSocket" /> is closed
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    ///     Triggered when a new message is received on the <see cref="_webSocket" />
    /// </summary>
    public event EventHandler<string>? MessageReceived;
    #endregion

    #region Fields
    /// <summary>
    ///     <see cref="Logger"/>
    /// </summary>
    private readonly Logger? _logger;

    private const int ReceiveBufferSize = 8192;

    /// <summary>
    ///     <see cref="ReceiveLoop"/>
    /// </summary>
    private readonly CancellationTokenSource _receiveLoopCts;

    /// <summary>
    ///     The url of the websocket
    /// </summary>
    private readonly Uri _url;

    /// <summary>
    ///     The current message id
    /// </summary>
    private int _messageId;

    /// <summary>
    ///     The websocket
    /// </summary>
    private readonly ClientWebSocket _webSocket;

    /// <summary>
    ///     Websocket operation timeout in milliseconds
    /// </summary>
    private readonly int _timeout;

    /// <summary>
    /// Task to await for <see cref="ReceiveLoop"/> completion.
    /// </summary>
    private Task<Task>? _receiveTask;

    /// <summary>
    ///     Keeps track is we already disposed our resources
    /// </summary>
    private bool _disposed;
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets all it's needed properties
    /// </summary>
    /// <param name="url">The url</param>
    /// <param name="timeout">Websocket open timeout in milliseconds</param>
    /// <param name="logger"><see cref="Logger"/></param>
    private Connection(string url, int timeout, Logger? logger)
    {
        _url = new Uri(url);
        _timeout = timeout;
        _logger = logger;
        _logger?.Info("Creating new websocket connection to url '{url}'", url);
        _webSocket = new ClientWebSocket();
        _receiveLoopCts = new CancellationTokenSource();
    }

    internal static async Task<Connection> Create(string url, int timeout, Logger? logger, CancellationToken cancellationToken)
    {
        var connection = new Connection(url, timeout, logger);
        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        await OpenWebSocketAsync(cancellationToken).ConfigureAwait(false);
        _receiveTask = Task.Factory.StartNew(ReceiveLoop, new ReceiveLoopState(_logger, _webSocket, OnMessageReceived, _receiveLoopCts.Token), _receiveLoopCts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    #endregion

    #region ReceiveLoop
    private sealed record ReceiveLoopState(Logger? Logger, ClientWebSocket WebSocket, Action<string> OnMessageReceived, CancellationToken Token);

    private static async Task ReceiveLoop(object? stateData)
    {
        var state = (ReceiveLoopState)stateData!;

        MemoryStream? outputStream = null;
        var buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);

        try
        {
            while (!state.Token.IsCancellationRequested)
            {
                outputStream = new MemoryStream(ReceiveBufferSize);
                WebSocketReceiveResult receiveResult;

                do
                {
                    receiveResult = await state.WebSocket.ReceiveAsync(buffer, state.Token).ConfigureAwait(false);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) continue;
                    if (buffer.Array != null)
                        outputStream.Write(buffer.Array, 0, receiveResult.Count);
                } while (!receiveResult.EndOfMessage);

                if (receiveResult.MessageType == WebSocketMessageType.Close) break;

                outputStream.Position = 0;
                using var reader = new StreamReader(outputStream);
                var response = await reader.ReadToEndAsync().ConfigureAwait(false);

                WebSocketOnMessageReceived(state.Logger, state.OnMessageReceived, new MessageReceivedEventArgs(response));
            }
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (WebSocketException webSocketException) when (webSocketException.Message == "The remote party closed the WebSocket connection without completing the close handshake.")
        {
            // Ignore
        }
        catch (Exception exception)
        {
            WebSocketOnError(state.Logger, new ErrorEventArgs(exception));
        }
        finally
        {
            if (outputStream != null)
#if (NETSTANDARD2_0)
                outputStream.Dispose();
#else
                await outputStream.DisposeAsync().ConfigureAwait(false);
#endif
        }
    }
    #endregion

    #region OpenWebSocket
    private async Task OpenWebSocketAsync(CancellationToken cancellationToken)
    {
        if (_webSocket.State is WebSocketState.Open or WebSocketState.Connecting) return;

        _logger?.Info("Opening websocket connection with a timeout of {timeout} milliseconds", _timeout);

        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        await _webSocket.ConnectAsync(_url, linkedCts.Token).ConfigureAwait(false);
        _logger?.Info("Websocket opened");
    }
    #endregion

    #region Websocket events
    private static void WebSocketOnMessageReceived(Logger? logger, Action<string> onMessageReceived, MessageReceivedEventArgs e)
    {
        var response = e.Message;

        var error = CheckForError(response);

        if (!string.IsNullOrEmpty(error))
            logger?.Error("Chrome returned error: {error}", error);

        onMessageReceived(response);
    }

    private void OnMessageReceived(string response)
    {
        MessageReceived?.Invoke(this, response);
    }

    private static void WebSocketOnError(Logger? logger, ErrorEventArgs e)
    {
        logger?.Error(e.Exception, "WebSocket operation failed with {exception}", ExceptionHelpers.GetInnerException(e.Exception));
    }

    private void WebSocketOnClosed(EventArgs e)
    {
        Closed?.Invoke(this, e);
    }
    #endregion

    #region SendForResponseAsync
    /// <summary>
    ///     Sends a message asynchronously to the <see cref="_webSocket" /> and returns response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Response given by <see cref="_webSocket" /></returns>
    internal async Task<string> SendForResponseAsync(Message message, CancellationToken cancellationToken)
    {
        _messageId += 1;
        message.Id = _messageId;

        var tcs = new TaskCompletionSource<string>();

        var receivedHandler = new EventHandler<string>((_, data) =>
        {
            var messageBase = MessageBase.FromJson(data);
            if (messageBase.Id == message.Id)
                tcs.SetResult(data);
        });

        MessageReceived += receivedHandler;

        try
        {
            using var timeoutCts = new CancellationTokenSource(_timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await _webSocket.SendAsync(MessageToBytes(message), WebSocketMessageType.Text, true, linkedCts.Token).ConfigureAwait(false);

            tcs.Task.Wait(cancellationToken);

            return tcs.Task.Result;
        }
        catch (Exception exception)
        {
            WebSocketOnError(_logger, new ErrorEventArgs(exception));
            throw;
        }
        finally
        {
            MessageReceived -= receivedHandler;
        }
    }
    #endregion

    #region SendAsync
    /// <summary>
    ///     Sends a message to the <see cref="_webSocket" /> and awaits no response
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <param name="cancellationToken">The message to send</param>
    /// <returns></returns>
    internal async Task SendAsync(Message message, CancellationToken cancellationToken)
    {
        _messageId += 1;
        message.Id = _messageId;

        try
        {
            using var timeoutCts = new CancellationTokenSource(_timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await _webSocket.SendAsync(MessageToBytes(message), WebSocketMessageType.Text, true, linkedCts.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            WebSocketOnError(_logger, new ErrorEventArgs(exception));
        }
    }
    #endregion

    #region CheckForError
    /// <summary>
    ///     Checks if <paramref name="message" /> contains an error and if so returns this error, <c>null</c> otherwise
    /// </summary>
    /// <param name="message"></param>
    private static string? CheckForError(string message)
    {
        var error = Error.FromJson(message);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (error.InnerError is { Code: not 0 } &&
            !string.IsNullOrEmpty(error.InnerError.Message))
            return error.InnerError.Message;

        return null;
    }
    #endregion

    #region MessageToBytes
    private ArraySegment<byte> MessageToBytes(MessageBase message)
    {
        return new ArraySegment<byte>(Encoding.UTF8.GetBytes(message.ToJson()));
    }
    #endregion

    #region InternalDisposeAsync
    /// <summary>
    ///     Closes the websocket connection
    /// </summary>
    /// <returns></returns>
    public async Task InternalDisposeAsync()
    {
        if (_disposed)
            return;

        _receiveLoopCts.Cancel();

        if (_receiveTask != null)
            await (await _receiveTask.ConfigureAwait(false)).ConfigureAwait(false);

        _receiveLoopCts.Dispose();

        _logger?.Info("Disposing websocket connection to url '{url}'", _url);

        if (_webSocket.State == WebSocketState.Open)
        {
            _logger?.Info("Closing web socket");

            try
            {
                using var timeoutCts = new CancellationTokenSource(5000);
                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", timeoutCts.Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger?.Error(exception, "An error occurred while closing the web socket, error: '{error}'", ExceptionHelpers.GetInnerException(exception));
            }

            _logger?.Info("Websocket connection closed");

            WebSocketOnClosed(EventArgs.Empty);
            _webSocket.Dispose();
            _logger?.Info("Web socket connection disposed");
        }

        _disposed = true;
    }
    #endregion

    #region Dispose
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        InternalDisposeAsync().GetAwaiter().GetResult();
    }
    #endregion

    #region DisposeAsync
#if (!NETSTANDARD2_0)
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await InternalDisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
#endif
    #endregion
}
