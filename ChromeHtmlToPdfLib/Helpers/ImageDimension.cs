using System;
using ChromeHtmlToPdfLib.Enums;

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    /// Returns the image dimensions in pixel for the given <see cref="PaperFormats"/>
    /// in pixels at 72 DPI
    /// </summary>
    internal class ImageDimension
    {
        #region Properties
        /// <summary>
        /// Returns the max width in pixels
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Returns the max height in pixels
        /// </summary>
        public int Height { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Makes this object and sets it's needed properties
        /// </summary>
        /// <param name="paperFormat"><see cref="PaperFormats"/></param>
        internal ImageDimensions(PaperFormats paperFormat)
        {
            switch (paperFormat)
            {
                case PaperFormats.Letter:
                    Width = 612;
                    Height = 792;
                    break;
                case PaperFormats.Legal:
                    Width = 612;
                    Height = 1008;
                    break;
                case PaperFormats.Tabloid:
                    Width = 792;
                    Height = 1224;
                    break;
                case PaperFormats.Ledger:
                    Width = 1224;
                    Height = 792;
                    break;
                case PaperFormats.A0:
                    Width = 2384;
                    Height = 3370;
                    break;
                case PaperFormats.A1:
                    Width = 1684;
                    Height = 2384;
                    break;
                case PaperFormats.A2:
                    Width = 1191;
                    Height = 1684;
                    break;
                case PaperFormats.A3:
                    Width = 842;
                    Height = 1191;
                    break;
                case PaperFormats.A4:
                    Width = 595;
                    Height = 842;
                    break;
                case PaperFormats.A5:
                    Width = 420;
                    Height = 595;
                    break;
                case PaperFormats.A6:
                    Width = 298;
                    Height = 420;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paperFormat), paperFormat, null);
            }
        }
        #endregion
    }
}