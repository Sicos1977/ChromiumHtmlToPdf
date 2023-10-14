using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace ChromiumHtmlToPdfLib.Helpers;

/// <summary>
///     An <see cref="HttpClientHandler"/> that caches the response when requested
/// </summary>
internal class FileCacheHandler : HttpClientHandler
{
    #region Properties
    /// <summary>
    ///     When <c>true</c> then caching is enabled
    /// </summary>
    private readonly bool _useCache;

    /// <summary>
    ///     The cache folder
    /// </summary>
    private DirectoryInfo _cacheDirectory;

    /// <summary>
    ///     The cache size
    /// </summary>
    private readonly long _cacheSize;

    /// <summary>
    ///     <see cref="FileCache"/>
    /// </summary>
    private FileCache _fileCache;
    #endregion

    #region Properties
    /// <summary>
    ///     Returns <c>true</c> when the response is from the cache
    /// </summary>
    internal bool IsFromCache { get; set; }

    /// <summary>
    ///     Returns a file cache
    /// </summary>
    private FileCache FileCache
    {
        get
        {
            if (_fileCache != null)
                return _fileCache;

            //_cacheDirectory = new DirectoryInfo(Path.Combine("d:\\", "HttpClientHandler"));
            _cacheDirectory = new DirectoryInfo(Path.Combine(_cacheDirectory.FullName, "HttpClientHandler"));

            if (!_cacheDirectory.Exists)
                _cacheDirectory.Create();

            FileCache.DefaultCacheManager = FileCacheManagers.Hashed;

            _fileCache = new FileCache(_cacheDirectory.FullName)
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
    internal FileCacheHandler(bool useCache, FileSystemInfo cacheDirectory, long cacheSize)
    {
        _useCache = useCache;
        if (!useCache) return;
        _cacheDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "HttpClientHandler"));
        _cacheSize = cacheSize;
    }
    #endregion

    #region SendAsync
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_useCache)
            return base.SendAsync(request, cancellationToken);

        var key = request.RequestUri.ToString();
        var item = FileCache.GetCacheItem(key);

        if (item is { Value: not null })
        {
            var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream((byte[])item.Value)),
                ReasonPhrase = "Loaded from cache"
            };

            return Task.FromResult(cachedResponse);
        }

        var response = base.SendAsync(request, cancellationToken).Result;
        var memoryStream = new MemoryStream();
        response.Content.ReadAsStreamAsync().GetAwaiter().GetResult().CopyTo(memoryStream);
        //WriteToLog($"Adding item from url '{sourceUri}' to the cache");
        FileCache.Add(key, memoryStream.ToArray(), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
        response.Content = new StreamContent(new MemoryStream(memoryStream.ToArray()));

        return Task.FromResult(response);
    }
    #endregion
}