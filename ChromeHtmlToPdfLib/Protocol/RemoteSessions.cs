using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChromeHtmlToPdf.Protocol
{
    /// <summary>
    /// The JSON object that is returned from Chrome when calling the <see cref="Communicator.GetAvailableSessions"/> method
    /// </summary>
    public class RemoteSessionsResponse
    {
        #region Properties
        /// <summary>
        /// The message id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        
        /// <summary>
        /// A description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The url to use when we want to debug the Chrome session from Chrome itself
        /// </summary>
        [JsonProperty("devtoolsFrontendUrl")]
        public string DevtoolsFrontendUrl { get; set; }

        /// <summary>
        /// The title from the Chrome screen
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// The type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The url that is currently viewed
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// The websocket url to use when sending commands to Chrome
        /// </summary>
        [JsonProperty("webSocketDebuggerUrl")]
        public string WebSocketDebuggerUrl { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<RemoteSessionsResponse> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<List<RemoteSessionsResponse>>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }
}
