namespace ChromiumHtmlToPdfLib.FileCache;

internal class FileCacheManagerFactory
{
    internal static FileCacheManager Create(FileCacheManagers type)
    {
        FileCacheManager instance;

        switch (type)
        {
            case FileCacheManagers.Basic:
                instance = new BasicFileCacheManager();
                break;
            case FileCacheManagers.Hashed:
                instance = new HashedFileCacheManager();
                break;
            default:
                instance = new BasicFileCacheManager();
                break;
        }

        return instance;
    }
}