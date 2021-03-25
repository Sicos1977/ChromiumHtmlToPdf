//
// ResponseReceived.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2021 Magic-Sessions. (www.magic-sessions.com)
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

namespace ChromeHtmlToPdfLib.Protocol.Network
{
    public class ResponseReceived : Base
    {
        #region Properties
        [JsonProperty("params")]
        public ResponseReceivedParams Params { get; set; }
        #endregion
        
        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static ResponseReceived FromJson(string json) => JsonConvert.DeserializeObject<ResponseReceived>(json, ResponseReceivedConverter.Settings);
        #endregion
    }

    public class ResponseReceivedParams
    {
        #region Properties
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("loaderId")]
        public string LoaderId { get; set; }

        [JsonProperty("timestamp")]
        public double Timestamp { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("response")]
        public ResponseReceivedResponse Response { get; set; }

        [JsonProperty("frameId")]
        public string FrameId { get; set; }
        #endregion
    }

    public class ResponseReceivedResponse
    {
        #region Properties
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("statusText")]
        public string StatusText { get; set; }

        [JsonProperty("headers")]
        public ResponseReceivedHeaders Headers { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("connectionReused")]
        public bool ConnectionReused { get; set; }

        [JsonProperty("connectionId")]
        public long ConnectionId { get; set; }

        [JsonProperty("remoteIPAddress")]
        public string RemoteIpAddress { get; set; }

        [JsonProperty("remotePort")]
        public long RemotePort { get; set; }

        [JsonProperty("fromDiskCache")]
        public bool FromDiskCache { get; set; }

        [JsonProperty("fromServiceWorker")]
        public bool FromServiceWorker { get; set; }

        [JsonProperty("fromPrefetchCache")]
        public bool FromPrefetchCache { get; set; }

        [JsonProperty("encodedDataLength")]
        public long EncodedDataLength { get; set; }

        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("securityState")]
        public string SecurityState { get; set; }
        #endregion
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ResponseReceivedHeaders
    {
    }

    #region Static class ResponseReceivedConverter
    internal static class ResponseReceivedConverter
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
