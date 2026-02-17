//
// ChromeNavigationException.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2026 Magic-Sessions. (www.magic-sessions.com)
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
using System.Runtime.Serialization;

namespace ChromiumHtmlToPdfLib.Exceptions;

/// <summary>
///     Raised when an error is returned when navigation to a page in Chromium
/// </summary>
[Serializable]
public class ChromiumNavigationException : Exception
{
    /// <summary>
    ///     Raised when a navigation exception occurs in Chromium
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected ChromiumNavigationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    ///     Raised when a navigation exception occurs in Chromium
    /// </summary>
    internal ChromiumNavigationException()
    {
    }

    /// <summary>
    ///     Raised when a navigation exception occurs in Chromium
    /// </summary>
    /// <param name="message"></param>
    internal ChromiumNavigationException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Raised when a navigation exception occurs in Chromium
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    internal ChromiumNavigationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
