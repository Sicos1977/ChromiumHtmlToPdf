//
// Evaluate.cs
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

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
///     The JSON structure that is returned from Chromium for a <b>Runtime.evaluate</b> command
/// </summary>
internal class Evaluate : MessageBase
{
    #region Properties
    /// <summary>
    ///     The returned result
    /// </summary>
    [JsonPropertyName("result")]
    public EvaluateResult? Result { get; set; }

    /// <summary>
    ///     The method that we want to execute in Chromium
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static Evaluate FromJson(string json)
    {
        return JsonSerializer.Deserialize<Evaluate>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="Evaluate" /> class
/// </summary>
internal class EvaluateResult
{
    #region Propreties
    /// <summary>
    ///     The result of the evaluation as a remote object
    /// </summary>
    [JsonPropertyName("result")]
    public EvaluateInnerResult? Result { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="EvaluateResult" /> class
/// </summary>
internal class EvaluateInnerResult
{
    #region Properties
    /// <summary>
    ///     Object type
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    ///     Object subtype hint. Specified for <c>object</c> type values only
    /// </summary>
    [JsonPropertyName("subtype")]
    public string? SubType { get; set; }

    /// <summary>
    ///     Object class (constructor) name. Specified for <c>object</c> type values only
    /// </summary>
    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    /// <summary>
    ///     Remote object value in case of primitive values or JSON values (if it was requested)
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    ///     Primitive value which can not be JSON-stringified does not have <see cref="Value" />, but gets this property
    /// </summary>
    [JsonPropertyName("unserializableValue")]
    public string? UnserializableValue { get; set; }

    /// <summary>
    ///     Deep serialized value
    /// </summary>
    [JsonPropertyName("deepSerializedValue")]
    public string? DeepSerializedValue { get; set; }

    /// <summary>
    ///     Preview containing abbreviated property values. Specified for <c>object</c> type values only
    /// </summary>
    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    /// <summary>
    ///     The custom preview
    /// </summary>
    [JsonPropertyName("customPreview")]
    public string? CustomPreview { get; set; }
    #endregion

    #region ToString
    /// <summary>
    ///     Returns a string representation of this object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(Type))
            stringBuilder.AppendLine($"Type: {Type}");

        if (!string.IsNullOrEmpty(SubType))
            stringBuilder.AppendLine($"SubType: {SubType}");

        if (!string.IsNullOrEmpty(ClassName))
            stringBuilder.AppendLine($"ClassName: {ClassName}");

        if (!string.IsNullOrEmpty(Value))
            stringBuilder.AppendLine($"Value: {Value}");

        if (!string.IsNullOrEmpty(UnserializableValue))
            stringBuilder.AppendLine($"UnserializableValue: {UnserializableValue}");

        if (!string.IsNullOrEmpty(DeepSerializedValue))
            stringBuilder.AppendLine($"DeepSerializedValue: {DeepSerializedValue}");

        if (!string.IsNullOrEmpty(Preview))
            stringBuilder.AppendLine($"Preview: {Preview}");

        if (!string.IsNullOrEmpty(CustomPreview))
            stringBuilder.AppendLine($"CustomPreview: {CustomPreview}");

        return stringBuilder.ToString();
    }
    #endregion
}
