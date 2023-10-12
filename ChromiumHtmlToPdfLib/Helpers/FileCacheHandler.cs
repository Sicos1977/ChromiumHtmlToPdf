using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChromiumHtmlToPdfLib.Helpers;

internal class FileCacheHandler : HttpClientHandler
{
    #region Properties
    /// <summary>
    ///     <see cref="HttpClientHandler"/>
    /// </summary>
    HttpClientHandler _httpClientHandler;

    /// <summary>
    ///     The cache folder
    /// </summary>
    readonly DirectoryInfo _cacheFolder;

    private readonly long _cacheSize;
    #endregion

    internal FileCacheHandler(HttpClientHandler httpClientHandler, FileSystemInfo cacheFolder, long cacheSize)
    {
        _httpClientHandler = httpClientHandler;
        _cacheFolder = new DirectoryInfo(Path.Combine(cacheFolder.FullName, "HttpClientHandler"));
        _cacheSize = cacheSize;

        if (!_cacheFolder.Exists)
            _cacheFolder.Create();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hash = GetMd5HashFromString(request.RequestUri.ToString());
        var cachedFile = new FileInfo(Path.Combine(_cacheFolder.FullName, hash));

        if (!cachedFile.Exists) 
            return base.SendAsync(request, cancellationToken);

        // TODO: Cache file

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(cachedFile.OpenRead()),
            ReasonPhrase = "Loaded from cache"
        };

        return Task.FromResult(response);
    }

    #region GetMd5HashFromString
    /// <summary>
    ///     Retourneert een MD5 hash voor de opgegeven <paramref name="value" />
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string GetMd5HashFromString(string value)
    {
        value ??= string.Empty;
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
        return bytes.Aggregate(string.Empty, (current, b) => current + b.ToString("X2"));
    }
    #endregion


    private static void DeleteOldestFilesIfFolderExceedsSizeLimit(string folderPath, long maxSizeInBytes)
    {
        var directoryInfo = new DirectoryInfo(folderPath);
        var files = directoryInfo.GetFiles();

        var currentFolderSize = files.Sum(file => file.Length);

        if (currentFolderSize <= maxSizeInBytes) return;
        {
            var oldestFiles = files.OrderBy(file => file.CreationTime).ToArray();

            foreach (var file in oldestFiles)
            {
                try
                {
                    file.Delete();
                    currentFolderSize -= file.Length;

                    if (currentFolderSize <= maxSizeInBytes)
                        break;
                }
                catch (Exception exception)
                {
                    throw new Exception($"Failed to delete file: {file.FullName} - {exception.Message}");
                }
            }
        }
    }
}