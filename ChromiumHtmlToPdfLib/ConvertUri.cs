//
// ConvertUri.cs
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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

// ReSharper disable UnusedMember.Global

namespace ChromiumHtmlToPdfLib;

/// <summary>
///     <inheritdoc cref="Uri" />
/// </summary>
public class ConvertUri : Uri
{
    #region Properties
    /// <summary>
    ///     The encoding of the file that will be converted
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    ///     The request headers to sent
    /// </summary>
    public Dictionary<string, string> RequestHeaders { get; }
    #endregion

    #region Constructor
    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="uriString"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(string uriString, Dictionary<string, string> requestHeaders = null) : base(uriString)
    {
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     Sets the uri and encoding
    /// </summary>
    /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
    /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(string uriString, Encoding encoding, Dictionary<string, string> requestHeaders = null) : base(uriString)
    {
        Encoding = encoding;
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     Sets the uri and encoding
    /// </summary>
    /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
    /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(string uriString, string encoding, Dictionary<string, string> requestHeaders = null) : base(uriString)
    {
        Encoding = !string.IsNullOrWhiteSpace(encoding)
            ? Encoding.GetEncoding(encoding)
            : null;

        RequestHeaders = requestHeaders;
    }

#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="uriString"></param>
    /// <param name="dontEscape"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(string uriString, bool dontEscape, Dictionary<string, string> requestHeaders = null) : base(uriString, dontEscape)
    {
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="relativeUri"></param>
    /// <param name="dontEscape"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(Uri baseUri, string relativeUri, bool dontEscape, Dictionary<string, string> requestHeaders = null) : base(baseUri, relativeUri, dontEscape)
    {
        RequestHeaders = requestHeaders;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="uriString"></param>
    /// <param name="uriKind"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(string uriString, UriKind uriKind, Dictionary<string, string> requestHeaders = null) : base(uriString, uriKind)
    {
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="relativeUri"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(Uri baseUri, string relativeUri, Dictionary<string, string> requestHeaders = null) : base(baseUri, relativeUri)
    {
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="relativeUri"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    public ConvertUri(Uri baseUri, Uri relativeUri, Dictionary<string, string> requestHeaders = null) : base(baseUri, relativeUri)
    {
        RequestHeaders = requestHeaders;
    }

    /// <summary>
    ///     The uri to converter
    /// </summary>
    /// <param name="serializationInfo"></param>
    /// <param name="streamingContext"></param>
    /// <param name="requestHeaders">The request headers to sent</param>
    protected ConvertUri(SerializationInfo serializationInfo, StreamingContext streamingContext, Dictionary<string, string> requestHeaders = null) : base(serializationInfo, streamingContext)
    {
        RequestHeaders = requestHeaders;
    }
    #endregion
}