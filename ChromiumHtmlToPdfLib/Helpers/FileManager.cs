//
// FileManager.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2026 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

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