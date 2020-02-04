//
// Error.cs
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

using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON structure that is returned from Chrome when an Error occurs
    /// </summary>
    public class Error : MessageBase
    {
        #region Properties
        /// <summary>
        /// <see cref="InnerError"/>
        /// </summary>
        [JsonProperty("error")]
        public ErrorInnerError InnerError { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static Error FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Error>(json);
        }
        #endregion
    }

    /// <summary>
    /// The inner error
    /// </summary>
    public class ErrorInnerError
    {
        #region Properties
        /// <summary>
        /// The error code
        /// </summary>
        [JsonProperty("code")]
        public double Code { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
        #endregion
    }
}

