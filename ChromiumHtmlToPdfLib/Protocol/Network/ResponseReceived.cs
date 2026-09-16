//
// ResponseReceived.cs
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

internal class ResponseReceived : Base
{
    #region Properties
    [JsonPropertyName("params")] public ResponseReceivedParams Params { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static ResponseReceived FromJson(string json)
    {
        return JsonSerializer.Deserialize<ResponseReceived>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

internal class ResponseReceivedParams
{
    #region Properties
    [JsonPropertyName("requestId")] public string? RequestId { get; set; }

    [JsonPropertyName("loaderId")] public string? LoaderId { get; set; }

    [JsonPropertyName("timestamp")] public double Timestamp { get; set; }

    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("response")] public ResponseReceivedResponse Response { get; set; } = null!;

    [JsonPropertyName("frameId")] public string? FrameId { get; set; }
    #endregion
}

internal class ResponseReceivedResponse
{
    #region Properties
    /// <summary>
    ///     Response URL. This URL can be different from CachedResource.url in case of redirect
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    ///     HTTP response status code.
    /// </summary>
    [JsonPropertyName("status")]
    public long Status { get; set; }

    /// <summary>
    ///     HTTP response status text.
    /// </summary>
    [JsonPropertyName("statusText")]
    public string? StatusText { get; set; }

    /// <summary>
    ///     HTTP response headers.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Resource mimeType as determined by the browser.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    ///     Specifies whether physical connection was actually reused for this request.
    /// </summary>
    [JsonPropertyName("connectionReused")]
    public bool ConnectionReused { get; set; }

    /// <summary>
    ///     Physical connection id that was actually used for this request.
    /// </summary>
    [JsonPropertyName("connectionId")]
    public long ConnectionId { get; set; }

    /// <summary>
    ///     Remote IP address.
    /// </summary>
    [JsonPropertyName("remoteIPAddress")]
    public string? RemoteIpAddress { get; set; }

    /// <summary>
    ///     Remote port.
    /// </summary>
    [JsonPropertyName("remotePort")]
    public long RemotePort { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the disk cache.
    /// </summary>
    [JsonPropertyName("fromDiskCache")]
    public bool FromDiskCache { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the ServiceWorker.
    /// </summary>
    [JsonPropertyName("fromServiceWorker")]
    public bool FromServiceWorker { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the prefetch cache.
    /// </summary>
    [JsonPropertyName("fromPrefetchCache")]
    public bool FromPrefetchCache { get; set; }

    /// <summary>
    ///     Total number of bytes received for this request so far.
    /// </summary>
    [JsonPropertyName("encodedDataLength")]
    public long EncodedDataLength { get; set; }

    /// <summary>
    ///     Cache Storage Cache Name.
    /// </summary>
    [JsonPropertyName("cacheStorageCacheName")]
    public string? CacheStorageCacheName { get; set; }

    /// <summary>
    ///     Protocol used to fetch this request.
    /// </summary>
    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    ///     Security state of the request resource.
    /// </summary>
    /// <remarks>
    ///     unknown, neutral, insecure, secure, info, insecure-broken
    /// </remarks>
    [JsonPropertyName("securityState")]
    public string? SecurityState { get; set; }

    [JsonPropertyName("securityDetails")] public ResponseReceiveSecurityDetails? SecurityDetails { get; set; }
    #endregion
}

internal class ResponseReceiveSecurityDetails
{
    #region Properties
    [JsonPropertyName("protocol")] public string? Protocol { get; set; }

    [JsonPropertyName("keyExchange")] public string? KeyExchange { get; set; }

    [JsonPropertyName("keyExchangeGroup")] public string? KeyExchangeGroup { get; set; }

    [JsonPropertyName("cipher")] public string? Cipher { get; set; }

    [JsonPropertyName("certificateId")] public long CertificateId { get; set; }

    [JsonPropertyName("subjectName")] public string? SubjectName { get; set; }

    [JsonPropertyName("sanList")] public string[]? SanList { get; set; }

    [JsonPropertyName("issuer")] public string? Issuer { get; set; }

    [JsonPropertyName("validFrom")] public long ValidFrom { get; set; }

    [JsonPropertyName("validTo")] public long ValidTo { get; set; }

    [JsonPropertyName("signedCertificateTimestampList")]
    public object[]? SignedCertificateTimestampList { get; set; }

    [JsonPropertyName("certificateTransparencyCompliance")]
    public string? CertificateTransparencyCompliance { get; set; }
    #endregion
}
