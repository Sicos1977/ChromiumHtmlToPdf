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
    [JsonPropertyName("method")] public string? Method { get; set; }

    [JsonPropertyName("params")] public FetchParams Params { get; set; } = null!;
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
    [JsonPropertyName("requestId")] public string RequestId { get; set; } = null!;

    [JsonPropertyName("request")] public FetchRequest Request { get; set; } = null!;

    [JsonPropertyName("frameId")] public string? FrameId { get; set; }

    [JsonPropertyName("resourceType")] public string? ResourceType { get; set; }

    [JsonPropertyName("networkId")] public string? NetworkId { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="Fetch" /> class
/// </summary>
internal class FetchRequest
{
    #region Properties
    [JsonPropertyName("url")] public string Url { get; set; } = null!;

    [JsonPropertyName("method")] public string? Method { get; set; }

    [JsonPropertyName("headers")] public FetchHeaders? Headers { get; set; }

    [JsonPropertyName("initialPriority")] public string? InitialPriority { get; set; }

    [JsonPropertyName("referrerPolicy")] public string? ReferrerPolicy { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="FetchRequest" /> class
/// </summary>
internal class FetchHeaders
{
    #region Properties
    [JsonPropertyName("Accept")] public string? Accept { get; set; }
    #endregion
}