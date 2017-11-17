using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using CommandLine;
using CommandLine.Text;
using Microsoft.Win32;

namespace ChromeHtmlToPdf
{
    class Program
    {
        #region Main
        static void Main(string[] args)
        {
            Options options = null;
            var errors = false;
            var parser = new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = true;
                settings.HelpWriter = null;
                settings.IgnoreUnknownArguments = false;
                settings.ParsingCulture = CultureInfo.InvariantCulture;
            });

            var parserResult = parser.ParseArguments<Options>(args).WithNotParsed(notParsed =>
            {
                errors = notParsed.Any();
            }
            ).WithParsed(parsed =>
            {
                options = parsed;
            });

            PortRangeSettings portRangeSettings = null;
            string result = null;

            if (errors || !GetPortRangeSettings(options, out result, out portRangeSettings))
            {
                var helpText = HelpText.AutoBuild(parserResult);

                helpText.AddPreOptionsText("Example usage:");
                helpText.AddPreOptionsText("    ChromeHtmlToPdf --input https://www.google.nl --output c:\\google.pdf");

                if (!string.IsNullOrWhiteSpace(result))
                    helpText.AddPreOptionsText(result);

                helpText.AddEnumValuesToHelpText = true;
                helpText.AdditionalNewLineAfterOption = false;
                helpText.AddOptions(parserResult);
                helpText.AddPostOptionsLine("Contact:");
                helpText.AddPostOptionsLine("    If you experience bugs or want to request new features please visit");
                helpText.AddPostOptionsLine("    https://github.com/Sicos1977/ChromeHtmlToPdf/issues");
                helpText.AddPostOptionsLine(string.Empty);

                Console.Error.Write(helpText);
                Environment.Exit(1);
            }

            var chrome = !string.IsNullOrWhiteSpace(options.ChromeLocation)
                ? options.ChromeLocation
                : Path.Combine(GetChromeLocation(), "chrome.exe");

            try
            {
                var pageSettings = GetPageSettings(options);

                using (var converter = new Converter(chrome, portRangeSettings))
                {
                    converter.UseMobileScreen(options.UseMobileScreen);

                    if (options.WindowSize != WindowSize.HD_1366_768)
                        converter.SetWindowSize(options.WindowSize);
                    else
                        converter.SetWindowSize(options.WindowWidth, options.WindowHeight);

                    converter.ConvertToPdf(new Uri(options.Input), options.Output, pageSettings);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }
        #endregion

        #region GetPortRangeSettings
        /// <summary>
        /// Parses the port(range) settings from the commandline
        /// </summary>
        /// <param name="options"><see cref="Options"/></param>
        /// <param name="result"></param>
        /// <param name="portRangeSettings"><see cref="PortRangeSettings"/></param>
        /// <returns><c>true</c> when the portrange options are valid</returns>
        private static bool GetPortRangeSettings(Options options,
                                                 out string result, 
                                                 out PortRangeSettings portRangeSettings)
        {
            int start;
            var end = 0;
            result = string.Empty;
            portRangeSettings = null;

            var portRangeParts = options.PortRange.Split('-');

            if (portRangeParts.Length > 2)
            {
                result = "Portrange should only contain 1 or 2 parts, e.g 9222 or 9222-9322";
                return false;
            }

            switch (portRangeParts.Length)
            {
                case 2:
                    if (!int.TryParse(portRangeParts[0], out start))
                    {
                        result = $"The start port {portRangeParts[0]} is not valid";
                        return false;
                    }

                    if (!int.TryParse(portRangeParts[1], out end))
                    {
                        result = $"The end port {portRangeParts[1]} is not valid";
                        return false;
                    }

                    if (start >= end)
                    {
                        result = "The end port needs to be bigger then the start port";
                        return false;
                    }

                    break;

                case 1:
                    if (!int.TryParse(portRangeParts[0], out start))
                    {
                        result = $"The port {portRangeParts[0]} is not valid";
                        return false;
                    }
                    break;

                default:
                    result = "Port(range) is blank";
                    return false;
            }

            portRangeSettings = new PortRangeSettings(start, end);
            return true;
        }
        #endregion

        #region GetChromeLocation
        /// <summary>
        /// Returns the location of Chrome
        /// </summary>
        /// <returns></returns>
        private static string GetChromeLocation()
        {
            var key = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome",
                "InstallLocation", string.Empty);
            if (key != null)
                return key.ToString();

            key = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\U‌​ninstall\Google Chrome",
                "InstallLocation", string.Empty);
            if (key != null)
                return key.ToString();

            return string.Empty;
        }
        #endregion

        #region GetPageSettings
        /// <summary>
        /// Returns a <see cref="PageSettings"/> object
        /// </summary>
        /// <param name="options"><see cref="Options"/></param>
        /// <returns></returns>
        private static PageSettings GetPageSettings(Options options)
        {
            PageSettings pageSettings;

            if (options.PaperFormat != PaperFormats.Letter)
            {
                pageSettings = new PageSettings(options.PaperFormat);
            }
            else
            {
                pageSettings = new PageSettings
                {
                    PaperWidth = options.PaperWidth,
                    PaperHeight = options.PaperHeight
                };
            }

            pageSettings.Landscape = options.Landscape;
            pageSettings.DisplayHeaderFooter = options.DisplayHeaderFooter;
            pageSettings.PrintBackground = options.PrintBackground;
            pageSettings.Scale = options.Scale;
            pageSettings.MarginTop = options.MarginTop;
            pageSettings.MarginBottom = options.MarginBottom;
            pageSettings.MarginLeft = options.MarginLeft;
            pageSettings.MarginRight = options.MarginRight;
            pageSettings.PageRanges = options.PageRanges;
            pageSettings.IgnoreInvalidPageRanges = options.IgnoreInvalidPageRanges;

            return pageSettings;
        }
        #endregion
    }
}
