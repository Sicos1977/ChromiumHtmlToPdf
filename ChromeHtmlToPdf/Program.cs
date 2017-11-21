using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Settings;
using CommandLine;
using CommandLine.Text;
using Microsoft.Win32;

namespace ChromeHtmlToPdf
{
    class Program
    {
        #region Fields
        /// <summary>
        ///     <see cref="LimitedConcurrencyLevel" />
        /// </summary>
        private static TaskFactory _taskFactory;

        /// <summary>
        /// A list with <see cref="ConversionItem"/>'s to process
        /// </summary>
        private static ConcurrentQueue<ConversionItem> _itemsToConvert;

        /// <summary>
        /// A list with converted <see cref="ConversionItem"/>'s
        /// </summary>
        private static ConcurrentQueue<ConversionItem> _itemsConverted;

        /// <summary>
        ///     Used to keep track of all the worker tasks we are starting
        /// </summary>
        private static List<Task> _workerTasks;
        #endregion

        #region Main
        static void Main(string[] args)
        {
            ParseCommandlineParameters(args, out var options, out var portRangeSettings);

            var maxTasks = SetMaxConcurrencyLevel(options);

            if (options.InputIsList)
            {
                if (options.UseMultiThreading)
                {
                    _workerTasks = new List<Task>();
                    _itemsToConvert = new ConcurrentQueue<ConversionItem>();
                    _itemsConverted = new ConcurrentQueue<ConversionItem>();

                    WriteToLog($"Reading inputfile '{options.Input}'");
                    var lines = File.ReadAllLines(options.Input);
                    foreach (var line in lines)
                    {
                        var inputUri = new Uri(line);
                        var outputPath = Path.GetFullPath(options.Output);

                        var outputFile = inputUri.Scheme == "file"
                            ? Path.GetFileName(inputUri.AbsolutePath)
                            : FileManager.RemoveInvalidFileNameChars(inputUri.ToString());

                        _itemsToConvert.Enqueue(new ConversionItem(inputUri, Path.Combine(outputPath, outputFile)));
                    }

                    WriteToLog($"{_itemsToConvert.Count} items read");

                    WriteToLog($"Starting {maxTasks} processing tasks");
                    for (var i = 0; i < maxTasks; i++)
                    {
                        var i1 = i;
                        _workerTasks.Add(_taskFactory.StartNew(() => ConvertWithTask(options, portRangeSettings, (i1 + 1).ToString())));
                    }
                    WriteToLog("Started");

                    // Waiting until all tasks are finished
                    foreach (var task in _workerTasks)
                    {
                        task.Wait();
                    }

                    // Write conversion information to output file
                    using (var output = File.OpenWrite(options.Output))
                    {
                        foreach (var itemConverted in _itemsConverted)
                        {
                            var bytes = new UTF8Encoding(true).GetBytes(itemConverted.OutputLine);
                            output.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
                else
                {
                    foreach (var uri in _itemsToConvert)
                    {
                        // TODO: Write code
                        //Convert();
                    }
                }
            }
            else
            {
                Convert(options, portRangeSettings);                
            }

            
            try
            {

            }
            catch (Exception exception)
            {
                WriteToLog(exception.Message);
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }
        #endregion

        #region ParseCommandlineParameters
        /// <summary>
        /// Parses the commandline parameters and returns these as an <paramref name="options"/> and
        /// <paramref name="portRangeSettings"/> object
        /// </summary>
        /// <param name="args"></param>
        /// <param name="options"><see cref="Options"/></param>
        /// <param name="portRangeSettings"><see cref="PortRangeSettings"/></param>
        private static void ParseCommandlineParameters(IEnumerable<string> args, 
            out Options options, 
            out PortRangeSettings portRangeSettings)
        {
            Options tempOptions = null;

            options = null;
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
                tempOptions = parsed;
            });

            options = tempOptions;

            portRangeSettings = null;
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

        #region SetMaxConcurrencyLevel
        /// <summary>
        /// Sets the maximum concurrency level
        /// </summary>
        /// <param name="options"><see cref="Options"/></param>
        /// <returns>Maximum concurrency level</returns>
        private static int SetMaxConcurrencyLevel(Options options)
        {
            var maxConcurrencyLevel = Environment.ProcessorCount;
            if (options.MaxConcurrencyLevel != 0)
            {
                if (options.MaxConcurrencyLevel < 1)
                    throw new ArgumentException(
                        "--max-concurrency-level needs to be a value equal to 0 (system decides how many threads to start) or a value equal or greater than 1");

                maxConcurrencyLevel = options.MaxConcurrencyLevel;
            }

            var lcts = new LimitedConcurrencyLevel(maxConcurrencyLevel);
            _taskFactory = new TaskFactory(lcts);
            return maxConcurrencyLevel;
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

        #region Convert
        /// <summary>
        /// Sets the converter settings
        /// </summary>
        /// <param name="converter"><see cref="Converter"/></param>
        /// <param name="options"><see cref="Options"/></param>
        private static void SetConvertedSettings(Converter converter, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.UserAgent))
                converter.SetUserAgent(options.UserAgent);

            if (options.WindowSize != WindowSize.HD_1366_768)
                converter.SetWindowSize(options.WindowSize);
            else
                converter.SetWindowSize(options.WindowWidth, options.WindowHeight);

            if (!string.IsNullOrWhiteSpace(options.User))
                converter.SetUser(options.User, options.Password);

            if (!string.IsNullOrWhiteSpace(options.ProxyServer))
            {
                converter.SetProxyServer(options.ProxyServer);
                converter.SetProxyBypassList(options.ProxyByPassList);
            }

            if (!string.IsNullOrWhiteSpace(options.ProxyPacUrl))
                converter.SetProxyPacUrl(options.ProxyPacUrl);
        }

        private static void Convert(Options options, PortRangeSettings portRangeSettings)
        {
            var pageSettings = GetPageSettings(options);

            var chrome = !string.IsNullOrWhiteSpace(options.ChromeLocation)
                ? options.ChromeLocation
                : Path.Combine(GetChromeLocation(), "chrome.exe");

            using (var converter = new Converter(chrome, portRangeSettings))
            {
                SetConvertedSettings(converter, options);
                converter.ConvertToPdf(new Uri(options.Input), options.Output, pageSettings, options.WaitForNetworkIdle);
            }
        }

        private static void ConvertWithTask(Options options, 
                                            PortRangeSettings portRangeSettings,
                                            string instanceId)
        {
            var pageSettings = GetPageSettings(options);

            var chrome = !string.IsNullOrWhiteSpace(options.ChromeLocation)
                ? options.ChromeLocation
                : Path.Combine(GetChromeLocation(), "chrome.exe");

            using (var converter = new Converter(chrome, portRangeSettings, null, Console.OpenStandardOutput()))
            {
                converter.InstanceId = instanceId;
                SetConvertedSettings(converter, options);

                while (!_itemsToConvert.IsEmpty)
                {
                    if (!_itemsToConvert.TryDequeue(out var itemToConvert)) continue;
                    try
                    {
                        converter.ConvertToPdf(itemToConvert.InputUri, itemToConvert.OutputFile, pageSettings,
                            options.WaitForNetworkIdle);

                        itemToConvert.SetStatus(ConversionItemStatus.Success);
                    }
                    catch (Exception exception)
                    {
                        itemToConvert.SetStatus(ConversionItemStatus.Failed, exception);
                    }

                    _itemsConverted.Enqueue(itemToConvert);
                }
            }
        }
        #endregion

        #region WriteToLog
        /// <summary>
        ///     Writes a line to the console
        /// </summary>
        /// <param name="message">The message to write</param>
        private static void WriteToLog(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("s") + " - " + message);
        }
        #endregion
    }
}
