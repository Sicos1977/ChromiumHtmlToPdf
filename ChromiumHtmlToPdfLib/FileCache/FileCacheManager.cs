using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
// ReSharper disable UnusedMember.Global

namespace ChromiumHtmlToPdfLib.FileCache;

internal abstract class FileCacheManager
{
    #region Consts
    protected const ulong CacheVersion = 3 << (16 + 3) << (8 + 0) << 0;
    #endregion

    #region Properties
    public string CacheDir { get; set; }
    public string CacheSubFolder { get; set; }
    public string PolicySubFolder { get; set; }
    public SerializationBinder Binder { get; set; }

    /// <summary>
    ///     Used to determine how long the FileCache will wait for a file to become
    ///     available.  Default (00:00:00) is indefinite.  Should the timeout be
    ///     reached, an exception will be thrown.
    /// </summary>
    public TimeSpan AccessTimeout { get; set; }
    #endregion

    #region HeaderVersionValid
    /// <summary>
    ///     Differentiate outdated cache formats from newer.
    ///     Older caches use "BinaryFormatter", which is a security risk:
    ///     https://docs.microsoft.com/nl-nl/dotnet/standard/serialization/binaryformatter-security-guide#preferred-alternatives
    ///     The newer caches have a 'magic' header we'll look for.
    /// </summary>
    /// <param name="reader">BinaryReader opened to stream containing the file contents.</param>
    /// <returns>boolean indicating validity</returns>
    protected bool HeaderVersionValid(BinaryReader reader)
    {
        // Don't much care about exceptions here - let them bubble up.
        var version = reader.ReadUInt64();
        // Valid if magic header version matches.
        return version == CacheVersion;
    }
    #endregion

    #region HeaderVersionWrite
    /// <summary>
    ///     Differentiate outdated cache formats from newer.
    ///     Older caches use "BinaryFormatter", which is a security risk:
    ///     https://docs.microsoft.com/nl-nl/dotnet/standard/serialization/binaryformatter-security-guide#preferred-alternatives
    ///     The newer caches have a 'magic' header we'll look for.
    /// </summary>
    /// <param name="writer">BinaryWriter opened to stream that will contain the file contents.</param>
    protected void HeaderVersionWrite(BinaryWriter writer)
    {
        // Don't much care about exceptions here - let them bubble up.
        writer.Write(CacheVersion);
    }
    #endregion

    #region DeserializePayloadData
    protected virtual object DeserializePayloadData(string fileName, SerializationBinder objectBinder = null)
    {
        object data;
        if (!File.Exists(fileName)) return null;
        using var stream = GetStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        var formatter = new BinaryFormatter();

        //AC: From http://spazzarama.com//2009/06/25/binary-deserialize-unable-to-find-assembly/
        //    Needed to deserialize custom objects
        if (objectBinder != null)
            //take supplied binder over default binder
            formatter.Binder = objectBinder;
        else if (Binder != null) formatter.Binder = Binder;
        try
        {
            data = formatter.Deserialize(stream);
        }
        catch (SerializationException)
        {
            data = null;
        }

        return data;
    }
    #endregion

