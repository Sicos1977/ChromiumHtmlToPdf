using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned when the <b>Fetch.enable</b> is activated in Chrome
    /// </summary>
    public class Fetch
    {
        #region Properties
        [JsonProperty("method")] 
        public string Method { get; set; }

        [JsonProperty("params")] 
        public FetchParams Params { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Fetch FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Fetch>(json);
        }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="Fetch"/> class
    /// </summary>
    public class FetchParams
    {
        #region Properties
        [JsonProperty("requestId")] 
        public string RequestId { get; set; }

        [JsonProperty("request")] 
        public FetchRequest Request { get; set; }

        [JsonProperty("frameId")] 
        public string FrameId { get; set; }

        [JsonProperty("resourceType")] 
        public string ResourceType { get; set; }

        [JsonProperty("networkId")] 
        public string NetworkId { get; set; }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="Fetch"/> class
    /// </summary>
    public class FetchRequest
    {
        #region Properties
        [JsonProperty("url")] 
        public string Url { get; set; }

        [JsonProperty("method")] 
        public string Method { get; set; }

        [JsonProperty("headers")] 
        public FetchHeaders Headers { get; set; }

        [JsonProperty("initialPriority")] 
        public string InitialPriority { get; set; }

        [JsonProperty("referrerPolicy")] 
        public string ReferrerPolicy { get; set; }
        #endregion
    }

    /// <summary>
    /// Part of the <see cref="FetchRequest"/> class
    /// </summary>
    public class FetchHeaders
    {
        #region Properties
        [JsonProperty("Accept")] 
        public string Accept { get; set; }
        #endregion
    }
}