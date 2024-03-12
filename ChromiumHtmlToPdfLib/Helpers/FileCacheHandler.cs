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
///     An <see cref="HttpClientHandler"/> that caches the response when requested
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
            _logger?.WriteToLog($"Creating cache directory '{_cacheDirectory.FullName}'");
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
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_useCache)
        {
            IsFromCache = false;
            return base.SendAsync(request, cancellationToken);
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

            _logger?.WriteToLog("Returned item from cache");

            return Task.FromResult(cachedResponse);
        }

        IsFromCache = false;
        
        var response = base.SendAsync(request, cancellationToken).Result;
        var memoryStream = new MemoryStream();
        
        response.Content.ReadAsStreamAsync().GetAwaiter().GetResult().CopyTo(memoryStream);
        
        FileCache.Add(key, memoryStream.ToArray(), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
        _logger?.WriteToLog("Added item to cache");

        response.Content = new StreamContent(new MemoryStream(memoryStream.ToArray()));

        return Task.FromResult(response);
    }
    #endregion
}