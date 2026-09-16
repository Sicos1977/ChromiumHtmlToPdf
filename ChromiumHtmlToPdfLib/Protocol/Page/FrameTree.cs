//
// FrameTree.cs
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

namespace ChromiumHtmlToPdfLib.Protocol.Page;

internal class FrameTree
{
    #region Properties
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("result")] public FrameTreeResponse Result { get; set; } = null!;
    #endregion

    #region FromJson
    public static FrameTree FromJson(string json)
    {
        return JsonSerializer.Deserialize<FrameTree>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

internal class FrameTreeResponse
{
    #region Properties
    [JsonPropertyName("frameTree")] public FrameResponse FrameTree { get; set; } = null!;
    #endregion
}

internal class FrameResponse
{
    #region Properties
    [JsonPropertyName("frame")] public FrameBody Frame { get; set; } = null!;
    #endregion
}

internal class FrameBody
{
    #region Properties
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    [JsonPropertyName("loaderId")] public string? LoaderId { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }
    #endregion
}