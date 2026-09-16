//
// DataReceived.cs
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
///     The JSON object that is returned from Chromium for the <b>Network.dataReceived</b> event
/// </summary>
internal class DataReceived
{
    #region Properties
    /// <summary>
    ///     The method (event) that Chromium sent
    /// </summary>
    [JsonPropertyName("method")] 
    public string? Method { get; set; }

    /// <summary>
    ///     The parameters that belong to the <see cref="Method" />
    /// </summary>
    [JsonPropertyName("params")]
    public DataReceivedParams Params { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static DataReceived FromJson(string json)
    {
        return JsonSerializer.Deserialize<DataReceived>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="DataReceived" /> class
/// </summary>
internal class DataReceivedParams
{
    #region Properties
    /// <summary>
    ///     Request identifier
    /// </summary>
    [JsonPropertyName("requestId")] 
    public string? RequestId { get; set; }

    /// <summary>
    ///     Timestamp
    /// </summary>
    [JsonPropertyName("timestamp")] 
    public double Timestamp { get; set; }

    /// <summary>
    ///     Data chunk length
    /// </summary>
    [JsonPropertyName("dataLength")] 
    public long DataLength { get; set; }

    /// <summary>
    ///     Actual bytes received (might be less than <see cref="DataLength" /> for compressed encodings)
    /// </summary>
    [JsonPropertyName("encodedDataLength")] 
    public long EncodedDataLength { get; set; }
    #endregion
}
