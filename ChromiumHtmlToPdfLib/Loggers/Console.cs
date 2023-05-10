namespace ChromiumHtmlToPdfLib.Loggers
{
    /// <summary>
    ///     Writes log information to the console
    /// </summary>
    public class Console: Stream
    {
        /// <summary>
        ///     Writes logging to the console
        /// </summary>
        public Console() : base(System.Console.OpenStandardOutput())
        {
        }
    }
}
