using System;
using System.IO;
using Newtonsoft.Json;
#pragma warning disable 1584,1711,1572,1581,1580
#pragma warning disable 1574

namespace ChromeHtmlToPdf.Protocol
{
    /// <summary>
    /// The JSON object that is returned from Chrome when calling the <see cref="Converter.ConvertToPdf"/> method
    /// </summary>
    public class PrintToPdfResponse
    {
        #region Properties
        /// <summary>
        /// <see cref="PrintToPdfResult"/>
        /// </summary>
        [JsonProperty("result")]
        public PrintToPdfResult Result { get; set; }

        /// <summary>
        /// Returns <see cref="PrintToPdfResult.Data"/> as a stream
        /// </summary>
        public Stream Stream
        {
            get { return new MemoryStream(Convert.FromBase64String(Result.Data)); }
        }
        #endregion

        #region SaveToFile
        /// <summary>
        /// Saves the <see cref="PrintToPdfResult.Data"/> to a file
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveToFile(string fileName)
        {
            File.WriteAllBytes(fileName, Convert.FromBase64String(Result.Data));
        }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static PrintToPdfResponse FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PrintToPdfResponse>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }

    /// <summary>
    /// The result returned from the <see cref="Converter.ConverToPdf"/> method
    /// </summary>
    public class PrintToPdfResult
    {
        /// <summary>
        /// The PDF as base64 string
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
