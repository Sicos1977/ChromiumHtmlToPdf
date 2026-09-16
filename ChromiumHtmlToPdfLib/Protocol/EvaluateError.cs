//
// EvaluateError.cs
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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChromiumHtmlToPdfLib.Protocol;

/// <summary>
///     Returned by an <see cref="Evaluate" /> message when an error occurs
/// </summary>
internal class EvaluateError : MessageBase
{
    #region Properties
    [JsonPropertyName("result")] public EvaluateErrorResult? Result { get; set; }
    #endregion

    #region FromJson
    public new static EvaluateError FromJson(string json)
    {
        return JsonSerializer.Deserialize<EvaluateError>(json, JsonHelper.SerializerOptions)!;
    }
    #endregion
}

/// <summary>
///     Part of the <see cref="EvaluateError" /> class
/// </summary>
internal class EvaluateErrorResult
{
    #region Properties
    [JsonPropertyName("result")] public ExceptionClass? Result { get; set; }

    [JsonPropertyName("exceptionDetails")] public EvaluateErrorExceptionDetails? ExceptionDetails { get; set; }
    #endregion
}

/// <summary>
///     Part of the <see cref="EvaluateError" /> class
/// </summary>
internal class EvaluateErrorExceptionDetails
{
    #region Properties
    [JsonPropertyName("exceptionId")] public long ExceptionId { get; set; }

    [JsonPropertyName("text")] public string? Text { get; set; }

    [JsonPropertyName("lineNumber")] public long LineNumber { get; set; }

    [JsonPropertyName("columnNumber")] public long ColumnNumber { get; set; }

    [JsonPropertyName("scriptId")]
    [JsonConverter(typeof(EvaluateErrorParseStringConverter))]
    public long ScriptId { get; set; }

    [JsonPropertyName("exception")] public ExceptionClass Exception { get; set; } = null!;
    #endregion
}

/// <summary>
///     Part of the <see cref="EvaluateError" /> class
/// </summary>
internal class ExceptionClass
{
    #region Properties
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("subtype")] public string? Subtype { get; set; }

    [JsonPropertyName("className")] public string? ClassName { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("objectId")] public string? ObjectId { get; set; }
    #endregion
}

internal class EvaluateErrorParseStringConverter : JsonConverter<long>
{
    #region Read
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();
            case JsonTokenType.String:
            {
                var value = reader.GetString();
                if (long.TryParse(value, out var l))
                    return l;
                break;
            }
        }

        throw new JsonException("Cannot unmarshal type long");
    }
    #endregion

    #region Write
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
    #endregion
}
