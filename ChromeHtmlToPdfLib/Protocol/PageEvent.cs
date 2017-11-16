using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned when we dit request Chrome to send page events
    /// </summary>
    public class PageEvent
    {
        #region Properties
        /// <summary>
        /// The method executed by Chrome
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// The parameters used with this <see cref="Method"/>
        /// </summary>
        [JsonProperty("params")]
        public Params Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static PageEvent FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PageEvent>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }

    public class Params
    {
        /// <summary>
        /// The parameters name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The timestamp
        /// </summary>
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}
