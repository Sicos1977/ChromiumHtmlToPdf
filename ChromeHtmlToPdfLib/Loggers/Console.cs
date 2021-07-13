using System;

namespace ChromeHtmlToPdfLib.Loggers
{
    /// <summary>
    ///     Writes log information to the console
    /// </summary>
    public class Console: Stream
    {
        public Console() : base(System.Console.OpenStandardOutput())
        {
        }
    }
}
