using System;
using System.IO;
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
            var options = new Options();
            var isValid = Parser.Default.ParseArguments(args, options);

            var parser = new Parser();
            parser.ParseArguments(args, options);
            var helpText = HelpText.AutoBuild(options);
            ;

            //parserResult.WithNotParsed(errors =>
            //{
            //    // Use custom help text to ensure valid enum values are displayed
            //    var helpText = HelpText.AutoBuild(parserResult);
            //    helpText.AddEnumValuesToHelpText = true;
            //    helpText.AddOptions(parserResult);
            //    Console.Error.Write(helpText);
            //    Environment.Exit(1);
            //});

            if (!isValid)
            {
                Console.Write(HelpText.AutoBuild(options).ToString());
                return;
            }

            var portRangeSettings = GetPortRangeSettings(options);
            if (portRangeSettings == null)
                return;

            var chrome = !string.IsNullOrWhiteSpace(options.ChromeLocation)
                ? options.ChromeLocation
                : Path.Combine(GetChromeLocation(), "chrome.exe");

            try
            {
                var pageSettings = GetPageSettings(options);

                using (var converter = new Converter(chrome, portRangeSettings))
                {
                    converter.ConvertToPdf(new Uri(options.Input), options.Output, pageSettings);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        #endregion

        #region GetPortRangeSettings
        /// <summary>
        /// Parses the port(range) settings from the commandline
        /// </summary>
        /// <param name="options"><see cref="Options"/></param>
        /// <returns></returns>
        private static PortRangeSettings GetPortRangeSettings(Options options)
        {
            int start;
            var end = 0;

            var portRangeParts = options.PortRange.Split('-');

            var help = HelpText.AutoBuild(options).ToString();

            if (portRangeParts.Length > 2)
            {
                Console.Write(help);
                return null;
            }

            switch (portRangeParts.Length)
            {
                case 2:
                    if (!int.TryParse(portRangeParts[0], out start))
                    {
                        Console.Write(help);
                        Console.WriteLine($"The start port {portRangeParts[0]} is not valid");
                        return null;
                    }

                    if (!int.TryParse(portRangeParts[1], out end))
                    {
                        Console.Write(help);
                        Console.WriteLine($"The end port {portRangeParts[1]} is not valid");
                        return null;
                    }

                    if (start >= end)
                    {
                        Console.Write(help);
                        Console.WriteLine("The end port needs to be bigger then the start port");
                        return null;
                    }

                    break;

                case 1:
                    if (!int.TryParse(portRangeParts[0], out start))
                    {
                        Console.Write(help);
                        Console.WriteLine($"The port {portRangeParts[0]} is not valid");
                        return null;
                    }
                    break;

                default:
                    Console.Write(help);
                    Console.WriteLine("Port(range) is blank");
                    return null;
            }

            return new PortRangeSettings(start, end);
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

            if (options.PaperFormat != PaperFormats.None)
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
