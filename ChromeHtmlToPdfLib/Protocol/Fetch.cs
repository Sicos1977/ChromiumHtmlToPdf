//
// Fetch.cs
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


using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned when the <b>Fetch.enable</b> is activated in Chrome
    /// </summary>
    public class Fetch
    {
        #region Properties
        [JsonProperty("method")] 
        public string Method { get; set; }

        [JsonProperty("params")] 
        public FetchParams Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Fetch FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Fetch>(json);
        }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="Fetch"/> class
    /// </summary>
    public class FetchParams
    {
        #region Properties
        [JsonProperty("requestId")] 
        public string RequestId { get; set; }

        [JsonProperty("request")] 
        public FetchRequest Request { get; set; }

        [JsonProperty("frameId")] 
        public string FrameId { get; set; }

        [JsonProperty("resourceType")] 
        public string ResourceType { get; set; }

        [JsonProperty("networkId")] 
        public string NetworkId { get; set; }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="Fetch"/> class
    /// </summary>
    public class FetchRequest
    {
        #region Properties
        [JsonProperty("url")] 
        public string Url { get; set; }

        [JsonProperty("method")] 
        public string Method { get; set; }

        [JsonProperty("headers")] 
        public FetchHeaders Headers { get; set; }

        [JsonProperty("initialPriority")] 
        public string InitialPriority { get; set; }

        [JsonProperty("referrerPolicy")] 
        public string ReferrerPolicy { get; set; }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="FetchRequest"/> class
    /// </summary>
    public class FetchHeaders
    {
        #region Properties
        [JsonProperty("Accept")] 
        public string Accept { get; set; }
        #endregion
    }
}