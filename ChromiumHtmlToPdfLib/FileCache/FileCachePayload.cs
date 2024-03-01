/*
Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     The payload that is stored in the cache
/// </summary>
/// <param name="payload"></param>
/// <param name="policy"></param>
[Serializable]
public class FileCachePayload(object payload, SerializableCacheItemPolicy policy)
{
    /// <summary>
    ///     Returns or sets the payload
    /// </summary>
    public object Payload { get; set; } = payload;

    /// <summary>
    ///     Returns or sets the policy
    /// </summary>
    public SerializableCacheItemPolicy Policy { get; set; } = policy;

    /// <summary>
    ///     Creates a new payload with a default policy
    /// </summary>
    /// <param name="payload"></param>
    public FileCachePayload(object payload) : this(payload, new SerializableCacheItemPolicy
    {
        AbsoluteExpiration = DateTime.Now.AddYears(10)
    })
    {
    }
}