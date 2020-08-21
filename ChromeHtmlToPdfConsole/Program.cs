using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChromeHtmlToPdfLib;
using ChromeHtmlToPdfLib.Enums;
using ChromeHtmlToPdfLib.Settings;
using CommandLine;
using CommandLine.Text;

namespace ChromeHtmlToPdfConsole
{
    class Program
    {
        #region Fields
        /// <summary>
        ///     When set then logging is written to this stream
        /// </summary>
        private static Stream _logStream;

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
            Options options = null;

            try
            {
                ParseCommandlineParameters(args, out options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (options == null)
                throw new ArgumentException(nameof(options));

            // ReSharper disable once PossibleNullReferenceException
            using (_logStream = string.IsNullOrWhiteSpace(options.LogFile)
                ? Console.OpenStandardOutput()
                : File.OpenWrite(ReplaceWildCards(options.LogFile)))
            {
                try
                {
                    var maxTasks = SetMaxConcurrencyLevel(options);

                    if (options.InputIsList)
                    {
                        _itemsToConvert = new ConcurrentQueue<ConversionItem>();
                        _itemsConverted = new ConcurrentQueue<ConversionItem>();

                        WriteToLog($"Reading input file '{options.Input}'");
                        var lines = File.ReadAllLines(options.Input);
                        foreach (var line in lines)
                        {
                            var inputUri = new ConvertUri(line);
                            var outputPath = Path.GetFullPath(options.Output);

                            var outputFile = inputUri.IsFile
                                ? Path.GetFileName(inputUri.AbsolutePath)
                                : FileManager.RemoveInvalidFileNameChars(inputUri.ToString());

                            outputFile = Path.ChangeExtension(outputFile, ".pdf");

                            _itemsToConvert.Enqueue(new ConversionItem(inputUri,
                                // ReSharper disable once AssignNullToNotNullAttribute
                                Path.Combine(outputPath, outputFile)));
                        }

                        WriteToLog($"{_itemsToConvert.Count} items read");

                        if (options.UseMultiThreading)
                        {
                            _workerTasks = new List<Task>();

                            WriteToLog($"Starting {maxTasks} processing tasks");
                            for (var i = 0; i < maxTasks; i++)
                            {
                                var i1 = i;
                                _workerTasks.Add(_taskFactory.StartNew(() =>
                                    ConvertWithTask(options, (i1 + 1).ToString())));
                            }

                            WriteToLog("Started");

                            // Waiting until all tasks are finished
                            foreach (var task in _workerTasks)
                            {
                                task.Wait();
                            }
                        }
                        else
                        {
                            ConvertWithTask(options, null);
                        }

                        // Write conversion information to output file
                        using var output = File.OpenWrite(options.Output);
                        foreach (var itemConverted in _itemsConverted)
                        {
                            var bytes = new UTF8Encoding(true).GetBytes(itemConverted.OutputLine);
                            output.Write(bytes, 0, bytes.Length);
                        }
                    }
                    else
                    {
                        Convert(options);
                    }

                    Environment.Exit(0);
                }
                catch (Exception exception)
                {
                    WriteToLog(exception.StackTrace + ", " + exception.Message);
                    Environment.Exit(1);
                }
            }
        }
        #endregion

        #region ParseCommandlineParameters
        /// <summary>
        /// Parses the commandline parameters and returns these as an <paramref name="options"/>object
        /// </summary>
        /// <param name="args"></param>
        /// <param name="options"><see cref="Options"/></param>
        private static void ParseCommandlineParameters(IEnumerable<string> args, out Options options)
        {
            Options tempOptions = null;

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

            if (errors)
            {
                var helpText = HelpText.AutoBuild(parserResult);

                helpText.AddPreOptionsText("Example usage:");
                helpText.AddPreOptionsText("    ChromeHtmlToPdf --input https://www.google.nl --output c:\\google.pdf");
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

            if (options.PaperFormat != PaperFormat.Letter)
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

        #region SetConverterSettings
        /// <summary>
        /// Sets the converter settings
        /// </summary>
        /// <param name="converter"><see cref="Converter"/></param>
        /// <param name="options"><see cref="Options"/></param>
        private static void SetConverterSettings(Converter converter, Options options)
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

            if (!string.IsNullOrWhiteSpace(options.TempFolder))
                converter.TempDirectory = options.TempFolder;
            else
                Path.GetTempPath();

            if (options.PreWrapFileExtensions?.Count() == 0)
            {
                converter.PreWrapExtensions.Add(".txt");
                converter.PreWrapExtensions.Add(".log");
            }

            converter.ImageResize = options.ImageResize;
            converter.ImageRotate = options.ImageRotate;
            converter.ImageDownloadTimeout = options.ImageDownloadTimeout;
            converter.SanitizeHtml = options.SanitizeHtml;
            converter.RunJavascript = options.RunJavascript;

            if (!string.IsNullOrWhiteSpace(options.UrlBlacklist))
            {
                var urlBlacklist = options.UrlBlacklist.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                converter.SetUrlBlacklist(urlBlacklist);
            }

            converter.CaptureSnapshot = options.Snapshot;
        }
        #endregion

        #region ReplaceWildCards
        private static string ReplaceWildCards(string text)
        {
            text = text.Replace("{PID}", Process.GetCurrentProcess().Id.ToString());
            text = text.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"));
            text = text.Replace("{TIME}", DateTime.Now.ToString("HH:mm:ss"));
            return text;
        }
        #endregion

        #region Convert
        /// <summary>
        /// Convert a single <see cref="ConversionItem"/> to PDF
        /// </summary>
        /// <param name="options"></param>
        private static void Convert(Options options)
        {
            var pageSettings = GetPageSettings(options);

            using var converter = new Converter(options.ChromeLocation, options.ChromeUserProfile, _logStream);
            SetConverterSettings(converter, options);

            converter.ConvertToPdf(CheckInput(options),
                options.Output,
                pageSettings,
                options.WaitForWindowStatus,
                options.WaitForWindowStatusTimeOut,
                options.Timeout,
                options.MediaLoadTimeout);
        }
        #endregion

        #region CheckInput
        /// <summary>
        ///     Checks the input if a file without path is given
        /// </summary>
        /// <returns></returns>
        private static ConvertUri CheckInput(Options options)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(options.Encoding)
                    ? new ConvertUri(options.Input, options.Encoding)
                    : new ConvertUri(options.Input);
            }
            catch (UriFormatException)
            {
                // Check if this is a local file
                var file = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), options.Input));
                if (file.Exists)
                    return new ConvertUri(file.FullName);
            }

            return new ConvertUri(options.Input);
        }
        #endregion

        #region ConvertWithTask
        /// <summary>
        /// This function is started from a <see cref="Task"/> and processes <see cref="ConversionItem"/>'s
        /// that are in the <see cref="_itemsToConvert"/> queue
        /// </summary>
        /// <param name="options"></param>
        /// <param name="instanceId"></param>
        private static void ConvertWithTask(Options options,
                                            string instanceId)
        {
            var pageSettings = GetPageSettings(options);

            var logStream = string.IsNullOrWhiteSpace(options.LogFile)
                ? Console.OpenStandardOutput()
                : File.OpenWrite(ReplaceWildCards(options.LogFile));

            using (logStream)
            {
                using var converter = new Converter(options.ChromeLocation, options.ChromeUserProfile, logStream)
                {
                    InstanceId = instanceId
                };

                SetConverterSettings(converter, options);

                while (!_itemsToConvert.IsEmpty)
                {
                    if (!_itemsToConvert.TryDequeue(out var itemToConvert)) continue;
                    try
                    {
                        converter.ConvertToPdf(itemToConvert.InputUri, itemToConvert.OutputFile, pageSettings,
                            options.WaitForWindowStatus, options.WaitForWindowStatusTimeOut, options.Timeout,
                            options.MediaLoadTimeout);

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
        ///     Writes a line and linefeed to the <see cref="_logStream" />
        /// </summary>
        /// <param name="message">The message to write</param>
        private static void WriteToLog(string message)
        {
            if (_logStream == null) return;
            var line = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + " - " + message + Environment.NewLine;
            var bytes = Encoding.UTF8.GetBytes(line);
            _logStream.Write(bytes, 0, bytes.Length);
            _logStream.Flush();
        }
        #endregion
    }
}