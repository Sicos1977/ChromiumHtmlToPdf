using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned when we create a new Target (page)
    /// </summary>
    public class Page
    {
        #region Properties
        /// <summary>
        /// The id
        /// </summary>
        [JsonProperty("id")]
        public long Id { get; set; }

        /// <summary>
        /// The result
        /// </summary>
        [JsonProperty("result")]
        public Result Result { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Page FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Page>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }

    public class Result
    {
        [JsonProperty("targetId")]
        public string TargetId { get; set; }
    }
}
