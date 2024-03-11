using System.Collections.Generic;
using System.IO;

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     A basic file cache manager that does not use hashing
/// </summary>
internal class BasicFileCacheManager(string cacheDir, string cacheSubFolder, string policySubFolder) : FileCacheManager(cacheDir, cacheSubFolder, policySubFolder)
{
    #region GetKeys
    /// <summary>
    ///     Returns a list of keys for a given region.
    /// </summary>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override IEnumerable<string> GetKeys(string? regionName = null)
    {
        var region = "";
        if (string.IsNullOrEmpty(regionName) == false) region = regionName;
        var directory = Path.Combine(CacheDir, CacheSubFolder, region);
        if (!Directory.Exists(directory)) yield break;
        foreach (var file in Directory.EnumerateFiles(directory))
            yield return Path.GetFileNameWithoutExtension(file);
    }
    #endregion

    #region GetCachePath
    /// <summary>
    ///     Builds a string that will place the specified file name within the appropriate
    ///     cache and workspace folder.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override string GetCachePath(string fileName, string? regionName = null)
    {
        regionName ??= string.Empty;
        var directory = Path.Combine(CacheDir, CacheSubFolder, regionName);
        var filePath = Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName) + ".dat");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        return filePath;
    }
    #endregion

    #region GetPolicyPath
    /// <summary>
    ///     Builds a string that will get the path to the supplied file's policy file
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override string GetPolicyPath(string key, string? regionName = null)
    {
        regionName ??= string.Empty;
        var directory = Path.Combine(CacheDir, PolicySubFolder, regionName);
        var filePath = Path.Combine(directory, Path.GetFileNameWithoutExtension(key) + ".policy");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        return filePath;
    }
    #endregion
}