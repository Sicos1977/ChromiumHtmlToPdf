//
// Evaluate.cs
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

using System.Text;
using Newtonsoft.Json;

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
/// </summary>
internal class Evaluate : MessageBase
{
    #region Properties
    /// <summary>
    ///     The returned result
    /// </summary>
    [JsonProperty("result")]
    public EvaluateResult Result { get; set; }

    /// <summary>
    ///     The method that we want to execute in Chromium
    /// </summary>
    [JsonProperty("method")]
    public string Method { get; set; }
    #endregion

    #region FromJson
    /// <summary>
    ///     Returns this object deserialized from the given <paramref name="json" /> string
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public new static Evaluate FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Evaluate>(json);
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="Evaluate" /> class
/// </summary>
internal class EvaluateResult
{
    #region Propreties
    [JsonProperty("result")] 
    public EvaluateInnerResult Result { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="EvaluateResult" /> class
/// </summary>
internal class EvaluateInnerResult
{
    #region Properties
    [JsonProperty("type")] 
    public string Type { get; set; }

    [JsonProperty("subtype")] 
    public string SubType { get; set; }

    [JsonProperty("className")] 
    public string ClassName { get; set; }

    [JsonProperty("value")] 
    public string Value { get; set; }
    
    [JsonProperty("unserializableValue")] 
    public string UnserializableValue { get; set; }
    
    [JsonProperty("deepSerializedValue")] 
    public string DeepSerializedValue { get; set; }
    
    [JsonProperty("preview")] 
    public string Preview { get; set; }
    
    [JsonProperty("customPreview")] 
    public string CustomPreview { get; set; }
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