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

/// <summary>
///     The JSON object that is returned from Chromium for the <b>Network.requestWillBeSent</b> event
/// </summary>
internal class RequestWillBeSent : Base
{
    #region Properties
    /// <summary>
    ///     The parameters that belong to the <see cref="Base.Method" />
    /// </summary>
    [JsonPropertyName("params")] 
    public RequestWillBeSentParams Params { get; set; } = null!;
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

/// <summary>
///     Part of the <see cref="RequestWillBeSent" /> class
/// </summary>
internal class RequestWillBeSentParams
{
    #region Properties
    /// <summary>
    ///     Request identifier
    /// </summary>
    [JsonPropertyName("requestId")] 
    public string? RequestId { get; set; }

    /// <summary>
    ///     Loader identifier. Empty string if the request is fetched from worker
    /// </summary>
    [JsonPropertyName("loaderId")] 
    public string? LoaderId { get; set; }

    /// <summary>
    ///     URL of the document this request is loaded for
    /// </summary>
    [JsonPropertyName("documentURL")] 
    public string? DocumentUrl { get; set; }

    /// <summary>
    ///     Request data
    /// </summary>
    [JsonPropertyName("request")] 
    public RequestWillBeSentRequest Request { get; set; } = null!;

    /// <summary>
    ///     Timestamp
    /// </summary>
    [JsonPropertyName("timestamp")] 
    public double Timestamp { get; set; }

    /// <summary>
    ///     Timestamp (UTC epoch time in seconds)
    /// </summary>
    [JsonPropertyName("wallTime")] 
    public double WallTime { get; set; }

    /// <summary>
    ///     Request initiator
    /// </summary>
    [JsonPropertyName("initiator")] 
    public WillBeSentInitiator? Initiator { get; set; }

    /// <summary>
    ///     Type of this resource
    /// </summary>
    [JsonPropertyName("type")] 
    public string? Type { get; set; }

    /// <summary>
    ///     Frame identifier
    /// </summary>
    [JsonPropertyName("frameId")] 
    public string? FrameId { get; set; }

    /// <summary>
    ///     Whether the request is initiated by a user gesture. Defaults to <c>false</c>
    /// </summary>
    [JsonPropertyName("hasUserGesture")] 
    public bool HasUserGesture { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="RequestWillBeSentParams" /> class
/// </summary>
internal class WillBeSentInitiator
{
    #region Properties
    /// <summary>
    ///     Type of this initiator
    /// </summary>
    [JsonPropertyName("type")] 
    public string? Type { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="RequestWillBeSentParams" /> class
/// </summary>
internal class RequestWillBeSentRequest
{
    #region Properties
    /// <summary>
    ///     Request URL (without fragment)
    /// </summary>
    [JsonPropertyName("url")] 
    public string? Url { get; set; }

    /// <summary>
    ///     HTTP request method
    /// </summary>
    [JsonPropertyName("method")] 
    public string? Method { get; set; }

    /// <summary>
    ///     HTTP request headers
    /// </summary>
    [JsonPropertyName("headers")] 
    public RequestWillBeSentHeaders? Headers { get; set; }

    /// <summary>
    ///     The mixed content type of the request
    /// </summary>
    [JsonPropertyName("mixedContentType")] 
    public string? MixedContentType { get; set; }

    /// <summary>
    ///     Priority of the resource request at the time request is sent
    /// </summary>
    [JsonPropertyName("initialPriority")] 
    public string? InitialPriority { get; set; }

    /// <summary>
    ///     The referrer policy of the request, as defined in https://www.w3.org/TR/referrer-policy/
    /// </summary>
    [JsonPropertyName("referrerPolicy")] 
    public string? ReferrerPolicy { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="RequestWillBeSentRequest" /> class
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
internal class RequestWillBeSentHeaders
{
}
