using Newtonsoft.Json;

namespace ChromeHtmlToPdf.Protocol
{
    /// <summary>
    /// The base for a <see cref="Message"/>
    /// </summary>
    public class MessageBase
    {
        #region Properties
        /// <summary>
        /// The message id
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// The method that we want to execute in Chrome
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static MessageBase FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MessageBase>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }
}
