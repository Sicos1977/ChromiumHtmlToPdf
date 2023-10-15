using Microsoft.Extensions.Logging;
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
    #region Fields
    /// <summary>
    ///     An unique id that can be used to identify the logging of the converter when
    ///     calling the code from multiple threads and writing all the logging to the same file
    /// </summary>
    private readonly string _instanceId;
    
    /// <summary>
    ///     When set then logging is written to this ILogger instance
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     Used to make the logging thread safe
    /// </summary>
    private readonly object _loggerLock = new();

    /// <summary>
    ///     When <c>true</c> then caching is enabled
    /// </summary>
    private readonly bool _useCache;

    /// <summary>
    ///     The cache folder
    /// </summary>
    private readonly DirectoryInfo _cacheDirectory;

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
    /// <summary>
    ///     Makes this object and sets its needed properties
    /// </summary>
    /// <param name="useCache">When <c>true</c> then caching is enabled on the <see cref="WebClient" /></param>
    /// <param name="cacheDirectory">The cache directory when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="cacheSize">The cache size when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="instanceId">An unique id that can be used to identify the logging of the converter when
    ///     calling the code from multiple threads and writing all the logging to the same file</param>
    /// <param name="logger">When set then logging is written to this ILogger instance for all conversions at the Information log level</param>
    internal FileCacheHandler(
        bool useCache, 
        FileSystemInfo cacheDirectory, 
        long cacheSize,
        string instanceId,
        ILogger logger)
    {
        _instanceId = instanceId;
        _logger = logger;
        _useCache = useCache;
        
        if (!useCache) return;
        
        _cacheDirectory = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, "DocumentHelper"));

        if (!_cacheDirectory.Exists)
        {
            WriteToLog($"Creating cache directory '{_cacheDirectory.FullName}'");
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

        var key = request.RequestUri.ToString();
        var item = FileCache.GetCacheItem(key);

        if (item is { Value: not null })
        {
            var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream((byte[])item.Value)),
                ReasonPhrase = "Loaded from cache"
            };

            IsFromCache = true;

            WriteToLog("Returned item from cache");

            return Task.FromResult(cachedResponse);
        }

        IsFromCache = false;
        
        var response = base.SendAsync(request, cancellationToken).Result;
        var memoryStream = new MemoryStream();
        
        response.Content.ReadAsStreamAsync().GetAwaiter().GetResult().CopyTo(memoryStream);
        
        FileCache.Add(key, memoryStream.ToArray(), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(1) });
        WriteToLog("Added item to cache");

        response.Content = new StreamContent(new MemoryStream(memoryStream.ToArray()));

        return Task.FromResult(response);
    }
    #endregion

    #region WriteToLog
    /// <summary>
    ///     Writes a line to the <see cref="_logger" />
    /// </summary>
    /// <param name="message">The message to write</param>
    internal void WriteToLog(string message)
    {
        lock (_loggerLock)
        {
            try
            {
                if (_logger == null) return;
                using (_logger.BeginScope(_instanceId))
                    _logger.LogInformation(message);
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }
    }
    #endregion
}