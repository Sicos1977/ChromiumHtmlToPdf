//
// ExpressionResponse.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON structure that is returned from Chrome when an expression is evaluated
    /// </summary>
    public class ExpressionResponse
    {
        #region Properties
        /// <summary>
        /// The message id
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// <see cref="ExpressionResult"/>
        /// </summary>
        [JsonProperty("result")]
        public ExpressionResult Result { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static ExpressionResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ExpressionResponse>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }

    /// <summary>
    /// The result for an expression
    /// </summary>
    public class ExpressionResult
    {
        #region Properties
        /// <summary>
        /// Returns an object when an exception occurs when Chrome evaluated the given epxression
        /// </summary>
        [JsonProperty("exceptionDetails")]
        public ExceptionDetails ExceptionDetails { get; set; }

        /// <summary>
        /// Returns the results for the given expression
        /// </summary>
        [JsonProperty("result")]
        public InnerResult InnerResult { get; set; }
        #endregion
    }

    /// <summary>
    /// The exact exception details for the expression that is sent to Chrome
    /// </summary>
    public class ExceptionDetails
    {
        #region Properties
        /// <summary>
        /// The column number where the exception occured
        /// </summary>
        [JsonProperty("columnNumber")]
        public long ColumnNumber { get; set; }

        /// <summary>
        /// <see cref="InnerResult"/>
        /// </summary>
        [JsonProperty("exception")]
        public InnerResult Exception { get; set; }

        /// <summary>
        /// The exception id
        /// </summary>
        [JsonProperty("exceptionId")]
        public long ExceptionId { get; set; }

        /// <summary>
        /// The line number where the exception occured
        /// </summary>
        [JsonProperty("lineNumber")]
        public long LineNumber { get; set; }

        /// <summary>
        /// The script <see cref="MessageBase.Id"/>
        /// </summary>
        [JsonProperty("scriptId")]
        public string ScriptId { get; set; }

        /// <summary>
        /// The text
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
        #endregion
    }

    public class InnerResult
    {
        #region Properties
        [JsonProperty("className")]
        public string ClassName { get; set; }


        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("subtype")]
        public string Subtype { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
        #endregion
    }
}
