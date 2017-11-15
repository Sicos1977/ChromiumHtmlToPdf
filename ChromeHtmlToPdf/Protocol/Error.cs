using Newtonsoft.Json;

namespace ChromeHtmlToPdf.Protocol
{
    /// <summary>
    /// The JSON structure that is returned from Chrome when an Error occurs
    /// </summary>
    public class Error
    {
        #region Properties
        /// <summary>
        /// The message id
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// <see cref="InnerError"/>
        /// </summary>
        [JsonProperty("error")]
        public InnerError InnerError { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Error FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Error>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }

    /// <summary>
    /// The inner error
    /// </summary>
    public class InnerError
    {
        #region Properties
        /// <summary>
        /// The error code
        /// </summary>
        [JsonProperty("code")]
        public double Code { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
        #endregion
    }
}

