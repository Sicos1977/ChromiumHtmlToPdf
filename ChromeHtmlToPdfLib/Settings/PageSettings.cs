using System;

namespace ChromeHtmlToPdfLib.Settings
{
    #region PaperFormats
    /// <summary>
    /// The paper formats to use when converting to PDF
    /// </summary>
    public enum PaperFormats
    {
        /// <summary>
        /// Letter format
        /// </summary>
        Letter,

        /// <summary>
        /// Legal format
        /// </summary>
        Legal,

        /// <summary>
        /// Tabloid format
        /// </summary>
        Tabloid,

        /// <summary>
        /// Ledger format
        /// </summary>
        Ledger,

        /// <summary>
        /// A0 format
        /// </summary>
        A0,

        /// <summary>
        /// A1 format
        /// </summary>
        A1,

        /// <summary>
        /// A2 format
        /// </summary>
        A2,

        /// <summary>
        /// A3 format
        /// </summary>
        A3,

        /// <summary>
        /// A4 format
        /// </summary>
        A4,

        /// <summary>
        /// A5 format
        /// </summary>
        A5,

        /// <summary>
        /// A6 format
        /// </summary>
        A6
    }
    #endregion

    /// <summary>
    /// The page settings to use when converting to PDF
    /// </summary>
    public class PageSettings
    {
        #region Properties
        /// <summary>
        ///     Paper orientation. Defaults to false.
        /// </summary>
        public bool Landscape { get; set; }

        /// <summary>
        ///     Display header and footer. Defaults to false.
        /// </summary>
        public bool DisplayHeaderFooter { get; set; }

        /// <summary>
        ///     Print background graphics. Defaults to false.
        /// </summary>
        public bool PrintBackground { get; set; }

        /// <summary>
        ///     Scale of the webpage rendering. Defaults to 1.
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        ///     Paper width in inches. Defaults to 8.5 inches.
        /// </summary>
        public double PaperWidth { get; set; }

        /// <summary>
        ///     Paper height in inches. Defaults to 11 inches.
        /// </summary>
        public double PaperHeight { get; set; }

        /// <summary>
        ///     Top margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginTop { get; set; }

        /// <summary>
        ///     Bottom margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginBottom { get; set; }

        /// <summary>
        ///     Left margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginLeft { get; set; }

        /// <summary>
        ///     Right margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        public double MarginRight { get; set; }

        /// <summary>
        ///     Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which means print all pages.
        /// </summary>
        public string PageRanges { get; set; }

        /// <summary>
        ///     Whether to silently ignore invalid but successfully parsed page ranges, such as '3-2'. Defaults to false.
        /// </summary>
        public bool IgnoreInvalidPageRanges { get; set; }
        #endregion

        #region PrintToPdfSettings
        /// <summary>
        /// Makes this object and sets all the settings to it's default values
        /// </summary>
        /// <remarks>
        /// Default paper settings are set to <see cref="PaperFormats.A4"/>
        /// </remarks>
        public PageSettings()
        {
            ResetToDefaultSettings();
        }

        /// <summary>
        /// Makes this object and sets all the settings to it's default values
        /// </summary>
        /// <remarks>
        /// Default paper settings are set to <see cref="PaperFormats.A4"/>
        /// </remarks>
        /// <param name="paperFormat"></param>
        public PageSettings(PaperFormats paperFormat)
        {
            ResetToDefaultSettings();
            SetPaperFormat(paperFormat);
        }
        #endregion

        #region ResetToDefaultSettings
        /// <summary>
        /// Resets the <see cref="PageSettings"/> to it's default settings
        /// </summary>
        public void ResetToDefaultSettings()
        {
            Landscape = false;
            DisplayHeaderFooter = false;
            PrintBackground = false;
            Scale = 1.0;
            SetPaperFormat(PaperFormats.A4);
            MarginTop = 0.4;
            MarginBottom = 0.4;
            MarginLeft = 0.4;
            MarginRight = 0.4;
            PageRanges = string.Empty;
        }
        #endregion

        #region SetPaperFormat
        /// <summary>
        /// Set the given <paramref name="paperFormat"/>
        /// </summary>
        /// <param name="paperFormat"><see cref="PaperFormats"/></param>
        private void SetPaperFormat(PaperFormats paperFormat)
        {
            switch (paperFormat)
            {
                case PaperFormats.Letter:
                    PaperWidth = 8.5;
                    PaperHeight = 11;
                    break;
                case PaperFormats.Legal:
                    PaperWidth = 8.5;
                    PaperHeight = 14;
                    break;
                case PaperFormats.Tabloid:
                    PaperWidth = 11;
                    PaperHeight = 17;
                    break;
                case PaperFormats.Ledger:
                    PaperWidth = 17;
                    PaperHeight = 11;
                    break;
                case PaperFormats.A0:
                    PaperWidth = 33.1;
                    PaperHeight = 46.8;
                    break;
                case PaperFormats.A1:
                    PaperWidth = 23.4;
                    PaperHeight = 33.1;
                    break;
                case PaperFormats.A2:
                    PaperWidth = 16.5;
                    PaperHeight = 23.4;
                    break;
                case PaperFormats.A3:
                    PaperWidth = 11.7;
                    PaperHeight = 16.5;
                    break;
                case PaperFormats.A4:
                    PaperWidth = 8.27;
                    PaperHeight = 11.7;
                    break;
                case PaperFormats.A5:
                    PaperWidth = 5.83;
                    PaperHeight = 8.27;
                    break;
                case PaperFormats.A6:
                    PaperWidth = 4.13;
                    PaperHeight = 5.83;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paperFormat), paperFormat, null);
            }
        }
        #endregion
    }
}