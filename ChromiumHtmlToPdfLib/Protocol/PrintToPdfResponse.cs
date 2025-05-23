﻿//
// PrintToPdfResponse.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2025 Magic-Sessions. (www.magic-sessions.com)
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
///     The JSON object that is returned from Chrome when calling the <see cref="Converter" /> ConvertToPdf method
/// </summary>
internal class PrintToPdfResponse
{
    #region Properties
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    ///     <see cref="PrintToPdfResult" />
    /// </summary>
    [JsonProperty("result")]
    public PrintToPdfResult? Result { get; set; }

    /// <summary>
    ///     <see cref="PrintToPdfErrorResult" />
    /// </summary>
    [JsonProperty("error")]
    public PrintToPdfErrorResult? Error { get; set; }

    /// <summary>
    ///     Returns <see cref="PrintToPdfResult.Data" /> as array of bytes
    /// </summary>
    public byte[]? Bytes => Result != null ? Convert.FromBase64String(Result.Data) : null;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static PrintToPdfResponse FromJson(string json)
    {
        return JsonConvert.DeserializeObject<PrintToPdfResponse>(json)!;
    }
    #endregion
}

/// <summary>
///     The result returned from the <see cref="Converter" /> ConvertToPdf  method
/// </summary>
internal class PrintToPdfResult
{
    #region Properties
    /// <summary>
    ///     The PDF as base64 string
    /// </summary>
    [JsonProperty("data")]
    public string Data { get; set; } = null!;

    /// <summary>
    ///     Returns a stream handle number
    /// </summary>
    [JsonProperty("stream")]
    public string? Stream { get; set; }
    #endregion
}

/// <summary>
///     Error result returned from the <see cref="Converter" /> ConvertToPdf  method
/// </summary>
internal class PrintToPdfErrorResult
{
    #region Properties
    /// <summary>
    ///     Error code
    /// </summary>
    [JsonProperty("code")]
    public int Code { get; set; }

    /// <summary>
    ///     Error message
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }
    #endregion
}
