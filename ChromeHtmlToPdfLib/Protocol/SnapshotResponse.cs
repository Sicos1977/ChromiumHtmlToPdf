using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    ///     Placeholder for the result of a page snapshot
    /// </summary>
    public class SnapshotResponse
    {
        #region Properties
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("result")]
        public SnapshotResult Result { get; set; }

        /// <summary>
        /// Returns <see cref="PrintToPdfResult.Data"/> as array of bytes
        /// </summary>
        public byte[] Bytes => Encoding.ASCII.GetBytes(Result.Data);
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static SnapshotResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SnapshotResponse>(json);
        }
        #endregion
    }

    /// <summary>
    ///     Part of the <see cref="SnapshotResponse"/> class
    /// </summary>
    public class SnapshotResult
    {
        #region Properties
        [JsonProperty("data")]
        public string Data { get; set; }
        #endregion
    }
}
