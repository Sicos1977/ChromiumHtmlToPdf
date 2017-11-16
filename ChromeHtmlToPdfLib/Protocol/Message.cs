using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON message that is sent to Chrome
    /// </summary>
    public class Message : MessageBase
    {
        #region Properties
        /// <summary>
        /// The parameters that we want to feed into the <see cref="Method"/>
        /// </summary>
        [JsonProperty("params")]
        public Dictionary<string, object> Parameters { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates this object and sets it's needed properties
        /// </summary>
        public Message()
        {
            Id = 1;
            Parameters = new Dictionary<string, object>();
        }
        #endregion

        #region AddParaneter
        /// <summary>
        /// Add's a parameter to <see cref="Parameters"/>
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="value">The value</param>
        public void AddParameter(string name, object value)
        {
            Parameters.Add(name, value);   
        }
        #endregion

        #region ToJson
        /// <summary>
        /// Returns this object as a JSON string
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
            });
        }
        #endregion
    }
}
