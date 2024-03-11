/*
3Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable UnusedType.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable MethodOverloadWithOptionalParameter

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     A file-based cache that can be used to store objects, files, or raw bytes.
/// </summary>
public class FileCache : ObjectCache
{
    #region Enum PayloadMode
    /// <summary>
    ///     Specified how the cache payload is to be handled.
    /// </summary>
    public enum PayloadMode
    {
        /// <summary>
        ///     Treat the payload a a serializable object.
        /// </summary>
        Serializable,

        /// <summary>
        ///     Treat the payload as a file name. File content will be copied on add, while get returns the file name.
        /// </summary>
        Filename,

        /// <summary>
        ///     Treat the payload as raw bytes. A byte[] and readable streams are supported on add.
        /// </summary>
        RawBytes
    }
    #endregion

    #region Consts
    private const string LastCleanedDateFile = "cache.lcd";
    private const string CacheSizeFile = "cache.size";
    // This is a file used to prevent multiple processes from trying to "clean" at the same time
    private const string SemaphoreFile = "cache.sem";
    private const string CacheSubFolder = "cache";
    private const string PolicySubFolder = "policy";
    #endregion

    #region Delegates
    /// <summary>
    ///     Event that will be called when <see cref="MaxCacheSize" /> is reached.
    /// </summary>
    public event EventHandler<FileCacheEventArgs> MaxCacheSizeReached = delegate { };

    /// <summary>
    ///     Raised when the cache is resized.
    /// </summary>
    public event EventHandler<FileCacheEventArgs> CacheResized = delegate { };
    #endregion

    #region Fields
    private static int _nameCounter = 1;
    private SerializationBinder _binder;
    private TimeSpan _cleanInterval = new(0, 0, 0, 0); // default to 1 week
    private long _currentCacheSize;
    private string _name = "";

    /// <summary>
    ///     The amount of time before expiry that a filename will be used as a payload. I.e.
    ///     the amount of time the cache's user can safely use the file delivered as a payload.
    ///     Default 10 minutes.
    /// </summary>
    public TimeSpan FilenameAsPayloadSafetyMargin = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     The default cache path used by FC.
    /// </summary>
    private string DefaultCachePath => Path.Combine(Directory.GetCurrentDirectory(), "FileCache");
    #endregion

    #region Properties
    /// <summary>
    ///     The cache's root file path
    /// </summary>
    public string CacheDir { get; private set; }

    /// <summary>
    ///     Allows for the setting of the default cache manager so that it doesn't have to be
    ///     specified on every instance creation.
    /// </summary>
    public static FileCacheManagers DefaultCacheManager { get; set; } = FileCacheManagers.Basic;

    /// <summary>
    ///     Used to abstract away the low-level details of file management.  This allows
    ///     for multiple file formatting schemes based on use case.
    /// </summary>
    public FileCacheManager CacheManager { get; protected set; }

    /// <summary>
    ///     Used to store the default region when accessing the cache via [] calls
    /// </summary>
    public string? DefaultRegion { get; set; }

    /// <summary>
    ///     Used to set the default policy when setting cache values via [] calls
    /// </summary>
    public CacheItemPolicy DefaultPolicy { get; set; }

    /// <summary>
    ///     Specified whether the payload is deserialized or just the file name.
    /// </summary>
    public PayloadMode PayloadReadMode { get; set; } = PayloadMode.Serializable;

    /// <summary>
    ///     Specified how the payload is to be handled on add operations.
    /// </summary>
    public PayloadMode PayloadWriteMode { get; set; } = PayloadMode.Serializable;

    /// <summary>
    ///     Used to determine how long the FileCache will wait for a file to become
    ///     available.  Default (00:00:00) is indefinite.  Should the timeout be
    ///     reached, an exception will be thrown.
    /// </summary>
    public TimeSpan AccessTimeout
    {
        get => CacheManager.AccessTimeout;
        set => CacheManager.AccessTimeout = value;
    }

    /// <summary>
    ///     Used to specify the disk size, in bytes, that can be used by the File Cache
    /// </summary>
    public long MaxCacheSize { get; set; }

    /// <summary>
    ///     Returns the approximate size of the file cache
    /// </summary>
    public long CurrentCacheSize
    {
        get
        {
            // if this is the first query, we need to load the cache size from somewhere
            if (_currentCacheSize == 0)
            {
                // Read the system file for cache size
                if (CacheManager.ReadSysValue(CacheSizeFile, out long cacheSize))
                    // Did we successfully get data from the file? Write it to our member var.
                    _currentCacheSize = cacheSize;
            }

            return _currentCacheSize;
        }
        private set
        {
            // no need to do a pointless re-store of the same value
            if (_currentCacheSize == value && value != 0) return;
            CacheManager.WriteSysValue(CacheSizeFile, value);
            _currentCacheSize = value;
        }
    }
    #endregion

    #region WriteHelper
    private void WriteHelper(PayloadMode mode, string key, FileCachePayload data, string? regionName = null,
        bool policyUpdateOnly = false)
    {
        CurrentCacheSize += CacheManager.WriteFile(mode, key, data, regionName, policyUpdateOnly);

        //check to see if limit was reached
        if (CurrentCacheSize > MaxCacheSize)
            MaxCacheSizeReached(this, new FileCacheEventArgs(CurrentCacheSize, MaxCacheSize));
    }
    #endregion

    #region Private class LocalCacheBinder
    private class LocalCacheBinder : SerializationBinder
    {
        /// <inheritdoc />
        public override Type? BindToType(string assemblyName, string typeName)
        {
            var currentAssembly = Assembly.GetAssembly(typeof(LocalCacheBinder))?.FullName;

            // Get the type using the typeName and assemblyName
            var typeToDeserialize = Type.GetType(string.Format("{0}, {1}",
                typeName, currentAssembly));

            return typeToDeserialize;
        }
    }
    #endregion

    #region Private class CacheItemReference
    // CT: This private class is used to help shrink the cache.
    // It computes the total size of an entry including it's policy file.
    // It also implements IComparable functionality to allow for sorting based on access time
    private class CacheItemReference : IComparable<CacheItemReference>
    {
        public readonly string Key;
        // ReSharper disable once MemberCanBePrivate.Local
        public readonly DateTime LastAccessTime;
        public readonly long Length;
        public readonly string? Region;

        public int CompareTo(CacheItemReference? other)
        {
            if (other == null) return 1;

            var i = LastAccessTime.CompareTo(other.LastAccessTime);

            // It's possible, although rare, that two different items will have
            // the same LastAccessTime. So in that case, we need to check to see
            // if they're actually the same.
            if (i != 0) return i;
            // second order should be length (but from smallest to largest,
            // that way we delete smaller files first)
            i = -1 * Length.CompareTo(other.Length);
            if (i != 0) return i;
            i = string.CompareOrdinal(Region, other.Region);
            if (i == 0) i = string.Compare(Key, other.Key, StringComparison.Ordinal);

            return i;
        }

        public CacheItemReference(string key, string? region, string cachePath, string policyPath)
        {
            Key = key;
            Region = region;
            var cfi = new FileInfo(cachePath);
            var pfi = new FileInfo(policyPath);
            cfi.Refresh();
            LastAccessTime = cfi.LastAccessTime;
            Length = cfi.Length + pfi.Length;
        }

        public static bool operator >(CacheItemReference lhs, CacheItemReference rhs)
        {
            return lhs.CompareTo(rhs) > 0;
        }

        public static bool operator <(CacheItemReference lhs, CacheItemReference rhs)
        {
            return lhs.CompareTo(rhs) < 0;
        }
    }
    #endregion

    #region Constructors
    /// <summary>
    ///     Creates a default instance of the file cache using the supplied file cache manager.
    /// </summary>
    /// <param name="manager"></param>
    public FileCache(FileCacheManagers manager)
    {
        Init(manager);
    }

    /// <summary>
    ///     Creates a default instance of the file cache.  Don't use if you plan to serialize custom objects
    /// </summary>
    /// <param name="calculateCacheSize">
    ///     If true, will calculates the cache's current size upon new object creation.
    ///     Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on
    ///     use case.
    /// </param>
    /// <param name="cleanInterval">If supplied, sets the interval of time that must occur between self cleans</param>
    public FileCache(
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new()
    )
    {
        // CT note: I moved this code to an init method because if the user specified a cache root, that needs to
        // be set before checking if we should clean (otherwise it will look for the file in the wrong place)
        Init(DefaultCacheManager, calculateCacheSize, cleanInterval);
    }

    /// <summary>
    ///     Creates an instance of the file cache using the supplied path as the root save path.
    /// </summary>
    /// <param name="cacheRoot">The cache's root file path</param>
    /// <param name="calculateCacheSize">
    ///     If true, will calculates the cache's current size upon new object creation.
    ///     Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on
    ///     use case.
    /// </param>
    /// <param name="cleanInterval">If supplied, sets the interval of time that must occur between self cleans</param>
    public FileCache(
        string cacheRoot,
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new())
    {
        CacheDir = cacheRoot;
        Init(DefaultCacheManager, calculateCacheSize, cleanInterval);
    }

    /// <summary>
    ///     Creates an instance of the file cache.
    /// </summary>
    /// <param name="binder">
    ///     The SerializationBinder used to deserialize cached objects.  Needed if you plan
    ///     to cache custom objects.
    /// </param>
    /// <param name="calculateCacheSize">
    ///     If true, will calculates the cache's current size upon new object creation.
    ///     Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on
    ///     use case.
    /// </param>
    /// <param name="cleanInterval">If supplied, sets the interval of time that must occur between self cleans</param>
    public FileCache(
        SerializationBinder binder,
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new()
    )
    {
        _binder = binder;
        Init(DefaultCacheManager, calculateCacheSize, cleanInterval);
    }

    /// <summary>
    ///     Creates an instance of the file cache.
    /// </summary>
    /// <param name="cacheRoot">The cache's root file path</param>
    /// <param name="binder">
    ///     The SerializationBinder used to deserialize cached objects.  Needed if you plan
    ///     to cache custom objects.
    /// </param>
    /// <param name="calculateCacheSize">
    ///     If true, will calculates the cache's current size upon new object creation.
    ///     Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on
    ///     use case.
    /// </param>
    /// <param name="cleanInterval">If supplied, sets the interval of time that must occur between self cleans</param>
    public FileCache(
        string cacheRoot,
        SerializationBinder binder,
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new()
    )
    {
        _binder = binder;
        CacheDir = cacheRoot;
        Init(DefaultCacheManager, calculateCacheSize, cleanInterval);
    }

    /// <summary>
    ///     Creates an instance of the file cache.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="cacheRoot">The cache's root file path</param>
    /// <param name="binder">
    ///     The SerializationBinder used to deserialize cached objects.  Needed if you plan
    ///     to cache custom objects.
    /// </param>
    /// <param name="calculateCacheSize">
    ///     If true, will calculates the cache's current size upon new object creation.
    ///     Turned off by default as directory traversal is somewhat expensive and may not always be necessary based on
    ///     use case.
    /// </param>
    /// <param name="cleanInterval">If supplied, sets the interval of time that must occur between self cleans</param>
    public FileCache(
        FileCacheManagers manager,
        string cacheRoot,
        SerializationBinder binder,
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new()
    )
    {
        _binder = binder;
        CacheDir = cacheRoot;
        Init(manager, calculateCacheSize, cleanInterval);
    }
    #endregion

    #region Init
    [MemberNotNull(nameof(_binder), nameof(CacheDir), nameof(DefaultPolicy), nameof(CacheManager))]
    private void Init(
        FileCacheManagers manager,
        bool calculateCacheSize = false,
        TimeSpan cleanInterval = new())
    {
        _name = $"FileCache_{_nameCounter}";
        _nameCounter++;

        DefaultRegion = null;
        DefaultPolicy = new CacheItemPolicy();
        MaxCacheSize = long.MaxValue;

        // set default values if not already set
        CacheDir ??= DefaultCachePath;
        _binder ??= new FileCacheBinder();

        // if it doesn't exist, we need to make it
        if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir!);

        // only set the clean interval if the user supplied it
        if (cleanInterval > new TimeSpan()) _cleanInterval = cleanInterval;

        //set up cache manager
        CacheManager = FileCacheManagerFactory.Create(manager, CacheDir, CacheSubFolder, PolicySubFolder);
        CacheManager.Binder = _binder;
        CacheManager.AccessTimeout = new TimeSpan();

        //check to see if cache is in need of immediate cleaning
        if (ShouldClean())
            CleanCacheAsync();
        else if (calculateCacheSize || CurrentCacheSize == 0)
            // This is in an else if block, because CleanCacheAsync will
            // update the cache size, so no need to do it twice.
            UpdateCacheSizeAsync();

        MaxCacheSizeReached += FileCacheMaxCacheSizeReached;
    }
    #endregion

    #region FileCacheMaxCacheSizeReached
    private void FileCacheMaxCacheSizeReached(object? sender, FileCacheEventArgs e)
    {
        Task.Factory.StartNew(() =>
        {
            // Shrink the cache to 75% of the max size
            // that way there's room for it to grow a bit
            // before we have to do this again.
            ShrinkCacheToSize((long)(MaxCacheSize * 0.75));
        });
    }
    #endregion

    #region GetCleaningLock
    /// <summary>
    ///     Returns the clean lock file if it can be opened, otherwise it is being used by another process so return null
    /// </summary>
    /// <returns></returns>
    private FileStream? GetCleaningLock()
    {
        try
        {
            return File.Open(Path.Combine(CacheDir, SemaphoreFile), FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None);
        }
        catch (Exception)
        {
            return null;
        }
    }
    #endregion

    #region ShouldClean
    /// <summary>
    ///     Determines whether or not enough time has passed that the cache should clean itself
    /// </summary>
    /// <returns></returns>
    private bool ShouldClean()
    {
        try
        {
            // if the file can't be found, or is corrupt this will throw an exception
            if (!CacheManager.ReadSysValue(LastCleanedDateFile, out DateTime lastClean))
                //AC: rewrote to be safer in cases where no value obtained.
                return true;

            // return true if the amount of time between now and the last clean is greater than or equal to the
            // clean interval, otherwise return false.
            return DateTime.Now - lastClean >= _cleanInterval;
        }
        catch (Exception)
        {
            return true;
        }
    }
    #endregion

    #region ShrinkCacheToSize
    /// <summary>
    ///     Shrinks the cache until the cache size is less than
    ///     or equal to the size specified (in bytes). This is a
    ///     rather expensive operation, so use with discretion.
    /// </summary>
    /// <returns>The new size of the cache</returns>
    public long ShrinkCacheToSize(long newSize, string? regionName = null)
    {
        long originalSize;
        long removed;

        //lock down other treads from trying to shrink or clean
        using (var cLock = GetCleaningLock())
        {
            if (cLock == null) return -1;

            // if we're shrinking the whole cache, we can use the stored
            // size if it's available. If it's not available we calculate it and store
            // it for next time.
            if (regionName == null)
            {
                if (CurrentCacheSize == 0) CurrentCacheSize = GetCacheSize();

                originalSize = CurrentCacheSize;
            }
            else
            {
                originalSize = GetCacheSize(regionName);
            }

            // Find out how much we need to get rid of
            var amount = originalSize - newSize;

            // CT note: This will update CurrentCacheSize
            removed = DeleteOldestFiles(amount, regionName);

            // unlock the semaphore for others
            cLock.Close();
        }

        // trigger the event
        CacheResized(this, new FileCacheEventArgs(originalSize - removed, MaxCacheSize));

        // return the final size of the cache (or region)
        
        return originalSize - removed;
    }
    #endregion

    #region CleanCacheAsync
    /// <summary>
    ///     Cleans the cache on a separate thread
    /// </summary>
    public void CleanCacheAsync()
    {
        Task.Factory.StartNew(() => { CleanCache(); });
    }
    #endregion

    #region CleanCache
    /// <summary>
    ///     Loop through the cache and delete all expired files
    /// </summary>
    /// <returns>The amount removed (in bytes)</returns>
    public long CleanCache(string? regionName = null)
    {
        long removed = 0;

        //lock down other treads from trying to shrink or clean
        using var cLock = GetCleaningLock();
        if (cLock == null)
            return 0;

        var regions =
            !string.IsNullOrEmpty(regionName)
                ? new List<string>(1) { regionName! }
                : CacheManager.GetRegions();

        foreach (var region in regions)
        foreach (var key in GetKeys(region))
        {
            var policy = GetPolicy(key, region);
            if (policy.AbsoluteExpiration < DateTime.Now)
                try
                {
                    var cachePath = CacheManager.GetCachePath(key, region);
                    var policyPath = CacheManager.GetPolicyPath(key, region);
                    var ci = new CacheItemReference(key, region, cachePath, policyPath);
                    Remove(key, region); // CT note: Remove will update CurrentCacheSize
                    removed += ci.Length;
                }
                catch (Exception) 
                {
                    // Skip if the file cannot be accessed
                }
        }

        // mark that we've cleaned the cache
        CacheManager.WriteSysValue(LastCleanedDateFile, DateTime.Now);

        // unlock
        cLock.Close();

        return removed;
    }
    #endregion

    #region DeleteOldestFiles
    /// <summary>
    ///     Delete the oldest items in the cache to shrink the cache by the
    ///     specified amount (in bytes).
    /// </summary>
    /// <returns>The amount of data that was actually removed</returns>
    private long DeleteOldestFiles(long amount, string? regionName = null)
    {
        // Verify that we actually need to shrink
        if (amount <= 0) return 0;

        //Heap of all CacheReferences
        var cacheReferences = new PriorityQueue<CacheItemReference>();

        var regions =
            !string.IsNullOrEmpty(regionName)
                ? new List<string>(1) { regionName! }
                : CacheManager.GetRegions();

        foreach (var region in regions)
            //build a heap of all files in cache region
        foreach (var key in GetKeys(region))
            try
            {
                //build item reference
                var cachePath = CacheManager.GetCachePath(key, region);
                var policyPath = CacheManager.GetPolicyPath(key, region);
                var ci = new CacheItemReference(key, region, cachePath, policyPath);
                cacheReferences.Enqueue(ci);
            }
            catch (FileNotFoundException)
            {
            }

        //remove cache items until size requirement is met
        long removedBytes = 0;
        while (removedBytes < amount && cacheReferences.GetSize() > 0)
        {
            //remove oldest item
            var oldest = cacheReferences.Dequeue();
            removedBytes += oldest.Length;
            Remove(oldest.Key, oldest.Region);
        }

        return removedBytes;
    }
    #endregion

    #region UpdateCacheSizeAsync
    /// <summary>
    ///     This method calls GetCacheSize on a separate thread to
    ///     calculate and then store the size of the cache.
    /// </summary>
    public void UpdateCacheSizeAsync()
    {
        Task.Factory.StartNew(() => { CurrentCacheSize = GetCacheSize(); });
    }
    #endregion

    #region GetCacheSize
    //AC Note: From MSDN / SO (http://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net)
    /// <summary>
    ///     Calculates the size, in bytes of the file cache
    /// </summary>
    /// <param name="regionName">The region to calculate.  If NULL, will return total size.</param>
    /// <returns></returns>
    public long GetCacheSize(string? regionName = null)
    {
        long size = 0;

        //AC note: First parameter is unused, so just pass in garbage ("DummyValue")
        var policyPath = Path.GetDirectoryName(CacheManager.GetPolicyPath("DummyValue", regionName))!;
        var cachePath = Path.GetDirectoryName(CacheManager.GetCachePath("DummyValue", regionName))!;
        size += CacheSizeHelper(new DirectoryInfo(policyPath));
        size += CacheSizeHelper(new DirectoryInfo(cachePath));
        return size;
    }
    #endregion

    #region CacheSizeHelper
    /// <summary>
    ///     Helper method for public <see cref="GetCacheSize" />.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private long CacheSizeHelper(DirectoryInfo root)
    {
        // Add file sizes.
        var fis = root.EnumerateFiles();
        var size = fis.Sum(fi => fi.Length);

        // Add subdirectory sizes.
        var dis = root.EnumerateDirectories();
        size += dis.Sum(CacheSizeHelper);

        return size;
    }
    #endregion

    #region Clear
    /// <summary>
    ///     Clears all FileCache-related items from the disk.  Throws an exception if the cache can't be
    ///     deleted.
    /// </summary>
    public void Clear()
    {
        //Before we can delete the entire file tree, we have to wait for any latent writes / reads to finish
        //To do this, we wait for access to our cacheLock file.  When we get access, we have to immediately
        //release it (can't delete a file that is open!), which somewhat muddies our condition of needing
        //exclusive access to the FileCache.  However, the time between closing and making the call to
        //delete is so small that we probably won't run into an exception most of the time.
        FileStream? cacheLock = null;
        var totalTime = new TimeSpan(0);
        var interval = new TimeSpan(0, 0, 0, 0, 50);
        var timeToWait = AccessTimeout;
        if (AccessTimeout == new TimeSpan())
            //if access timeout is not set, make really large wait time
            timeToWait = new TimeSpan(5, 0, 0);
        while (cacheLock == null && timeToWait > totalTime)
        {
            cacheLock = GetCleaningLock();
            Thread.Sleep(interval);
            totalTime += interval;
        }

        if (cacheLock == null)
            throw new TimeoutException("FileCache AccessTimeout reached when attempting to clear cache.");
        cacheLock.Close();

        //now that we've waited for everything to stop, we can delete the cache directory.
        Directory.Delete(CacheDir, true);
    }
    #endregion

    #region Flush
    /// <summary>
    ///     Flushes the file cache using DateTime.Now as the minimum date
    /// </summary>
    /// <param name="regionName"></param>
    public void Flush(string? regionName = null)
    {
        Flush(DateTime.Now, regionName);
    }
    #endregion

    #region Flush
    /// <summary>
    ///     Flushes the cache based on last access date, filtered by optional region
    /// </summary>
    /// <param name="minDate"></param>
    /// <param name="regionName"></param>
    public void Flush(DateTime minDate, string? regionName = null)
    {
        // prevent other threads from altering stuff while we delete junk
        using var cLock = GetCleaningLock();
        if (cLock == null) return;

        var regions =
            !string.IsNullOrEmpty(regionName)
                ? new List<string>(1) { regionName! }
                : CacheManager.GetRegions();

        foreach (var region in regions)
        {
            var keys = CacheManager.GetKeys(region);
            foreach (var key in keys)
            {
                var policyPath = CacheManager.GetPolicyPath(key, region);
                var cachePath = CacheManager.GetCachePath(key, region);

                // Update the Cache size before flushing this item.
                CurrentCacheSize = GetCacheSize();

                //if either policy or cache are stale, delete both
                if (File.GetLastAccessTime(policyPath) < minDate || File.GetLastAccessTime(cachePath) < minDate)
                    CurrentCacheSize -= CacheManager.DeleteFile(key, region);
            }
        }

        // unlock
        cLock.Close();
    }
    #endregion

    #region GetPolicy
    /// <summary>
    ///     Returns the policy attached to a given cache item.
    /// </summary>
    /// <param name="key">The key of the item</param>
    /// <param name="regionName">The region in which the key exists</param>
    /// <returns></returns>
    public CacheItemPolicy GetPolicy(string key, string? regionName = null)
    {
        var policy = new CacheItemPolicy();
        var payload = CacheManager.ReadFile(PayloadMode.Filename, key, regionName);
        try
        {
            policy.SlidingExpiration = payload.Policy.SlidingExpiration;
            policy.AbsoluteExpiration = payload.Policy.AbsoluteExpiration;
        }
        catch (Exception)
        {
            // Ignore
        }

        return policy;
    }
    #endregion

    #region GetKeys
    /// <summary>
    ///     Returns the keys in the cache
    /// </summary>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public IEnumerable<string> GetKeys(string? regionName = null)
    {
        return CacheManager.GetKeys(regionName);
    }
    #endregion

    #region AddOrGetExisting
    /// <summary>
    ///     Adds an item to the cache or gets the item if it already exists
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="policy"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override object? AddOrGetExisting(string key, object value, CacheItemPolicy policy, string? regionName = null)
    {
        var path = CacheManager.GetCachePath(key, regionName);
        object? oldData = null;

        //pull old value if it exists
        if (File.Exists(path))
            try
            {
                oldData = Get(key, regionName);
            }
            catch (Exception)
            {
                oldData = null;
            }

        var cachePolicy = new SerializableCacheItemPolicy(policy);
        var newPayload = new FileCachePayload(value, cachePolicy);
        WriteHelper(PayloadWriteMode, key, newPayload, regionName);

        //As documented in the spec (http://msdn.microsoft.com/en-us/library/dd780602.aspx), return the old
        //cached value or null
        return oldData;
    }

    /// <summary>
    ///     Adds an item to the cache or gets the item if it already exists
    /// </summary>
    /// <param name="value"></param>
    /// <param name="policy"></param>
    /// <returns></returns>
    public override CacheItem? AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
    {
        var oldData = AddOrGetExisting(value.Key, value.Value, policy, value.RegionName);
        CacheItem? returnItem = null;

        if (oldData != null)
            returnItem = new CacheItem(value.Key)
            {
                Value = oldData,
                RegionName = value.RegionName
            };
        return returnItem;
    }

    /// <summary>
    ///     Adds an item to the cache or gets the item if it already exists
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpiration"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override object? AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration,
        string? regionName = null)
    {
        var policy = new CacheItemPolicy
        {
            AbsoluteExpiration = absoluteExpiration
        };
        return AddOrGetExisting(key, value, policy, regionName);
    }
    #endregion

    #region Contains
    /// <summary>
    ///     Returns <c>true</c> when the cache contains an item with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override bool Contains(string key, string? regionName = null)
    {
        var path = CacheManager.GetCachePath(key, regionName);
        return File.Exists(path);
    }
    #endregion

    #region CreateCacheEntryChangeMonitor
    /// <summary>
    ///     Not implemented!!
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string? regionName = null)
    {
        throw new NotImplementedException();
    }
    #endregion

    /// <summary>
    ///     The default cache capabilities of the file cache
    /// </summary>
    public override DefaultCacheCapabilities DefaultCacheCapabilities => DefaultCacheCapabilities.CacheRegions | DefaultCacheCapabilities.AbsoluteExpirations | DefaultCacheCapabilities.SlidingExpirations;

    #region Get
    /// <summary>
    ///     Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override object? Get(string key, string? regionName = null)
    {
        var payload = CacheManager.ReadFile(PayloadReadMode, key, regionName);
        CacheManager.GetCachePath(key, regionName);

        var cutoff = DateTime.Now;
        if (PayloadReadMode == PayloadMode.Filename) cutoff += FilenameAsPayloadSafetyMargin;

        //null payload?
        if (payload.Policy != null && payload.Payload != null)
        {
            //did the item expire?
            if (payload.Policy.AbsoluteExpiration < cutoff)
            {
                //set the payload to null
                payload.Payload = null;

                //delete the file from the cache
                try
                {
                    // CT Note: I changed this to Remove from File.Delete so that the corresponding
                    // policy file will be deleted as well, and CurrentCacheSize will be updated.
                    Remove(key, regionName);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
            else
            {
                //does the item have a sliding expiration?
                if (payload.Policy.SlidingExpiration > new TimeSpan())
                {
                    payload.Policy.AbsoluteExpiration = DateTime.Now.Add(payload.Policy.SlidingExpiration);
                    WriteHelper(PayloadWriteMode, key, payload, regionName, true);
                }
            }
        }
        else
        {
            //remove null payload
            Remove(key, regionName);

            //create dummy one for return
            payload = new FileCachePayload(null);
        }

        return payload.Payload;
    }
    #endregion

    #region GetCacheItem
    /// <summary>
    ///     Gets the cache item associated with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override CacheItem GetCacheItem(string key, string? regionName = null)
    {
        var value = Get(key, regionName);
        var item = new CacheItem(key)
        {
            Value = value,
            RegionName = regionName
        };
        return item;
    }
    #endregion

    #region GetCount
    /// <summary>
    ///     Returns the amount of items in the cache
    /// </summary>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override long GetCount(string? regionName = null)
    {
        regionName ??= string.Empty;
        var path = Path.Combine(CacheDir, CacheSubFolder, regionName);
        return Directory.Exists(path) ? Directory.GetFiles(path).Length : 0;
    }
    #endregion

    #region GetEnumerator
    /// <summary>
    ///     Returns an enumerator for the specified region (defaults to base-level cache directory).
    ///     This function *WILL NOT* recursively locate files in subdirectories.
    /// </summary>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator(string? regionName = null)
    {
        //AC: This seems inefficient.  Wouldn't it be better to do this using a cursor?
        var enumerator = new List<KeyValuePair<string, object?>>();

        var keys = CacheManager.GetKeys(regionName);
        foreach (var key in keys) 
            enumerator.Add(new KeyValuePair<string, object?>(key, Get(key, regionName)));
        // ReSharper disable once NotDisposedResourceIsReturned
        return enumerator.GetEnumerator();
    }

    /// <summary>
    ///     Will return an enumerator with all cache items listed in the root file path ONLY
    /// </summary>
    /// <returns></returns>
    protected override IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    #region GetValues
    /// <summary>
    ///     Gets the values in the cache with the specified keys.
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override IDictionary<string, object?> GetValues(IEnumerable<string> keys, string? regionName = null)
    {
        var values = new Dictionary<string, object?>();
        foreach (var key in keys) values[key] = Get(key, regionName);
        return values;
    }
    #endregion

    #region Properties
    /// <summary>
    ///     Returns the name of the cache item
    /// </summary>
    public override string Name => _name;
    #endregion

    #region Remove
    /// <summary>
    ///     Removes the cache item from the cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override object? Remove(string key, string? regionName = null)
    {
        object? valueToDelete = null;

        if (!Contains(key, regionName)) return null;
        // Because of the possibility of multiple threads accessing this, it's possible that
        // while we're trying to remove something, another thread has already removed it.
        try
        {
            //remove cache entry
            // CT note: calling Get from remove leads to an infinite loop and stack overflow,
            // so I replaced it with a simple CacheManager.ReadFile call. None of the code here actually
            // uses this object returned, but just in case someone elses outside code does.
            var fcp = CacheManager.ReadFile(PayloadMode.Filename, key, regionName);
            valueToDelete = fcp.Payload;
            var path = CacheManager.GetCachePath(key, regionName);
            CurrentCacheSize -= new FileInfo(path).Length;
            File.Delete(path);

            //remove policy file
            var cachedPolicy = CacheManager.GetPolicyPath(key, regionName);
            CurrentCacheSize -= new FileInfo(cachedPolicy).Length;
            File.Delete(cachedPolicy);
        }
        catch (IOException)
        {
        }

        return valueToDelete;
    }
    #endregion

    #region Get
    /// <summary>
    ///     Sets the value of the cache item with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="policy"></param>
    /// <param name="regionName"></param>
    public override void Set(string key, object? value, CacheItemPolicy policy, string? regionName = null)
    {
        Add(key, value, policy, regionName);
    }

    /// <summary>
    ///     Sets the value of the cache item with the specified key.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="policy"></param>
    public override void Set(CacheItem item, CacheItemPolicy policy)
    {
        Add(item, policy);
    }

    /// <summary>
    ///     Sets the value of the cache item with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpiration"></param>
    /// <param name="regionName"></param>
    public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string? regionName = null)
    {
        Add(key, value, absoluteExpiration, regionName);
    }

    /// <summary>
    ///     Gets or sets the cache item with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public override object? this[string key]
    {
        get => Get(key, DefaultRegion);
        set => Set(key, value, DefaultPolicy, DefaultRegion);
    }
    #endregion
}