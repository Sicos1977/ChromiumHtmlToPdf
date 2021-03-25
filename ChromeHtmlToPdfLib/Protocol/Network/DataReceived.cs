//
// DataReceived.cs
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

using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChromeHtmlToPdfLib.Protocol.Network
{
    public class DataReceived : Base
    {
        #region Properties
        [JsonProperty("params")]
        public DataReceivedParams Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static DataReceived FromJson(string json) => JsonConvert.DeserializeObject<DataReceived>(json, ReceivedConverter.Settings);
        #endregion
    }

    public class DataReceivedParams
    {
        #region Properties
        [JsonProperty("frame")]
        public ReceivedFrame Frame { get; set; }
        #endregion
    }

    public class ReceivedFrame
    {
        #region Properties
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("loaderId")]
        public string LoaderId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("domainAndRegistry")]
        public string DomainAndRegistry { get; set; }

        [JsonProperty("securityOrigin")]
        public string SecurityOrigin { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("adFrameType")]
        public string AdFrameType { get; set; }

        [JsonProperty("secureContextType")]
        public string SecureContextType { get; set; }

        [JsonProperty("crossOriginIsolatedContextType")]
        public string CrossOriginIsolatedContextType { get; set; }

        [JsonProperty("gatedAPIFeatures")]
        public List<string> GatedApiFeatures { get; set; }
        #endregion
    }

    #region Static class ReceivedConverter
    internal static class ReceivedConverter
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
