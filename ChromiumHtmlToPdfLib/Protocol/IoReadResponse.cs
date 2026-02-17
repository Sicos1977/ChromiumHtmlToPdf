//
// IoReadResponse.cs
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
using Newtonsoft.Json;

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
///     The JSON structure that is returned from Chromium when reading from an IO stream
/// </summary>
internal class IoReadResponse : MessageBase
{
    #region Properties
    /// <summary>
    ///     <see cref="IoReadResponseResult" />
    /// </summary>
    [JsonProperty("result")]
    public IoReadResponseResult Result { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static IoReadResponse FromJson(string json)
    {
        return JsonConvert.DeserializeObject<IoReadResponse>(json)!;
    }
    #endregion
}

/// <summary>
///     The chunk read
/// </summary>
internal class IoReadResponseResult
{
    #region Properties
    /// <summary>
    ///     Returns <c>true</c> when <see cref="Data" /> is base64 encoded
    /// </summary>
    [JsonProperty("base64Encoded")]
    public bool Base64Encoded { get; set; }

    /// <summary>
    ///     Returns the data as a base64 encoded string
    /// </summary>
    [JsonProperty("data")]
    public string Data { get; set; } = null!;

    /// <summary>
    ///     Returns <see cref="PrintToPdfResult.Data" /> as array of bytes
    /// </summary>
    public byte[] Bytes => Convert.FromBase64String(Data);

    /// <summary>
    ///     Returns <c>true</c> when at the end of the file
    /// </summary>
    [JsonProperty("eof")]
    public bool Eof { get; set; }
    #endregion
}