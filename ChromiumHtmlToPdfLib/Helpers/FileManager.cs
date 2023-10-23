using System.Globalization;
using System.IO;
using System.Linq;

namespace ChromiumHtmlToPdfLib.Helpers;

internal static class FileManager
{
    #region RemoveInvalidFileNameChars
    /// <summary>
    ///     Verwijdert illegale karakters uit een bestandsnaam
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string RemoveInvalidFileNameChars(string fileName)
    {
        return Path.GetInvalidFileNameChars()
            .Aggregate(fileName,
                (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));
    }
    #endregion

    #region GetFileSizeString
    /// <summary>
    ///     Geeft de size van een bestand in Windows formaat (GB, MB, KB, Bytes)
    /// </summary>
    /// <param name="bytes">Aantal bytes</param>
    /// <param name="cultureInfo"></param>
    /// <returns></returns>
    internal static string GetFileSizeString(double? bytes, CultureInfo cultureInfo)
    {
        bytes ??= 0;

        var size = bytes switch
        {
            >= 1073741824.0 => string.Format(cultureInfo, "{0:##.##}", bytes / 1073741824.0) + " GB",
            >= 1048576.0 => string.Format(cultureInfo, "{0:##.##}", bytes / 1048576.0) + " MB",
            >= 1024.0 => string.Format(cultureInfo, "{0:##.##}", bytes / 1024.0) + " KB",
            > 0 and < 1024.0 => bytes + " Bytes",
            _ => "0 Bytes"
        };

        return size;
    }
    #endregion
}