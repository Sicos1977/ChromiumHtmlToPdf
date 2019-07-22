namespace ChromeHtmlToPdfLib
{
    public class WrapExtension
    {
        #region Properties
        /// <summary>
        /// Returns the extension
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Returns <c>true</c> when it neededs to be wrapped (default <c>true</c>
        /// </summary>
        public bool Wrap { get; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates this object and sets its needed properties
        /// </summary>
        /// <param name="ext">The extension</param>
        /// <param name="wrap"><c>true</c> (default) when it needs to be wrapped, otherwise <c>false</c></param>
        public WrapExtension(string ext, bool wrap = true)
        {
            Extension = ext;
            Wrap = wrap;
        }
        #endregion
    }
}
