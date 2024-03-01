/*
Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;
// ReSharper disable UnusedMember.Global

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     Used to pass information
/// </summary>
/// <param name="currentSize"></param>
/// <param name="maxSize"></param>
public class FileCacheEventArgs(long currentSize, long maxSize) : EventArgs
{
    /// <summary>
    ///     Returns the current cache size
    /// </summary>
    public long CurrentCacheSize { get; private set; } = currentSize;

    /// <summary>
    ///     Returns the maximum cache size
    /// </summary>
    public long MaxCacheSize { get; private set; } = maxSize;
}