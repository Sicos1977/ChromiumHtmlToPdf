//
// Fetch.cs
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

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
///     The JSON object that is returned when the <b>Fetch.enable</b> is activated in Chromium
/// </summary>
internal class Fetch
{
    #region Properties
    /// <summary>
    ///     The method (event) that Chromium sent, e.g. <b>Fetch.requestPaused</b>
    /// </summary>
    [JsonPropertyName("method")] 
    public string? Method { get; set; }

    /// <summary>
    ///     The parameters that belong to the <see cref="Method" />
    /// </summary>
    [JsonPropertyName("params")] 
    public FetchParams Params { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static Fetch FromJson(string json)
    {
        return JsonSerializer.Deserialize<Fetch>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="Fetch" /> class
/// </summary>
internal class FetchParams
{
    #region Properties
    /// <summary>
    ///     Each request the page makes will have a unique id
    /// </summary>
    [JsonPropertyName("requestId")] 
    public string RequestId { get; set; } = null!;

    /// <summary>
    ///     The details of the request
    /// </summary>
    [JsonPropertyName("request")] 
    public FetchRequest Request { get; set; } = null!;

    /// <summary>
    ///     The id of the frame that initiated the request
    /// </summary>
    [JsonPropertyName("frameId")] 
    public string? FrameId { get; set; }

    /// <summary>
    ///     How the requested resource will be used
    /// </summary>
    [JsonPropertyName("resourceType")] 
    public string? ResourceType { get; set; }

    /// <summary>
    ///     The id of the network request that produced this fetch event
    /// </summary>
    [JsonPropertyName("networkId")] 
    public string? NetworkId { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="Fetch" /> class
/// </summary>
internal class FetchRequest
{
    #region Properties
    /// <summary>
    ///     Request URL (without fragment)
    /// </summary>
    [JsonPropertyName("url")] 
    public string Url { get; set; } = null!;

    /// <summary>
    ///     HTTP request method
    /// </summary>
    [JsonPropertyName("method")] 
    public string? Method { get; set; }

    /// <summary>
    ///     HTTP request headers
    /// </summary>
    [JsonPropertyName("headers")] 
    public FetchHeaders? Headers { get; set; }

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
///     Part of the <see cref="FetchRequest" /> class
/// </summary>
internal class FetchHeaders
{
    #region Properties
    /// <summary>
    ///     The HTTP <b>Accept</b> request header
    /// </summary>
    [JsonPropertyName("Accept")] 
    public string? Accept { get; set; }
    #endregion
}
