//
// ResponseReceivedExtraInfo.cs
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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChromiumHtmlToPdfLib.Protocol.Network;

internal class ResponseReceivedExtraInfo : Base
{
    #region Properties
    [JsonProperty("params")] public ResponseReceivedExtraInfoParams Params { get; set; }
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static ResponseReceivedExtraInfo FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ResponseReceivedExtraInfo>(json,
            ResponseReceivedExtraInfoConverter.Settings);
    }
    #endregion
}

internal class ResponseReceivedExtraInfoParams
{
    #region Properties
    [JsonProperty("requestId")] public string RequestId { get; set; }

    [JsonProperty("blockedCookies")] public List<object> BlockedCookies { get; set; }

    [JsonProperty("headers")] public ResponseReceivedExtraInfoHeaders Headers { get; set; }
    #endregion
}

internal class ResponseReceivedExtraInfoHeaders
{
    #region Properties
    [JsonProperty("content-type")] public string ContentType { get; set; }

    [JsonProperty("content-length")]
    [JsonConverter(typeof(ResponseReceivedExtraInfoParseStringConverter))]
    public long ContentLength { get; set; }

    [JsonProperty("server")] public string Server { get; set; }

    [JsonProperty("etag")] public string Etag { get; set; }

    [JsonProperty("max-age")]
    [JsonConverter(typeof(ResponseReceivedExtraInfoParseStringConverter))]
    public long MaxAge { get; set; }

    [JsonProperty("x-debug")] public string XDebug { get; set; }

    [JsonProperty("cache-control")] public string CacheControl { get; set; }

    [JsonProperty("expires")] public string Expires { get; set; }

    [JsonProperty("date")] public string Date { get; set; }
    #endregion
}

#region Static class ResponseReceivedExtraInfoConverter
internal static class ResponseReceivedExtraInfoConverter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal } }
    };
}
#endregion

#region Class ResponseReceivedExtraInfoParseStringConverter
internal class ResponseReceivedExtraInfoParseStringConverter : JsonConverter
{
    public override bool CanConvert(Type t)
    {
        return t == typeof(long) || t == typeof(long?);
    }

    public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var value = serializer.Deserialize<string>(reader);

        if (long.TryParse(value, out var l))
            return l;

        throw new Exception("Cannot unmarshal type long");
    }

    public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
    {
        if (untypedValue == null)
        {
            serializer.Serialize(writer, null);
            return;
        }

        var value = (long)untypedValue;
        serializer.Serialize(writer, value.ToString());
    }
}
#endregion