//
// FileCacheHandler.cs
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


using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using ChromiumHtmlToPdfLib.Loggers;
using FileCacheManagers = ChromiumHtmlToPdfLib.FileCache.FileCacheManagers;

namespace ChromiumHtmlToPdfLib.Helpers;

/// <summary>
///     A <see cref="HttpClientHandler"/> that caches the response when requested
/// </summary>
internal class FileCacheHandler : HttpClientHandler
{
    #region Fields
    /// <summary>
    ///     When <c>true</c> then caching is enabled
    /// </summary>
    private readonly bool _useCache;

    /// <summary>
    ///     The cache folder
    /// </summary>
    private readonly DirectoryInfo? _cacheDirectory;

    /// <summary>
    ///     The cache size
    /// </summary>
    private readonly long _cacheSize;

    /// <summary>
    ///     <see cref="Logger"/>
    /// </summary>
    private readonly Logger? _logger;

    /// <summary>
    ///     <see cref="FileCache"/>
    /// </summary>
    private FileCache.FileCache? _fileCache;
    #endregion

    #region Properties
    /// <summary>
    ///     Returns <c>true</c> when the response is from the cache
    /// </summary>
    internal bool IsFromCache { get; set; }

    /// <summary>
    ///     Returns a file cache
    /// </summary>
    private FileCache.FileCache FileCache
    {
        get
        {
            if (_fileCache != null)
                return _fileCache;

            ChromiumHtmlToPdfLib.FileCache.FileCache.DefaultCacheManager = FileCacheManagers.Hashed;

            _fileCache = new FileCache.FileCache(_cacheDirectory!.FullName)
            {
                MaxCacheSize = _cacheSize,
                AccessTimeout = TimeSpan.FromSeconds(10),
                DefaultPolicy = new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) },
            };

            return _fileCache;
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets its needed properties
    /// </summary>
    /// <param name="useCache">When <c>true</c> then caching is enabled on the <see cref="WebClient" /></param>
    /// <param name="cacheDirectory">The cache directory when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="cacheSize">The cache size when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="logger"><see cref="Logger"/></param>
    internal FileCacheHandler(
        bool useCache,
        FileSystemInfo cacheDirectory,
        long cacheSize,
        Logger? logger)
    {
        _useCache = useCache;

        if (!useCache) return;

        _cacheDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "DocumentHelper"));
        _logger = logger;

        if (!_cacheDirectory.Exists)
        {
            _logger?.Info("Creating cache directory '{path}'", _cacheDirectory.FullName);
            _cacheDirectory.Create();
        }

        _cacheSize = cacheSize;
    }
    #endregion

    #region SendAsync
    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_useCache)
        {
            IsFromCache = false;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var key = request.RequestUri!.ToString();
        var item = FileCache.GetCacheItem(key);

        if (item is { Value: not null })
        {
            var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream((byte[])item.Value)),
                ReasonPhrase = "Loaded from cache"
            };

            IsFromCache = true;

            _logger?.Info("Returned item from cache");

            return cachedResponse;
        }

        IsFromCache = false;

        var response = base.SendAsync(request, cancellationToken).Result;
        var memoryStream = new MemoryStream();

        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#if (NETSTANDARD2_0)
        await contentStream.CopyToAsync(memoryStream).ConfigureAwait(false);
#else
        await contentStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
#endif

        FileCache.Add(key, memoryStream.ToArray(), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
        _logger?.Info("Added item to cache");

        response.Content = new StreamContent(new MemoryStream(memoryStream.ToArray()));

        return response;
    }
    #endregion
}
