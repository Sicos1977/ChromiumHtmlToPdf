using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned from Chrome when navigation to a page
    /// </summary>
    public class PageNavigateResponse : MessageBase
    {
        #region Properties
        [JsonProperty("result")]
        public PageNavigateResponseResult Result { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public new static PageNavigateResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PageNavigateResponse>(json);
        }
        #endregion
    }

    public class PageNavigateResponseResult
    {
        #region Properties
        [JsonProperty("frameId")]
        public string FrameId { get; set; }

        [JsonProperty("loaderId")]
        public string LoaderId { get; set; }

        [JsonProperty("errorText")]
        public string ErrorText { get; set; }
        #endregion
    }
}