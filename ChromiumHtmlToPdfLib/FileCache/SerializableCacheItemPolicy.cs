/*
Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;
using System.IO;
using System.Runtime.Caching;

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///    A serializable version of the CacheItemPolicy
/// </summary>
[Serializable]
public class SerializableCacheItemPolicy
{
    #region Fields
    /// <summary>
    ///     Magic version for new policies: 3.3.0 packed into a long
    /// </summary>
    protected const ulong CacheVersion = 3 << (16 + 3) << (8 + 0) << 0;

    private TimeSpan _slidingExpiration;
    #endregion

    #region Properties
    /// <summary>
    ///     Returns or sets the absolute expiration date and time for the cache entry
    /// </summary>
    public DateTimeOffset AbsoluteExpiration { get; set; }

    /// <summary>
    ///     Returns or sets the sliding expiration time for the cache entry
    /// </summary>
    public TimeSpan SlidingExpiration
    {
        get => _slidingExpiration;
        set
        {
            _slidingExpiration = value;
            if (_slidingExpiration > TimeSpan.Zero) AbsoluteExpiration = DateTimeOffset.Now.Add(_slidingExpiration);
        }
    }

    /// <summary>
    ///     The cache key that this particular policy refers to
    /// </summary>
    public string Key { get; set; }
    #endregion

    #region Constructors
    /// <summary>
    ///     Creates a new policy from an existing CacheItemPolicy
    /// </summary>
    /// <param name="policy"></param>
    public SerializableCacheItemPolicy(CacheItemPolicy policy)
    {
        AbsoluteExpiration = policy.AbsoluteExpiration;
        SlidingExpiration = policy.SlidingExpiration;
    }

    /// <summary>
    ///     Creates a new policy
    /// </summary>
    public SerializableCacheItemPolicy()
    {
        SlidingExpiration = new TimeSpan();
    }
    #endregion

    #region Serialize
    /// <summary>
    ///     Serialize this policy to the supplied BinaryWriter.
    ///     Older policies use the "[Serializable]" attribute and BinaryFormatter, which is a security risk:
    ///     https://docs.microsoft.com/nl-nl/dotnet/standard/serialization/binaryformatter-security-guide#preferred-alternatives
    ///     The newer caches have a 'magic' header we'll look for and serialize their fields manually.
    /// </summary>
    internal void Serialize(BinaryWriter writer)
    {
        writer.Write(CacheVersion);
        writer.Write(AbsoluteExpiration.DateTime.ToBinary());
        writer.Write(AbsoluteExpiration.Offset.TotalMilliseconds);
        writer.Write(SlidingExpiration.TotalMilliseconds);
        writer.Write(Key);
    }
    #endregion

    #region Deserialize
    /// <summary>
    ///     Deserialize a policy from the supplied BinaryReader.
    ///     Older policies use the "[Serializable]" attribute and BinaryFormatter, which is a security risk:
    ///     https://docs.microsoft.com/nl-nl/dotnet/standard/serialization/binaryformatter-security-guide#preferred-alternatives
    ///     The newer caches have a 'magic' header we'll look for and deserialize their fields manually.
    ///     If the 'magic' header isn't found, this returns an empty policy.
    /// </summary>
    internal static SerializableCacheItemPolicy Deserialize(BinaryReader reader, long streamLength)
    {
        // Can't even check for the magic version number; return empty policy.
        if (streamLength < sizeof(ulong))
            return new SerializableCacheItemPolicy();

        try
        {
            var version = reader.ReadUInt64();
            if (version != CacheVersion)
                // Just return an empty policy if we read an invalid one.
                // This is likely the older "BinaryFormatter"-serialized policy.
                return new SerializableCacheItemPolicy();

            return new SerializableCacheItemPolicy
            {
                AbsoluteExpiration = new DateTimeOffset(DateTime.FromBinary(reader.ReadInt64()), TimeSpan.FromMilliseconds(reader.ReadDouble())),
                // Don't clobber absolute by using slidings setter; set the private value instead.
                _slidingExpiration = TimeSpan.FromMilliseconds(reader.ReadDouble()),
                Key = reader.ReadString()
            };
        }
        catch (Exception error)
        {
            if (error is EndOfStreamException or IOException or ObjectDisposedException)
                // Just return an empty policy if we failed to read.
                return new SerializableCacheItemPolicy();
            // Didn't expect this error type; rethrow it.
            throw;
        }
    }
    #endregion
}