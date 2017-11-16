using System;
using ChromeHtmlToPdfLib.Settings;
using CommandLine;
using CommandLine.Text;

namespace ChromeHtmlToPdf
{
    /// <summary>
    /// The parameters that can be used when calling this application
    /// </summary>
    public class Options
    {
        #region Properties
        /// <summary>
        ///     The input url or file
        /// </summary>
        [Option("input", Required = true, HelpText = "The input url or file")]
        public string Input { get; set; }

        /// <summary>
        ///     The output file
        /// </summary>
        [Option("output", Required = true, HelpText = "The output file")]
        public string Output { get; set; }

        /// <summary>
        ///     Paper orientation. Defaults to false.
        /// </summary>
        [Option("landscape", DefaultValue = false, Required = false, HelpText = "Paper orientation")]
        public bool Landscape { get; set; }

        /// <summary>
        ///     Display header and footer. Defaults to false.
        /// </summary>
        [Option("displayheaderfooter", DefaultValue = false, Required = false, HelpText = "Display header and footer")]
        public bool DisplayHeaderFooter { get; set; }

        /// <summary>
        ///     Print background graphics. Defaults to false.
        /// </summary>
        [Option("printbackground", DefaultValue = false, Required = false, HelpText = "Print background graphics")]
        public bool PrintBackground { get; set; }

        /// <summary>
        ///     Scale of the webpage rendering. Defaults to 1.
        /// </summary>
        [Option("scale", DefaultValue = 1.0, Required = false, HelpText = "Scale of the webpage rendering")]
        public double Scale { get; set; }

        /// <summary>
        ///     The papersize to use, when this option is set is will override <see cref="PaperWidth"/> and <see cref="PaperHeight"/>
        /// </summary>
        [Option("paperformat", DefaultValue = PaperFormats.None, Required = false, HelpText = "Paper format to use, when set then this will override paperwidth and paperheight")]
        public PaperFormats PaperFormat { get; set; }
        
        /// <summary>
        ///     Paper width in inches. Defaults to 8.5 inches.
        /// </summary>
        [Option("paperwidth", DefaultValue = 8.5, Required = false, HelpText = "Paper width in inches")]
        public double PaperWidth { get; set; }

        /// <summary>
        ///     Paper height in inches. Defaults to 11 inches.
        /// </summary>
        [Option("paperheight", DefaultValue = 11.0, Required = false, HelpText = "Paper height in inches")]
        public double PaperHeight { get; set; }

        /// <summary>
        ///     Top margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        [Option("margintop", DefaultValue = 0.4, Required = false, HelpText = "Top margin in inches")]
        public double MarginTop { get; set; }

        /// <summary>
        ///     Bottom margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        [Option("marginbottom", DefaultValue = 0.4, Required = false, HelpText = "Top margin in inches")]
        public double MarginBottom { get; set; }

        /// <summary>
        ///     Left margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        [Option("marginleft", DefaultValue = 0.4, Required = false, HelpText = "Left margin in inches")]
        public double MarginLeft { get; set; }

        /// <summary>
        ///     Right margin in inches. Defaults to 1cm (~0.4 inches).
        /// </summary>
        [Option("marginright", DefaultValue = 0.4, Required = false, HelpText = "Right margin in inches")]
        public double MarginRight { get; set; }

        /// <summary>
        ///     Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which means print all pages.
        /// </summary>
        [Option("pageranges", Required = false, HelpText = "Paper ranges to print, e.g., '1-5, 8, 11-13'")]
        public string PageRanges { get; set; }

        /// <summary>
        ///     Whether to silently ignore invalid but successfully parsed page ranges, such as '3-2'. Defaults to false.
        /// </summary>
        [Option("ignoreinvalidpageranges", DefaultValue = false, Required = false, HelpText = "Whether to silently ignore invalid but successfully parsed page ranges, such as '3-2'")]
        public bool IgnoreInvalidPageRanges { get; set; }

        /// <summary>
        ///     The location for Chrome, when not set then the registry is accessed to get the needed information
        /// </summary>
        [Option("chromelocation", Required = false, HelpText = "The location for Chrome, when not set then the registry is accessed to get the needed information")]
        public string ChromeLocation { get; set; }

        /// <summary>
        ///     The Chrome user profile location to use, when not set then the default location is used
        /// </summary>
        [Option("chromeuserprofile", Required = false, HelpText = "The Chrome user profile location to use, when not set then the default location is used")]
        public string ChromeUserProfile { get; set; }

        /// <summary>
        ///     The default port to use when communicating with Chrome
        /// </summary>
        [Option("portrange", DefaultValue = "9222-9322", Required = false, HelpText = "The port(range) to use when communicating with Chrome. For example 9222-9322 when setting a port range")]
        public string PortRange { get; set; }
        #endregion
    }
}
