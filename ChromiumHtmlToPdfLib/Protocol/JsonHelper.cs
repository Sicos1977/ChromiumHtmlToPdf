//
// JsonHelper.cs
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

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
///     Contains a shared, cached <see cref="JsonSerializerOptions" /> that is used to (de)serialize
///     the Chrome DevTools Protocol messages with <see cref="System.Text.Json" />
/// </summary>
internal static class JsonHelper
{
    #region Fields
    /// <summary>
    ///     The shared <see cref="JsonSerializerOptions" /> used for all protocol (de)serialization
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        // Chromium sometimes returns properties with a different casing than our models,
        // so match property names case-insensitively to be safe
        PropertyNameCaseInsensitive = true,
        // Do not emit null properties, this keeps the messages we send to Chromium compact
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    #endregion
}
