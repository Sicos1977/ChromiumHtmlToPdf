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

/// <summary>
///     The JSON object that is returned from Chromium for the <b>Network.requestWillBeSentExtraInfo</b> event
/// </summary>
internal class RequestWillBeSentExtraInfo : Base
{
    #region Properties
    /// <summary>
    ///     The parameters that belong to the <see cref="Base.Method" />
    /// </summary>
    [JsonPropertyName("params")] 
    public RequestWillBeSentExtraInfoParams? Params { get; set; }
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

/// <summary>
///     Part of the <see cref="RequestWillBeSentExtraInfo" /> class
/// </summary>
internal class RequestWillBeSentExtraInfoParams
{
    #region Properties
    /// <summary>
    ///     Request identifier. Used to match this information to an existing <b>requestWillBeSent</b> event
    /// </summary>
    [JsonPropertyName("requestId")] 
    public string? RequestId { get; set; }

    /// <summary>
    ///     A list of cookies potentially associated to the requested URL. This includes both cookies sent with
    ///     the request and the ones not sent; the latter are distinguished by having <c>blockedReason</c> field set
    /// </summary>
    [JsonPropertyName("associatedCookies")] 
    public List<object>? AssociatedCookies { get; set; }

    /// <summary>
    ///     Raw request headers as they will be sent over the wire
    /// </summary>
    [JsonPropertyName("headers")] 
    public RequestWillBeSentExtraInfoHeaders? Headers { get; set; }

    /// <summary>
    ///     The client security state set for the request
    /// </summary>
    [JsonPropertyName("clientSecurityState")]
    public RequestWillBeSentExtraInfoClientSecurityState? 
        ClientSecurityState { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="RequestWillBeSentExtraInfoParams" /> class
/// </summary>
internal class RequestWillBeSentExtraInfoClientSecurityState
{
    #region Properties
    /// <summary>
    ///     <c>true</c> when the initiator is a secure context
    /// </summary>
    [JsonPropertyName("initiatorIsSecureContext")]
    public bool InitiatorIsSecureContext { get; set; }

    /// <summary>
    ///     The IP address space of the initiator
    /// </summary>
    [JsonPropertyName("initiatorIPAddressSpace")]
    public string? InitiatorIpAddressSpace { get; set; }

    /// <summary>
    ///     The private network request policy
    /// </summary>
    [JsonPropertyName("privateNetworkRequestPolicy")]
    public string? PrivateNetworkRequestPolicy { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="RequestWillBeSentExtraInfoParams" /> class
/// </summary>
internal class RequestWillBeSentExtraInfoHeaders
{
    #region Properties
    /// <summary>
    ///     The HTTP/2 <b>:method</b> pseudo-header
    /// </summary>
    [JsonPropertyName(":method")] 
    public string? Method { get; set; }

    /// <summary>
    ///     The HTTP/2 <b>:authority</b> pseudo-header
    /// </summary>
    [JsonPropertyName(":authority")] 
    public string? Authority { get; set; }

    /// <summary>
    ///     The HTTP/2 <b>:scheme</b> pseudo-header
    /// </summary>
    [JsonPropertyName(":scheme")] 
    public string? Scheme { get; set; }

    /// <summary>
    ///     The HTTP/2 <b>:path</b> pseudo-header
    /// </summary>
    [JsonPropertyName(":path")] 
    public string? Path { get; set; }

    /// <summary>
    ///     The HTTP <b>user-agent</b> request header
    /// </summary>
    [JsonPropertyName("user-agent")] 
    public string? UserAgent { get; set; }

    /// <summary>
    ///     The HTTP <b>accept</b> request header
    /// </summary>
    [JsonPropertyName("accept")] 
    public string? Accept { get; set; }

    /// <summary>
    ///     The HTTP <b>sec-fetch-site</b> request header
    /// </summary>
    [JsonPropertyName("sec-fetch-site")] 
    public string? SecFetchSite { get; set; }

    /// <summary>
    ///     The HTTP <b>sec-fetch-mode</b> request header
    /// </summary>
    [JsonPropertyName("sec-fetch-mode")] 
    public string? SecFetchMode { get; set; }

    /// <summary>
    ///     The HTTP <b>sec-fetch-dest</b> request header
    /// </summary>
    [JsonPropertyName("sec-fetch-dest")] 
    public string? SecFetchDest { get; set; }

    /// <summary>
    ///     The HTTP <b>accept-encoding</b> request header
    /// </summary>
    [JsonPropertyName("accept-encoding")] 
    public string? AcceptEncoding { get; set; }

    /// <summary>
    ///     The HTTP <b>accept-language</b> request header
    /// </summary>
    [JsonPropertyName("accept-language")] 
    public string? AcceptLanguage { get; set; }
    #endregion
}
