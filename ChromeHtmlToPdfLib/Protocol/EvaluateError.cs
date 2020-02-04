//
// EvaluateError.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2019 Magic-Sessions. (www.magic-sessions.com)
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

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    ///     Returned by an <see cref="Evaluate" /> message when an error occurs
    /// </summary>
    public class EvaluateError : MessageBase
    {
        #region Properties
        [JsonProperty("result")]
        public EvaluateErrorResult Result { get; set; }
        #endregion

        #region FromJson
        public new static EvaluateError FromJson(string json)
        {
            return JsonConvert.DeserializeObject<EvaluateError>(json);
        }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="EvaluateError"/> class
    /// </summary>
    public class EvaluateErrorResult
    {
        #region Properties
        [JsonProperty("result")]
        public ExceptionClass Result { get; set; }

        [JsonProperty("exceptionDetails")]
        public EvaluateErrorExceptionDetails ExceptionDetails { get; set; }
        #endregion
    }
    
    /// <summary>
    /// Part of the <see cref="EvaluateError"/> class
    /// </summary>
    public class EvaluateErrorExceptionDetails
    {
        #region Properties
        [JsonProperty("exceptionId")]
        public long ExceptionId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("lineNumber")]
        public long LineNumber { get; set; }

        [JsonProperty("columnNumber")]
        public long ColumnNumber { get; set; }

        [JsonProperty("scriptId")]
        [JsonConverter(typeof(EvaluateErrorParseStringConverter))]
        public long ScriptId { get; set; }

        [JsonProperty("exception")]
        public ExceptionClass Exception { get; set; }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="EvaluateError"/> class
    /// </summary>
    public class ExceptionClass
    {
        #region Properties
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("subtype")]
        public string Subtype { get; set; }

        [JsonProperty("className")]
        public string ClassName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }
        #endregion
    }

    internal class EvaluateErrorParseStringConverter : JsonConverter
    {
        #region Properties
        public override bool CanConvert(Type t)
        {
            return t == typeof(long) || t == typeof(long?);
        }
        #endregion

        #region ReadJson
        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (long.TryParse(value, out var l))
                return l;
            throw new Exception("Cannot unmarshal type long");
        }
        #endregion

        #region WriteJson
        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var value = (long) untypedValue;
            serializer.Serialize(writer, value.ToString());
        }
        #endregion
    }
}