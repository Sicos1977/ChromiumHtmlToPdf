namespace ChromeHtmlToPdfLib.Settings
{
    /// <summary>
    ///     The port range to use for Chrome's debugger
    /// </summary>
    public sealed class PortRangeSettings
    {
        #region Constructor
        /// <summary>
        ///     Makes this object and sets all it's needed properties
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public PortRangeSettings(int start, int end)
        {
            Start = start;
            End = end;
        }
        #endregion

        #region Properties
        /// <summary>
        ///     The first port to use
        /// </summary>
        public int Start { get; }

        /// <summary>
        ///     The last port to use
        /// </summary>
        public int End { get; }
        #endregion
    }
}