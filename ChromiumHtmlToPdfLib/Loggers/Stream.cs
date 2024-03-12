//
// Stream.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2024 Magic-Sessions. (www.magic-sessions.com)
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
using System.Text;
using Microsoft.Extensions.Logging;

namespace ChromiumHtmlToPdfLib.Loggers;

/// <summary>
///     Writes log information to a stream
/// </summary>
public class Stream : ILogger, IDisposable
{
    #region Fields
    private System.IO.Stream? _stream;
    private string? _instanceId;
    #endregion

    #region Constructors
    /// <summary>
    ///     Logs information to the given <paramref name="stream" />
    /// </summary>
    /// <param name="stream"></param>
    public Stream(System.IO.Stream stream)
    {
        _stream = stream;
    }
    #endregion

    #region BeginScope
    /// <summary>
    ///     Beginning of the logging scope
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="state"></param>
    /// <returns></returns>
    public IDisposable? BeginScope<TState>(TState state)
         where TState : notnull
    {
        _instanceId = state.ToString();
        return null;
    }
    #endregion

    #region IsEnabled
    /// <summary>
    ///     Will always return <c>true</c>
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }
    #endregion

    #region Log
    /// <summary>
    ///     Write logging to the <see cref="ILogger" />
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = $"{formatter(state, exception)}";

        if (_stream == null || !_stream.CanWrite) return;
        var line =
            $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff}{(_instanceId != null ? $" - {_instanceId}" : string.Empty)} - {message}{Environment.NewLine}";
        var bytes = Encoding.UTF8.GetBytes(line);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.Flush();
    }
    #endregion

    #region Dispose
    /// <summary>
    ///     Disposes this object
    /// </summary>
    public void Dispose()
    {
        if (_stream == null)
            return;

        _stream.Dispose();
        _stream = null;
    }
    #endregion
}