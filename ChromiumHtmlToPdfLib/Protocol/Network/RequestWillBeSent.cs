//
// RequestWillBeSent.cs
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

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChromiumHtmlToPdfLib.Protocol.Network
{
    internal class RequestWillBeSent : Base
    {
        #region Properties
        [JsonProperty("params")]
        public RequestWillBeSentParams Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static RequestWillBeSent FromJson(string json) => JsonConvert.DeserializeObject<RequestWillBeSent>(json, RequestWillBeSentConverter.Settings);
        #endregion
    }

    internal class RequestWillBeSentParams
    {
        #region Properties
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("loaderId")]
        public string LoaderId { get; set; }

        [JsonProperty("documentURL")]
        public string DocumentUrl { get; set; }

        [JsonProperty("request")]
        public RequestWillBeSentRequest Request { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }

        [JsonProperty("wallTime")]
        public double WallTime { get; set; }

        [JsonProperty("initiator")]
        public WillBeSentInitiator Initiator { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("frameId")]
        public string FrameId { get; set; }

        [JsonProperty("hasUserGesture")]
        public bool HasUserGesture { get; set; }
        #endregion
    }

    internal class WillBeSentInitiator
    {
        #region Properties
        [JsonProperty("type")]
        public string Type { get; set; }
        #endregion
    }

    internal class RequestWillBeSentRequest
    {
        #region Properties
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("headers")]
        public RequestWillBeSentHeaders Headers { get; set; }

        [JsonProperty("mixedContentType")]
        public string MixedContentType { get; set; }

        [JsonProperty("initialPriority")]
        public string InitialPriority { get; set; }

        [JsonProperty("referrerPolicy")]
        public string ReferrerPolicy { get; set; }
        #endregion
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class RequestWillBeSentHeaders
    {
    }

    #region Static class RequestWillBeSentConverter
    internal static class RequestWillBeSentConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}}
        };
    }
    #endregion
}
