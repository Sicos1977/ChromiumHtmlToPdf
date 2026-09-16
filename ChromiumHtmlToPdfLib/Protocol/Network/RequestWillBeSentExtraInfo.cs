//
// RequestWillBeSentExtraInfo.cs
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

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromiumHtmlToPdfLib.Protocol.Network;

internal class RequestWillBeSentExtraInfo : Base
{
    #region Properties
    [JsonPropertyName("params")] public RequestWillBeSentExtraInfoParams? Params { get; set; }
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static RequestWillBeSentExtraInfo FromJson(string json)
    {
        return JsonSerializer.Deserialize<RequestWillBeSentExtraInfo>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

internal class RequestWillBeSentExtraInfoParams
{
    #region Properties
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }

    [JsonPropertyName("associatedCookies")] public List<object>? AssociatedCookies { get; set; }

    [JsonPropertyName("headers")] public RequestWillBeSentExtraInfoHeaders? Headers { get; set; }

    [JsonPropertyName("clientSecurityState")]
    public RequestWillBeSentExtraInfoClientSecurityState? ClientSecurityState { get; set; }
    #endregion
}

internal class RequestWillBeSentExtraInfoClientSecurityState
{
    #region Properties
    [JsonPropertyName("initiatorIsSecureContext")]
    public bool InitiatorIsSecureContext { get; set; }

    [JsonPropertyName("initiatorIPAddressSpace")]
    public string? InitiatorIpAddressSpace { get; set; }

    [JsonPropertyName("privateNetworkRequestPolicy")]
    public string? PrivateNetworkRequestPolicy { get; set; }
    #endregion
}

internal class RequestWillBeSentExtraInfoHeaders
{
    #region Properties
    [JsonPropertyName(":method")] public string? Method { get; set; }

    [JsonPropertyName(":authority")] public string? Authority { get; set; }

    [JsonPropertyName(":scheme")] public string? Scheme { get; set; }

    [JsonPropertyName(":path")] public string? Path { get; set; }

    [JsonPropertyName("user-agent")] public string? UserAgent { get; set; }

    [JsonPropertyName("accept")] public string? Accept { get; set; }

    [JsonPropertyName("sec-fetch-site")] public string? SecFetchSite { get; set; }

    [JsonPropertyName("sec-fetch-mode")] public string? SecFetchMode { get; set; }

    [JsonPropertyName("sec-fetch-dest")] public string? SecFetchDest { get; set; }

    [JsonPropertyName("accept-encoding")] public string? AcceptEncoding { get; set; }

    [JsonPropertyName("accept-language")] public string? AcceptLanguage { get; set; }
    #endregion
}
