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
    DirectoryInfo _cacheFolder;
    #endregion

    internal FileCacheHandler(HttpClientHandler httpClientHandler, DirectoryInfo cacheFolder)
    {
        _httpClientHandler = httpClientHandler;
        _cacheFolder = cacheFolder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hash = GetMd5HashFromString(request.RequestUri.ToString());

        if (File.Exists(hash))
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FileStream(hash, FileMode.OpenOrCreate)),
                ReasonPhrase = "Loaded from cache"
            };
            return Task.FromResult(response);
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FileStream(hash, FileMode.OpenOrCreate)),
            ReasonPhrase = "Loaded from cache"
        };
        
        return Task.FromResult(response);

        //throw new NotImplementedException();
        return base.SendAsync(request, cancellationToken);
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
}