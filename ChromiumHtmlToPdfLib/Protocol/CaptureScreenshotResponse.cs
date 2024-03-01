//
// CaptureScreenshotResponse.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2024 Magic-Sessions. (www.magic-sessions.com)
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
///     Placeholder for the result of a page snapshot
/// </summary>
internal class CaptureScreenshotResponse
{
    #region Properties
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("result")] public CaptureScreenshotResult Result { get; set; }

    /// <summary>
    ///     Returns <see cref="PrintToPdfResult.Data" /> as array of bytes
    /// </summary>
    public byte[] Bytes => Convert.FromBase64String(Result.Data);
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static CaptureScreenshotResponse FromJson(string json)
    {
        return JsonConvert.DeserializeObject<CaptureScreenshotResponse>(json);
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="CaptureScreenshotResponse" /> class
/// </summary>
internal class CaptureScreenshotResult
{
    #region Properties
    [JsonProperty("data")] public string Data { get; set; }
    #endregion
}