//
// ResponseReceivedExtraInfo.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2026 Magic-Sessions. (www.magic-sessions.com)
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromiumHtmlToPdfLib.Protocol.Network;

internal class ResponseReceivedExtraInfo : Base
{
    #region Properties
    [JsonPropertyName("params")] public ResponseReceivedExtraInfoParams? Params { get; set; }
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static ResponseReceivedExtraInfo FromJson(string json)
    {
        return JsonSerializer.Deserialize<ResponseReceivedExtraInfo>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

internal class ResponseReceivedExtraInfoParams
{
    #region Properties
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }

    [JsonPropertyName("blockedCookies")] public List<object>? BlockedCookies { get; set; }

    [JsonPropertyName("headers")] public ResponseReceivedExtraInfoHeaders? Headers { get; set; }
    #endregion
}

internal class ResponseReceivedExtraInfoHeaders
{
    #region Properties
    [JsonPropertyName("content-type")] public string? ContentType { get; set; }

    [JsonPropertyName("content-length")]
    [JsonConverter(typeof(ResponseReceivedExtraInfoParseStringConverter))]
    public long ContentLength { get; set; }

    [JsonPropertyName("server")] public string? Server { get; set; }

    [JsonPropertyName("etag")] public string? Etag { get; set; }

    [JsonPropertyName("max-age")]
    [JsonConverter(typeof(ResponseReceivedExtraInfoParseStringConverter))]
    public long MaxAge { get; set; }

    [JsonPropertyName("x-debug")] public string? XDebug { get; set; }

    [JsonPropertyName("cache-control")] public string? CacheControl { get; set; }

    [JsonPropertyName("expires")] public string? Expires { get; set; }

    [JsonPropertyName("date")] public string? Date { get; set; }
    #endregion
}


#region Class ResponseReceivedExtraInfoParseStringConverter
internal class ResponseReceivedExtraInfoParseStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();
            case JsonTokenType.String:
            {
                var value = reader.GetString();
                if (long.TryParse(value, out var l))
                    return l;
                break;
            }
        }

        throw new JsonException("Cannot unmarshal type long");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
#endregion
