namespace ChromiumHtmlToPdfLib.Loggers;

/// <summary>
///     Writes log information to a file
/// </summary>
public class File : Stream
{
    /// <summary>
    ///     Logs information to the given <paramref name="fileName" />
    /// </summary>
    /// <param name="fileName"></param>
    public File(string fileName) : base(System.IO.File.OpenWrite(fileName))
    {
    }
}