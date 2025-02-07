using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChromiumHtmlToPdfLib;
using ChromiumHtmlToPdfLib.Enums;
using ChromiumHtmlToPdfLib.Settings;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Stream = ChromiumHtmlToPdfLib.Loggers.Stream;

namespace ChromiumHtmlToPdfConsole;

static class Program
{
    #region Fields
    /// <summary>
    ///     When set then logging is written to this stream
    /// </summary>
    private static Stream? _logger;

    /// <summary>
    ///     <see cref="LimitedConcurrencyLevel" />
    /// </summary>
    private static TaskFactory? _taskFactory;

    /// <summary>
    ///     Used to keep track of all the worker tasks we are starting
    /// </summary>
    private static List<Task>? _workerTasks;
    #endregion

    #region Main
    public static void Main(string[] args)
    {
        Options? options = null;

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

        _logger = !string.IsNullOrWhiteSpace(options.LogFile)
            ? new Stream(File.OpenWrite(ReplaceWildCards(options.LogFile)))
            : new ChromiumHtmlToPdfLib.Loggers.Console();

        using (_logger)
        {
            try
            {
                var maxTasks = SetMaxConcurrencyLevel(options);

                if (options.InputIsList)
                {
                    var itemsToConvert = new ConcurrentQueue<ConversionItem>();
                    var itemsConverted = new ConcurrentQueue<ConversionItem>();

                    WriteToLog($"Reading input file '{options.Input}'");
                    var lines = File.ReadAllLines(options.Input);
                    foreach (var line in lines)
                    {
                        var inputUri = new ConvertUri(line);
                        var outputPath = Path.GetFullPath(options.Output);

                        string outputFile;

                        if (line.Contains("|"))
                        {
                            var parts = line.Split('|');
                            outputFile = Path.GetFileName(parts[1]);
                        }
                        else
                        {
                            outputFile = inputUri.IsFile
                                ? Path.GetFileName(inputUri.AbsolutePath)
                                : FileManager.RemoveInvalidFileNameChars(inputUri.ToString());
                        }

                        outputFile = Path.ChangeExtension(outputFile, ".pdf");

                        itemsToConvert.Enqueue(new ConversionItem(inputUri,
                            // ReSharper disable once AssignNullToNotNullAttribute
                            Path.Combine(outputPath, outputFile)));
                    }

                    WriteToLog($"{itemsToConvert.Count} items read");

                    if (options.UseMultiThreading)
                    {
                        _workerTasks = new List<Task>();

                        WriteToLog($"Starting {maxTasks} processing tasks");
                        for (var i = 0; i < maxTasks; i++)
                        {
                            var i1 = i;
                            _workerTasks.Add(_taskFactory.StartNew(() =>
                                ConvertWithTask(itemsToConvert, itemsConverted, options, (i1 + 1).ToString())));
                        }

                        WriteToLog("Started");

                        Task.WaitAll(_workerTasks.ToArray());
                    }
                    else
                        ConvertWithTask(itemsToConvert, itemsConverted, options, null).GetAwaiter().GetResult();

                    // Write conversion information to output file
                    using var output = File.OpenWrite(options.Output);
                    foreach (var itemConverted in itemsConverted)
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
    /// Parses the commandline parameters and returns these as a <paramref name="options"/>object
    /// </summary>
    /// <param name="args"></param>
    /// <param name="options"><see cref="Options"/></param>
    private static void ParseCommandlineParameters(IEnumerable<string> args, out Options? options)
    {
        Options? tempOptions = null;

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
            helpText.AddPreOptionsText("    ChromiumHtmlToPdf --input https://www.google.nl --output c:\\google.pdf");
            helpText.AddEnumValuesToHelpText = true;
            helpText.AdditionalNewLineAfterOption = false;
            helpText.AddOptions(parserResult);
            helpText.AddPostOptionsLine("Contact:");
            helpText.AddPostOptionsLine("    If you experience bugs or want to request new features please visit");
            helpText.AddPostOptionsLine("    https://github.com/Sicos1977/ChromiumHtmlToPdf/issues");
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

        return pageSettings;
    }
    #endregion

    #region SetMaxConcurrencyLevel
    /// <summary>
    /// Sets the maximum concurrency level
    /// </summary>
    /// <param name="options"><see cref="Options"/></param>
    /// <returns>Maximum concurrency level</returns>
    [MemberNotNull(nameof(_taskFactory))]
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

            if (!string.IsNullOrWhiteSpace(options.ProxyByPassList))
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
        converter.SanitizeHtml = options.SanitizeHtml;
        converter.RunJavascript = options.RunJavascript;

        if (!string.IsNullOrWhiteSpace(options.UrlBlacklist))
        {
            var urlBlacklist = options.UrlBlacklist.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            converter.SetUrlBlacklist(urlBlacklist);
        }

        converter.CaptureSnapshot = options.Snapshot;
        converter.LogNetworkTraffic = options.LogNetworkTraffic;

        if (!string.IsNullOrWhiteSpace(options.DiskCacheDirectory))
            converter.SetDiskCache(options.DiskCacheDirectory, options.DiskCacheSize);

        converter.DiskCacheDisabled = options.DiskCacheDisabled;
        converter.ImageLoadTimeout = options.ImageLoadTimeout;

        if (options.NoMargins)
            converter.AddChromiumArgument("--no-margins");

        if (options.WebSocketTimeout.HasValue)
            converter.WebSocketTimeout = options.WebSocketTimeout.Value;

        converter.WaitForNetworkIdle = options.WaitForNetworkIdle;

        if (options.NoSandbox)
            converter.AddChromiumArgument("--no-sandbox");

        if (options.EnableChromiumLogging)
            converter.EnableChromiumLogging = true;
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
        var stopWatch = Stopwatch.StartNew();
        var pageSettings = GetPageSettings(options);

        Browser browser;
        switch (options.Browser.ToLowerInvariant())
        {
            case "edge":
                browser = Browser.Edge;
                WriteToLog("Using Edge browser");
                break;

            case "chrome":
                browser = Browser.Chrome;
                WriteToLog("Using Chrome browser");
                break;

            default:
                browser = Browser.Chrome;
                WriteToLog("No browser has been giving, trying to find Chrome browser. Use the --browser option if you want to use another browser");
                break;
        }

        using var converter = new Converter(options.ChromiumLocation, options.ChromiumUserProfile, _logger, browser: browser);
        SetConverterSettings(converter, options);

        converter.ConvertToPdf(CheckInput(options),
            options.Output,
            pageSettings,
            options.WaitForWindowStatus,
            options.WaitForWindowStatusTimeOut,
            options.Timeout,
            options.MediaLoadTimeout);

        stopWatch.Stop();
        WriteToLog($"Conversion took {stopWatch.ElapsedMilliseconds} ms");
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
    /// that are in the <paramref name="itemsToConvert"/> queue
    /// </summary>
    /// <param name="itemsConverted"></param>
    /// <param name="options"></param>
    /// <param name="instanceId"></param>
    /// <param name="itemsToConvert"></param>
    private static async Task ConvertWithTask(ConcurrentQueue<ConversionItem> itemsToConvert, ConcurrentQueue<ConversionItem> itemsConverted, Options options, string? instanceId)
    {
        var pageSettings = GetPageSettings(options);

        var logger = !string.IsNullOrWhiteSpace(options.LogFile)
            ? new Stream(File.OpenWrite(ReplaceWildCards(options.LogFile)))
            : new ChromiumHtmlToPdfLib.Loggers.Console();

        using (logger)
        {
            await using var converter = new Converter(options.ChromiumLocation, options.ChromiumUserProfile, logger);
            converter.InstanceId = instanceId;

            SetConverterSettings(converter, options);

            while (!itemsToConvert.IsEmpty)
            {
                if (!itemsToConvert.TryDequeue(out var itemToConvert)) continue;
                try
                {
                    await converter.ConvertToPdfAsync(itemToConvert.InputUri, itemToConvert.OutputFile, pageSettings,
                        options.WaitForWindowStatus, options.WaitForWindowStatusTimeOut, options.Timeout,
                        options.MediaLoadTimeout);

                    itemToConvert.SetStatus(ConversionItemStatus.Success);
                }
                catch (Exception exception)
                {
                    itemToConvert.SetStatus(ConversionItemStatus.Failed, exception);
                }

                itemsConverted.Enqueue(itemToConvert);
            }
        }
    }
    #endregion

    #region WriteToLog
    /// <summary>
    ///     Writes a line to the <see cref="_logger" />
    /// </summary>
    /// <param name="message">The message to write</param>
    internal static void WriteToLog(string message)
    {
        try
        {
            if (_logger == null) return;
            _logger.LogInformation(message);
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }
    #endregion
}
