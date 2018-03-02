using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned when we create a new Target (page)
    /// </summary>
    public class Page : MessageBase
    {
        #region Properties
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
        public new static Page FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Page>(json);
        }
        #endregion
    }

    public class Result
    {
        #region Propertie
        [JsonProperty("targetId")]
        public string TargetId { get; set; }
        #endregion
    }
}
