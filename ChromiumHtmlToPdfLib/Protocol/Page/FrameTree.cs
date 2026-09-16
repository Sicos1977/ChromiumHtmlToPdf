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

/// <summary>
///     The JSON object that is returned from Chromium for the <b>Page.getFrameTree</b> command
/// </summary>
internal class FrameTree
{
    #region Properties
    /// <summary>
    ///     The message id
    /// </summary>
    [JsonPropertyName("id")] public int Id { get; set; }

    /// <summary>
    ///     The result containing the frame tree
    /// </summary>
    [JsonPropertyName("result")] public FrameTreeResponse Result { get; set; } = null!;
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static FrameTree FromJson(string json)
    {
        return JsonSerializer.Deserialize<FrameTree>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="FrameTree" /> class
/// </summary>
internal class FrameTreeResponse
{
    #region Properties
    /// <summary>
    ///     Present frame tree structure
    /// </summary>
    [JsonPropertyName("frameTree")] public FrameResponse FrameTree { get; set; } = null!;
    #endregion
}

/// <summary>
///     Part of the <see cref="FrameTreeResponse" /> class
/// </summary>
internal class FrameResponse
{
    #region Properties
    /// <summary>
    ///     Frame information for this tree item
    /// </summary>
    [JsonPropertyName("frame")] public FrameBody Frame { get; set; } = null!;
    #endregion
}

/// <summary>
///     Part of the <see cref="FrameResponse" /> class
/// </summary>
internal class FrameBody
{
    #region Properties
    /// <summary>
    ///     Frame unique identifier
    /// </summary>
    [JsonPropertyName("id")] public string Id { get; set; } = null!;

    /// <summary>
    ///     Identifier of the loader associated with this frame
    /// </summary>
    [JsonPropertyName("loaderId")] public string? LoaderId { get; set; }

    /// <summary>
    ///     Frame document's URL without fragment
    /// </summary>
    [JsonPropertyName("url")] public string? Url { get; set; }
    #endregion
}
