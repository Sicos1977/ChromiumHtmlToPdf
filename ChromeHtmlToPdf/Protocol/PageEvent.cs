using Newtonsoft.Json;

namespace ChromeHtmlToPdf.Protocol
{
    public class PageEvent
    {
        #region Properties
        [JsonProperty("method")]
        public string Method { get; set; }

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
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}
