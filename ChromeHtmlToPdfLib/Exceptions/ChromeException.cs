//
// ChromeException.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2019 Magic-Sessions. (www.magic-sessions.com)
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
using ChromeHtmlToPdfLib.Protocol;

namespace ChromeHtmlToPdfLib.Exceptions
{
    /// <summary>
    ///     Raised when an error is returned from Chrome
    /// </summary>
    [Serializable]
    public class ChromeException : Exception
    {
        /// <summary>
        ///     Returns the error code that is returned from Chrome
        /// </summary>
        public double Code { get; }

        /// <summary>
        ///     Raised when an error is returned from Chrome
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ChromeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        ///     Raised when an error is returned from Chrome
        /// </summary>
        public ChromeException() { }

        /// <summary>
        ///     Raised when an error is returned from Chrome
        /// </summary>
        /// <param name="error"></param>
        public ChromeException(Error error) : base(error.InnerError.Message)
        {
            Code = error.InnerError.Code;
        }

        /// <summary>
        ///     Raised when an error is returned from Chrome
        /// </summary>
        /// <param name="message"></param>
        public ChromeException(string message) : base(message) { }

        /// <summary>
        ///     Raised when an error is returned from Chrome
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ChromeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
