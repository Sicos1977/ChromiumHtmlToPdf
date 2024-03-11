namespace ChromiumHtmlToPdfLib.FileCache;

internal class FileCacheManagerFactory
{
    internal static FileCacheManager Create(FileCacheManagers type, string cacheDir, string cacheSubFolder, string policySubFolder)
    {
        FileCacheManager instance;

        switch (type)
        {
            case FileCacheManagers.Hashed:
                instance = new HashedFileCacheManager(cacheDir, cacheSubFolder, policySubFolder);
                break;
            case FileCacheManagers.Basic:
            default:
            instance = new BasicFileCacheManager(cacheDir, cacheSubFolder, policySubFolder);
                break;
        }

        return instance;
    }
}