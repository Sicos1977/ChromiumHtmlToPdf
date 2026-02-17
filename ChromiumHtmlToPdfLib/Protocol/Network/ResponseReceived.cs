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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChromiumHtmlToPdfLib.Protocol.Network;

internal class ResponseReceived : Base
{
    #region Properties
    [JsonProperty("params")] public ResponseReceivedParams Params { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static ResponseReceived FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ResponseReceived>(json, ResponseReceivedConverter.Settings)!;
    }
    #endregion
}

internal class ResponseReceivedParams
{
    #region Properties
    [JsonProperty("requestId")] public string? RequestId { get; set; }

    [JsonProperty("loaderId")] public string? LoaderId { get; set; }

    [JsonProperty("timestamp")] public double Timestamp { get; set; }

    [JsonProperty("type")] public string? Type { get; set; }

    [JsonProperty("response")] public ResponseReceivedResponse Response { get; set; } = null!;

    [JsonProperty("frameId")] public string? FrameId { get; set; }
    #endregion
}

internal class ResponseReceivedResponse
{
    #region Properties
    /// <summary>
    ///     Response URL. This URL can be different from CachedResource.url in case of redirect
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }

    /// <summary>
    ///     HTTP response status code.
    /// </summary>
    [JsonProperty("status")]
    public long Status { get; set; }

    /// <summary>
    ///     HTTP response status text.
    /// </summary>
    [JsonProperty("statusText")]
    public string? StatusText { get; set; }

    /// <summary>
    ///     HTTP response headers.
    /// </summary>
    [JsonProperty("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Resource mimeType as determined by the browser.
    /// </summary>
    [JsonProperty("mimeType")]
    public string? MimeType { get; set; }

    /// <summary>
    ///     Specifies whether physical connection was actually reused for this request.
    /// </summary>
    [JsonProperty("connectionReused")]
    public bool ConnectionReused { get; set; }

    /// <summary>
    ///     Physical connection id that was actually used for this request.
    /// </summary>
    [JsonProperty("connectionId")]
    public long ConnectionId { get; set; }

    /// <summary>
    ///     Remote IP address.
    /// </summary>
    [JsonProperty("remoteIPAddress")]
    public string? RemoteIpAddress { get; set; }

    /// <summary>
    ///     Remote port.
    /// </summary>
    [JsonProperty("remotePort")]
    public long RemotePort { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the disk cache.
    /// </summary>
    [JsonProperty("fromDiskCache")]
    public bool FromDiskCache { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the ServiceWorker.
    /// </summary>
    [JsonProperty("fromServiceWorker")]
    public bool FromServiceWorker { get; set; }

    /// <summary>
    ///     Specifies that the request was served from the prefetch cache.
    /// </summary>
    [JsonProperty("fromPrefetchCache")]
    public bool FromPrefetchCache { get; set; }

    /// <summary>
    ///     Total number of bytes received for this request so far.
    /// </summary>
    [JsonProperty("encodedDataLength")]
    public long EncodedDataLength { get; set; }

    /// <summary>
    ///     Cache Storage Cache Name.
    /// </summary>
    [JsonProperty("cacheStorageCacheName")]
    public string? CacheStorageCacheName { get; set; }

    /// <summary>
    ///     Protocol used to fetch this request.
    /// </summary>
    [JsonProperty("protocol")]
    public string? Protocol { get; set; }

    /// <summary>
    ///     Security state of the request resource.
    /// </summary>
    /// <remarks>
    ///     unknown, neutral, insecure, secure, info, insecure-broken
    /// </remarks>
    [JsonProperty("securityState")]
    public string? SecurityState { get; set; }

    [JsonProperty("securityDetails")] public ResponseReceiveSecurityDetails? SecurityDetails { get; set; }
    #endregion
}

internal class ResponseReceiveSecurityDetails
{
    #region Properties
    [JsonProperty("protocol")] public string? Protocol { get; set; }

    [JsonProperty("keyExchange")] public string? KeyExchange { get; set; }

    [JsonProperty("keyExchangeGroup")] public string? KeyExchangeGroup { get; set; }

    [JsonProperty("cipher")] public string? Cipher { get; set; }

    [JsonProperty("certificateId")] public long CertificateId { get; set; }

    [JsonProperty("subjectName")] public string? SubjectName { get; set; }

    [JsonProperty("sanList")] public string[]? SanList { get; set; }

    [JsonProperty("issuer")] public string? Issuer { get; set; }

    [JsonProperty("validFrom")] public long ValidFrom { get; set; }

    [JsonProperty("validTo")] public long ValidTo { get; set; }

    [JsonProperty("signedCertificateTimestampList")]
    public object[]? SignedCertificateTimestampList { get; set; }

    [JsonProperty("certificateTransparencyCompliance")]
    public string? CertificateTransparencyCompliance { get; set; }
    #endregion
}

#region Static class ResponseReceivedConverter
internal static class ResponseReceivedConverter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal } }
    };
}
#endregion