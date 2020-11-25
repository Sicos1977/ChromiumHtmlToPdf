//
// ConvertUri.cs
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
using System.Runtime.Serialization;
using System.Text;
// ReSharper disable UnusedMember.Global

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// <inheritdoc cref="Uri"/>
    /// </summary>
    public class ConvertUri : Uri
    {
        /// <summary>
        ///     The encoding of the file that will be converted
        /// </summary>
        public Encoding Encoding { get; }

        public ConvertUri(string uriString) : base(uriString)
        {
        }

        /// <summary>
        ///     Sets the uri and encoding
        /// </summary>
        /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
        /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
        public ConvertUri(string uriString, Encoding encoding) : base(uriString)
        {
            Encoding = encoding;
        }

        /// <summary>
        ///     Sets the uri and encoding
        /// </summary>
        /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
        /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
        public ConvertUri(string uriString, string encoding) : base(uriString)
        {
            Encoding = !string.IsNullOrWhiteSpace(encoding)
                ? Encoding.GetEncoding(encoding)
                : null;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public ConvertUri(string uriString, bool dontEscape) : base(uriString, dontEscape)
        {
        }

        public ConvertUri(Uri baseUri, string relativeUri, bool dontEscape) : base(baseUri, relativeUri, dontEscape)
        {
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public ConvertUri(string uriString, UriKind uriKind) : base(uriString, uriKind)
        {
        }

        public ConvertUri(Uri baseUri, string relativeUri) : base(baseUri, relativeUri)
        {
        }

        public ConvertUri(Uri baseUri, Uri relativeUri) : base(baseUri, relativeUri)
        {
        }

        protected ConvertUri(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
