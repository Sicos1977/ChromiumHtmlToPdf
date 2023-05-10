//
// RequestWillBeSentExtraInfo.cs
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

using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChromiumHtmlToPdfLib.Protocol.Network
{
    internal class RequestWillBeSentExtraInfo : Base
    {
        #region Properties
        [JsonProperty("params")]
        public RequestWillBeSentExtraInfoParams Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static RequestWillBeSentExtraInfo FromJson(string json) => JsonConvert.DeserializeObject<RequestWillBeSentExtraInfo>(json, RequestWillBeSentExtraInfoConverter.Settings);
        #endregion
    }

    internal class RequestWillBeSentExtraInfoParams
    {
        #region Properties
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("associatedCookies")]
        public List<object> AssociatedCookies { get; set; }

        [JsonProperty("headers")]
        public RequestWillBeSentExtraInfoHeaders Headers { get; set; }

        [JsonProperty("clientSecurityState")]
        public RequestWillBeSentExtraInfoClientSecurityState ClientSecurityState { get; set; }
        #endregion
    }

    internal class RequestWillBeSentExtraInfoClientSecurityState
    {
        #region Properties
        [JsonProperty("initiatorIsSecureContext")]
        public bool InitiatorIsSecureContext { get; set; }

        [JsonProperty("initiatorIPAddressSpace")]
        public string InitiatorIpAddressSpace { get; set; }

        [JsonProperty("privateNetworkRequestPolicy")]
        public string PrivateNetworkRequestPolicy { get; set; }
        #endregion
    }

    internal class RequestWillBeSentExtraInfoHeaders
    {
        #region Properties
        [JsonProperty(":method")]
        public string Method { get; set; }

        [JsonProperty(":authority")]
        public string Authority { get; set; }

        [JsonProperty(":scheme")]
        public string Scheme { get; set; }

        [JsonProperty(":path")]
        public string Path { get; set; }

        [JsonProperty("user-agent")]
        public string UserAgent { get; set; }

        [JsonProperty("accept")]
        public string Accept { get; set; }

        [JsonProperty("sec-fetch-site")]
        public string SecFetchSite { get; set; }

        [JsonProperty("sec-fetch-mode")]
        public string SecFetchMode { get; set; }

        [JsonProperty("sec-fetch-dest")]
        public string SecFetchDest { get; set; }

        [JsonProperty("accept-encoding")]
        public string AcceptEncoding { get; set; }

        [JsonProperty("accept-language")]
        public string AcceptLanguage { get; set; }
        #endregion
    }

    #region Static class RequestWillBeSentExtraInfoConverter
    internal static class RequestWillBeSentExtraInfoConverter
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
