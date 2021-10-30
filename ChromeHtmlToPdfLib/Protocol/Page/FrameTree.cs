using System;
using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol.Page
{
    public class FrameTree
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("result")]
        public FrameTreeResponse Result { get; set; }

        public static FrameTree FromJson(string json) => JsonConvert.DeserializeObject<FrameTree>(json);

    }

    public class FrameTreeResponse
    {
        [JsonProperty("frameTree")]
        public FrameResponse FrameTree { get; set; }
    }


    public class FrameResponse
    {
        [JsonProperty("frame")]
        public FrameBody Frame { get; set; }
    }


    public class FrameBody
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("loaderId")]
        public string LoaderId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}

