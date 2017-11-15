using Newtonsoft.Json;

namespace ChromeHtmlToPdf.Protocol
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
        /// The script <see cref="Message.Id"/>
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
