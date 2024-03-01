namespace ChromiumHtmlToPdfLib.FileCache;

/// <summary>
///     Specifies the type of file cache manager
/// </summary>
public enum FileCacheManagers
{
    /// <summary>
    ///     A basic file cache manager that does not use hashing
    /// </summary>
    Basic,

    /// <summary>
    ///     A file cache manager that uses hashing
    /// </summary>
    Hashed
}