    #region SerializableCacheItemPolicy
    protected SerializableCacheItemPolicy DeserializePolicyData(string policyPath)
    {
        SerializableCacheItemPolicy policy;

        try
        {
            if (File.Exists(policyPath))
            {
                using var stream = GetStream(policyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new BinaryReader(stream);
                policy = SerializableCacheItemPolicy.Deserialize(reader, stream.Length);
            }
            else
                policy = new SerializableCacheItemPolicy();
        }
        catch
        {
            policy = new SerializableCacheItemPolicy();
        }

        return policy;
    }
    #endregion

    #region ReadFile
    /// <summary>
    ///     This function serves to centralize file reads within this class.
    /// </summary>
    /// <param name="mode">the payload reading mode</param>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <param name="objectBinder"></param>
    /// <returns></returns>
    // and restore the visibility to protected
    public virtual FileCachePayload ReadFile(FileCache.PayloadMode mode, string key, string regionName = null,
        SerializationBinder objectBinder = null)
    {
        var cachePath = GetCachePath(key, regionName);
        var policyPath = GetPolicyPath(key, regionName);
        var payload = new FileCachePayload(null);
        switch (mode)
        {
            case FileCache.PayloadMode.Filename:
                payload.Payload = cachePath;
                break;
            case FileCache.PayloadMode.Serializable:
                payload.Payload = Deserialize(cachePath);
                break;
            case FileCache.PayloadMode.RawBytes:
                payload.Payload = LoadRawPayloadData(cachePath);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        payload.Policy = DeserializePolicyData(policyPath);

        return payload;
    }
    #endregion

    #region LoadRawPayloadData
    private byte[] LoadRawPayloadData(string fileName)
    {
        if (!File.Exists(fileName)) return null;
        using var stream = GetStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream);
        // Check if it's valid version first.
        if (!HeaderVersionValid(reader))
            // Failure - return invalid data.
            return null;
        // `using` statements will clean up for us.
        // Valid - read entire file.
        var data = reader.ReadBytes(int.MaxValue);

        return data;
    }
    #endregion

    #region Deserialize
    protected virtual object Deserialize(string fileName, SerializationBinder objectBinder = null)
    {
        object data;
        if (!File.Exists(fileName)) return null;
        using var stream = GetStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        var formatter = new BinaryFormatter();

        //AC: From http://spazzarama.com//2009/06/25/binary-deserialize-unable-to-find-assembly/
        //    Needed to deserialize custom objects
        if (objectBinder != null)
            //take supplied binder over default binder
            formatter.Binder = objectBinder;
        else if (Binder != null) formatter.Binder = Binder;
        try
        {
            data = formatter.Deserialize(stream);
        }
        catch (SerializationException)
        {
            data = null;
        }
        finally
        {
            stream.Close();
        }

        return data;
    }
    #endregion

    #region WriteFile
    /// <summary>
    ///     This function serves to centralize file writes within this class
    /// </summary>
    public virtual long WriteFile(FileCache.PayloadMode mode, string key, FileCachePayload data, string regionName = null, bool policyUpdateOnly = false)
    {
        var cachedPolicy = GetPolicyPath(key, regionName);
        var cachedItemPath = GetCachePath(key, regionName);
        long cacheSizeDelta = 0;

        //ensure that the cache policy contains the correct key
        data.Policy.Key = key;

        if (!policyUpdateOnly)
        {
            long oldBlobSize = 0;
            if (File.Exists(cachedItemPath)) oldBlobSize = new FileInfo(cachedItemPath).Length;

            switch (mode)
            {
                case FileCache.PayloadMode.Serializable:
                    using (var stream = GetStream(cachedItemPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(stream, data.Payload);
                    }

                    break;
                case FileCache.PayloadMode.RawBytes:
                    using (var stream = GetStream(cachedItemPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using var writer = new BinaryWriter(stream);
                        switch (data.Payload)
                        {
                            case byte[] bytes:
                                writer.Write(bytes);
                                break;
                            case Stream payload:
                            {
                                var dataPayload = payload;
                                var bytePayload = new byte[dataPayload.Length - dataPayload.Position];
                                _ = dataPayload.Read(bytePayload, (int)dataPayload.Position, bytePayload.Length);
                                // no close or the like for data.Payload - we are not the owner
                                break;
                            }
                        }
                    }

                    break;

                case FileCache.PayloadMode.Filename:
                    File.Copy((string)data.Payload, cachedItemPath, true);
                    break;
            }

            //adjust cache size (while we have the file to ourselves)
            cacheSizeDelta += new FileInfo(cachedItemPath).Length - oldBlobSize;
        }

        //remove current policy file from cache size calculations
        if (File.Exists(cachedPolicy)) cacheSizeDelta -= new FileInfo(cachedPolicy).Length;

        //write the cache policy
        using (var stream = GetStream(cachedPolicy, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var writer = new BinaryWriter(stream);
            data.Policy.Serialize(writer);
        }

        // Adjust cache size outside of the using blocks to ensure it's after the data is written.
        cacheSizeDelta += new FileInfo(cachedPolicy).Length;

        return cacheSizeDelta;
    }
    #endregion

    #region Abstract methods
    /// <summary>
    ///     Builds a string that will place the specified file name within the appropriate
    ///     cache and workspace folder.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public abstract string GetCachePath(string key, string regionName = null);

    /// <summary>
    ///     Returns a list of keys for a given region.
    /// </summary>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public abstract IEnumerable<string> GetKeys(string regionName = null);

    /// <summary>
    ///     Builds a string that will get the path to the supplied file's policy file
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public abstract string GetPolicyPath(string key, string regionName = null);
    #endregion

    #region GetRegions
    /// <summary>
    ///     Returns a list of regions, including the root region.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetRegions()
    {
        var directory = Path.Combine(CacheDir, CacheSubFolder);
        var di = new DirectoryInfo(directory);
        if (di.Exists)
        {
            yield return null;
            foreach (var d in di.EnumerateDirectories()) yield return d.Name;
        }
    }
    #endregion

    #region ReadSysValue
    /// <summary>
    ///     Generic version of ReadSysValue just throws an ArgumentException to error on unknown new types.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="value">The value read</param>
    /// <returns>success/failure boolean</returns>
    public bool ReadSysValue<T>(string filename, out T value) where T : struct
    {
        throw new ArgumentException($"Type is currently unsupported: {typeof(T)}", nameof(value));

        // These types could be easily implemented following the `long` function as a template:
        //   - bool:
        //     + reader.ReadBoolean();
        //   - byte:
        //     + reader.ReadByte();
        //   - char:
        //     + reader.ReadChar();
        //   - decimal:
        //     + reader.ReadDecimal();
        //   - double:
        //     + reader.ReadDouble();
        //   - short:
        //     + reader.ReadInt16();
        //   - int:
        //     + reader.ReadInt32();
        //   - long:
        //     + reader.ReadInt64();
        //   - sbyte:
        //     + reader.ReadSbyte();
        //   - ushort:
        //     + reader.ReadUInt16();
        //   - uint:
        //     + reader.ReadUInt32();
        //   - ulong:
        //     + reader.ReadUInt64();
    }

    /// <summary>
    ///     Read a `long` (64 bit signed int) from a sysfile.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="value">The value read or long.MinValue</param>
    /// <returns>success/failure boolean</returns>
    public bool ReadSysValue(string filename, out long value)
    {
        // Return min value on fail. Success/fail will be either exception or bool return.
        value = long.MinValue;
        var success = false;

        // sys files go in the root directory
        var path = Path.Combine(CacheDir, filename);

        if (!File.Exists(path)) return false;
        for (var i = 5; i > 0; i--) // try 5 times to read the file, if we can't, give up
            try
            {
                using var stream = GetStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (var reader = new BinaryReader(stream))
                {
                    try
                    {
                        // The old "BinaryFormatter" sysfiles will fail this check.
                        if (HeaderVersionValid(reader))
                        {
                            value = reader.ReadInt64();
                        }
                        else
                        {
                            // Invalid version - return invalid value & failure.
                            value = long.MinValue;
                            success = false;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        value = long.MinValue;
                        // DriveCommerce: Need to rethrow to get the IOException caught.
                        throw;
                    }
                }

                success = true;
                break;
            }
            catch (IOException)
            {
                // we timed out... so try again
            }

        // `value` already set correctly.
        return success;
    }

    /// <summary>
    ///     Read a `DateTime` struct from a sysfile using `DateTime.FromBinary()`.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="value">The value read or DateTime.MinValue</param>
    /// <returns>success/failure boolean</returns>
    public bool ReadSysValue(string filename, out DateTime value)
    {
        // DateTime is serialized as a long, so use that `ReadSysValue()` function.
        if (ReadSysValue(filename, out long serialized))
        {
            value = DateTime.FromBinary(serialized);
            return true;
        }

        // else failed:
        value = DateTime.MinValue;
        return false;
    }
    #endregion

    #region WriteSysValue
    /// <summary>
    ///     Generic version of `WriteSysValue` just throws an ArgumentException on unknown new types.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="data">The data to write to the sysfile</param>
    public void WriteSysValue<T>(string filename, T data) where T : struct
    {
        throw new ArgumentException($"Type is currently unsupported: {typeof(T)}", nameof(data));
    }

    /// <summary>
    ///     Writes a long to a system file that is not part of the cache itself,
    ///     but is used to help it function.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="data">The long to write to the file</param>
    public void WriteSysValue(string filename, long data)
    {
        // sys files go in the root directory
        var path = Path.Combine(CacheDir, filename);

        // write the data to the file
        using var stream = GetStream(path, FileMode.Create, FileAccess.Write, FileShare.Write);
        using var writer = new BinaryWriter(stream);
        // Must write the magic version header first.
        HeaderVersionWrite(writer);

        writer.Write(data);
    }

    /// <summary>
    ///     Writes a long to a system file that is not part of the cache itself,
    ///     but is used to help it function.
    /// </summary>
    /// <param name="filename">The name of the sysfile (without directory)</param>
    /// <param name="data">The DateTime to write to the file</param>
    public void WriteSysValue(string filename, DateTime data)
    {
        // Convert to long and use long's function.
        WriteSysValue(filename, data.ToBinary());
    }
    #endregion

    #region GetStream
    /// <summary>
    ///     This function servies to centralize file stream access within this class.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mode"></param>
    /// <param name="access"></param>
    /// <param name="share"></param>
    /// <returns></returns>
    protected FileStream GetStream(string path, FileMode mode, FileAccess access, FileShare share)
    {
        FileStream stream = null;
        var interval = new TimeSpan(0, 0, 0, 0, 50);
        var totalTime = new TimeSpan();
        while (stream == null)
            try
            {
                stream = File.Open(path, mode, access, share);
            }
            catch
            {
                Thread.Sleep(interval);
                totalTime += interval;

                //if we've waited too long, throw the original exception.
                if (AccessTimeout.Ticks != 0)
                    if (totalTime > AccessTimeout)
                        throw;
            }

        return stream;
    }    
    #endregion

    #region DeleteFile
    /// <summary>
    ///     Deletes the specified key/region combo.
    ///     Returns bytes freed from delete.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="regionName"></param>
    /// <returns></returns>
    public virtual long DeleteFile(string key, string regionName = null)
    {
        long bytesFreed = 0;

        // Because of the possibility of multiple threads accessing this, it's possible that
        // while we're trying to remove something, another thread has already removed it.
        ReadFile(FileCache.PayloadMode.Filename, key, regionName);
        var path = GetCachePath(key, regionName);
        bytesFreed -= new FileInfo(path).Length;
        File.Delete(path);

        //remove policy file
        var cachedPolicy = GetPolicyPath(key, regionName);
        bytesFreed -= new FileInfo(cachedPolicy).Length;
        File.Delete(cachedPolicy);

        return Math.Abs(bytesFreed);
    }
    #endregion

    #region Class LocalCacheBinder
    protected class LocalCacheBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var currentAssembly = Assembly.GetAssembly(typeof(LocalCacheBinder))!.FullName;
            assemblyName = currentAssembly;

            // Get the type using the typeName and assemblyName
            var typeToDeserialize = Type.GetType($"{typeName}, {assemblyName}");

            return typeToDeserialize;
        }
    }
    #endregion
}