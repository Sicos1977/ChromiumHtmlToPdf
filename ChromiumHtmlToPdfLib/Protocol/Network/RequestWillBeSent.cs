//
// RequestWillBeSent.cs
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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromiumHtmlToPdfLib.Protocol.Network;

internal class RequestWillBeSent : Base
{
    #region Properties
    [JsonPropertyName("params")] public RequestWillBeSentParams Params { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static RequestWillBeSent FromJson(string json)
    {
        return JsonSerializer.Deserialize<RequestWillBeSent>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

internal class RequestWillBeSentParams
{
    #region Properties
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }

    [JsonPropertyName("loaderId")] public string? LoaderId { get; set; }

    [JsonPropertyName("documentURL")] public string? DocumentUrl { get; set; }

    [JsonPropertyName("request")] public RequestWillBeSentRequest Request { get; set; } = null!;

    [JsonPropertyName("timestamp")] public double Timestamp { get; set; }

    [JsonPropertyName("wallTime")] public double WallTime { get; set; }

    [JsonPropertyName("initiator")] public WillBeSentInitiator? Initiator { get; set; }

    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("frameId")] public string? FrameId { get; set; }

    [JsonPropertyName("hasUserGesture")] public bool HasUserGesture { get; set; }
    #endregion
}

internal class WillBeSentInitiator
{
    #region Properties
    [JsonPropertyName("type")] public string? Type { get; set; }
    #endregion
}

internal class RequestWillBeSentRequest
{
    #region Properties
    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("method")] public string? Method { get; set; }

    [JsonPropertyName("headers")] public RequestWillBeSentHeaders? Headers { get; set; }

    [JsonPropertyName("mixedContentType")] public string? MixedContentType { get; set; }

    [JsonPropertyName("initialPriority")] public string? InitialPriority { get; set; }

    [JsonPropertyName("referrerPolicy")] public string? ReferrerPolicy { get; set; }
    #endregion
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class RequestWillBeSentHeaders
{
}
