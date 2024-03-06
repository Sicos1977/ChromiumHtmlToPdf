using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     File-based caching using xxHash. Collisions are handled by appending
///     numerically ascending identifiers to each hash key (e.g. _1, _2, etc.).
/// </summary>
internal class HashedFileCacheManager : FileCacheManager
{
    #region Fields
    private static readonly MD5 md5Hash = MD5.Create();
    #endregion

    #region ComputeHash
    /// <summary>
    ///     Returns a 64bit hash in hex of supplied key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string ComputeHash(string key)
    {
        var hash = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(key));

        return BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0,16); // Grab 64-bit value
    }
    #endregion

    #region GetFileName
    /// <summary>
    ///     Because hash collisions prevent us from knowing the exact file name of the supplied key, we need to probe through
    ///     all possible fine name combinations.  This function is used internally by the Delete and Get functions in this class.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    private string GetFileName(string key, string regionName = null)
    {
        regionName ??= string.Empty;

        //CacheItemPolicies have references to the original key, which is why we look there.  This implies that
        //manually deleting a policy in the file system has dire implications for any keys that probe after
        //the policy.  It also means that deleting a policy file makes the related .dat "invisible" to FC.
        var directory = Path.Combine(CacheDir, PolicySubFolder, regionName);

        
        var hash = ComputeHash(key);
        var hashCounter = 0;
        var fileName = Path.Combine(directory, $"{hash}_{hashCounter}.policy");
        var found = false;
        while (found == false)
        {
            fileName = Path.Combine(directory, $"{hash}_{hashCounter}.policy");
            if (File.Exists(fileName))
                //check for correct key
                try
                {
                    var policy = DeserializePolicyData(fileName);
                    if (string.Compare(key, policy.Key, StringComparison.Ordinal) == 0)
                        //correct key found!
                        found = true;
                    else
                        //wrong key, try again
                        hashCounter++;
                }
                catch
                {
                    //Corrupt file?  Assume usable for current key.
                    found = true;
                }
            else
                //key not found, must not exist.  Return last generated file name.
                found = true;
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }
    #endregion

    #region GetCachePath
    /// <summary>
    ///     Builds a string that will place the specified file name within the appropriate
    ///     cache and workspace folder.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    public override string GetCachePath(string key, string regionName = null)
    {
        regionName ??= string.Empty;
        var directory = Path.Combine(CacheDir, CacheSubFolder, regionName);
        var filePath = Path.Combine(directory, GetFileName(key, regionName) + ".dat");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        return filePath;
    }
    #endregion

    #region GetKeys
    /// <summary>
    ///     Returns a list of keys for a given region.
    /// </summary>
    /// <param name="regionName"></param>
    public override IEnumerable<string> GetKeys(string regionName = null)
    {
        var region = string.Empty;
        if (string.IsNullOrEmpty(regionName) == false) region = regionName;
        var directory = Path.Combine(CacheDir, PolicySubFolder, region);
        var keys = new List<string>();
        if (!Directory.Exists(directory)) return keys.ToArray();
        foreach (var file in Directory.GetFiles(directory))
            try
            {
                var policy = DeserializePolicyData(file);
                keys.Add(policy.Key);
            }
            catch
            {
                // Ignore corrupt policy files
            }

        return keys.ToArray();
    }
    #endregion

    #region GetPolicyPath
    /// <summary>
    ///     Builds a string that will get the path to the supplied file's policy file
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public override string GetPolicyPath(string key, string regionName = null)
    {
        regionName ??= string.Empty;
        var directory = Path.Combine(CacheDir, PolicySubFolder, regionName);
        var filePath = Path.Combine(directory, GetFileName(key, regionName) + ".policy");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        return filePath;
    }
    #endregion
}