/*
Copyright 2012, 2013, 2017 Adam Carter (http://adam-carter.com)

This file is part of FileCache (http://github.com/acarteas/FileCache).

FileCache is distributed under the Apache License 2.0.
Consult "LICENSE.txt" included in this package for the Apache License 2.0.
*/

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     You should be able to copy and paste this code into your local project to enable caching custom objects.
/// </summary>
internal sealed class FileCacheBinder : SerializationBinder
{
    /// <inheritdoc />
    public override Type? BindToType(string assemblyName, string typeName)
    {
        // In this case we are always using the current assembly
        var currentAssembly = Assembly.GetExecutingAssembly().FullName;

        // Get the type using the typeName and assemblyName
        var typeToDeserialize = Type.GetType($"{typeName}, {currentAssembly}");

        return typeToDeserialize;
    }
}