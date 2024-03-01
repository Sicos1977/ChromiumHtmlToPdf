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
///     You should be able to copy & paste this code into your local project to enable caching custom objects.
/// </summary>
internal sealed class FileCacheBinder : SerializationBinder
{
    /// <summary>
    ///     Binds the type to a name
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public override Type BindToType(string assemblyName, string typeName)
    {
        var currentAssembly = Assembly.GetExecutingAssembly().FullName;

        // In this case we are always using the current assembly
        assemblyName = currentAssembly;

        // Get the type using the typeName and assemblyName
        var typeToDeserialize = Type.GetType($"{typeName}, {assemblyName}");

        return typeToDeserialize;
    }
}