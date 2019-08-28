using System.Globalization;
using System.IO;
using System.Linq;

namespace ChromeHtmlToPdfLib.Helpers
{
    internal static class FileManager
    {
        #region RemoveInvalidFileNameChars
        /// <summary>
        /// Verwijdert illegale karakters uit een bestandsnaam
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string RemoveInvalidFileNameChars(string fileName)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(fileName, (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));
        }
        #endregion
    }
}
