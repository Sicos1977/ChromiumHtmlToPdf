//
// Converter.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2024 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ChromiumHtmlToPdfLib.Enums;
using ChromiumHtmlToPdfLib.Exceptions;
using ChromiumHtmlToPdfLib.Helpers;
using ChromiumHtmlToPdfLib.Loggers;
using ChromiumHtmlToPdfLib.Settings;
using Ganss.Xss;
using Microsoft.Extensions.Logging;
using File = System.IO.File;
using Stream = System.IO.Stream;

// ReSharper disable UnusedMember.Global

namespace ChromiumHtmlToPdfLib;

/// <summary>
///     A converter class around Chromium headless to convert html to pdf
/// </summary>
#if (NETSTANDARD2_0)
public class Converter : IDisposable
#else
public class Converter : IDisposable, IAsyncDisposable
#endif
{
    #region Private enum OutputFormat
    /// <summary>
    ///     The output format
    /// </summary>
    private enum OutputFormat
    {
        /// <summary>
        ///     PDF
        /// </summary>
        Pdf,

        /// <summary>
        ///     Image
        /// </summary>
        Image
    }
    #endregion

    #region Fields
    /// <summary>
    ///     <see cref="Enums.Browser" />
    /// </summary>
    private readonly Enums.Browser _browserUsed;

    /// <summary>
    ///     The default Chromium arguments
    /// </summary>
    private List<string> _defaultChromiumArgument;

    /// <summary>
    ///     A helper class to share logging between the different classes
    /// </summary>
    private Logger _logger;

    /// <summary>
    ///     Chrome or Edge with it's full path
    /// </summary>
    private readonly string _chromiumExeFileName;

    /// <summary>
    ///     The user to use when starting the Chromium based browser, when blank then it is started under the code running user
    /// </summary>
    private string _userName;

    /// <summary>
    ///     The password for the <see cref="_userName" />
    /// </summary>
    private string _password;

    /// <summary>
    ///     A proxy server
    /// </summary>
    private string _proxyServer;

    /// <summary>
    ///     The proxy bypass list
    /// </summary>
    private string _proxyBypassList;

    /// <summary>
    ///     A web proxy
    /// </summary>
    private WebProxy _webProxy;

    /// <summary>
    ///     The process id under which the Chromium based browser is running
    /// </summary>
    private Process _chromiumProcess;

    /// <summary>
    ///     Handles the communication with the Chromium based browser dev tools
    /// </summary>
    private Browser _browser;

    /// <summary>
    ///     Keeps track is we already disposed our resources
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     When set then this folder is used for temporary files
    /// </summary>
    private string _tempDirectory;

    /// <summary>
    ///     The timeout for a conversion
    /// </summary>
    private int? _conversionTimeout;

    /// <summary>
    ///     Used to add the extension of text based files that needed to be wrapped in an HTML PRE
    ///     tag so that they can be opened by the Chromium based browser
    /// </summary>
    private List<string> _preWrapExtensions;

    /// <summary>
    ///     A list with URL's to blacklist
    /// </summary>
    private List<string> _urlBlacklist;

    /// <summary>
    ///     A flag to keep track if a user-data-dir has been set
    /// </summary>
    private readonly bool _userProfileSet;

    /// <summary>
    ///     The file that Chromium creates to tell us on what port it is listening with the devtools
    /// </summary>
    private readonly string _devToolsActivePortFile;

    /// <summary>
    ///     When <c>true</c> then caching will be enabled
    /// </summary>
    private bool _useCache;

    /// <summary>
    ///     <see cref="SetDiskCache"/>
    /// </summary>
    private string _cacheDirectory;

    /// <summary>
    ///     <see cref="SetDiskCache"/>
    /// </summary>
    /// <remarks>
    ///     Default set to 1GB
    /// </remarks>
    private long _cacheSize = 1073741824;

    /// <summary>
    ///     The instance id
    /// </summary>
    private string _instanceId;

    private CancellationTokenSource _cancellationTokenSource;
    #endregion

    #region Properties
    /// <summary>
    ///     Returns the Chromium based browser name
    /// </summary>
    private string BrowserName
    {
        get
        {
            switch (_browserUsed)
            {
                case Enums.Browser.Chrome:
                    return "Google Chrome";

                case Enums.Browser.Edge:
                    return "Microsoft Edge";

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    ///     Returns the Chromium based browser process id or <c>null</c> when the browser is not running (anymore)
    /// </summary>
    public int? ChromiumProcessId
    {
        get
        {
            if (!IsChromiumRunning)
                return null;

            return _chromiumProcess.Id;
        }
    }

    /// <summary>
    ///     Returns <c>true</c> when the Chromium based browser is running
    /// </summary>
    /// <returns></returns>
    public bool IsChromiumRunning
    {
        get
        {
            if (_chromiumProcess == null)
                return false;

            _chromiumProcess.Refresh();
            return !_chromiumProcess.HasExited;
        }
    }

    /// <summary>
    ///     Returns the list with default arguments that are send to the Chromium based browser when starting
    /// </summary>
    public IReadOnlyCollection<string> DefaultChromiumArguments => _defaultChromiumArgument.AsReadOnly();

    /// <summary>
    ///     An unique id that can be used to identify the logging of the converter when
    ///     calling the code from multiple threads and writing all the logging to the same file
    /// </summary>
    public string InstanceId
    {
        get => _instanceId;
        set
        {
            _instanceId = value;
            if (_logger != null)
                _logger.InstanceId = value;
        } 
    }

    /// <summary>
    ///     Used to add the extension of text based files that needed to be wrapped in an HTML PRE
    ///     tag so that they can be opened by Chromium
    /// </summary>
    /// <example>
    ///     <code>
    ///     var converter = new Converter()
    ///     converter.PreWrapExtensions.Add(".txt");
    ///     converter.PreWrapExtensions.Add(".log");
    ///     // etc ...
    ///     </code>
    /// </example>
    /// <remarks>
    ///     The extensions are used case insensitive
    /// </remarks>
    public List<string> PreWrapExtensions
    {
        get => _preWrapExtensions;
        set
        {
            _preWrapExtensions = value;
            _logger?.WriteToLog($"Setting pre wrap extension to '{string.Join(", ", value)}'");
        }
    }

    /// <summary>
    ///     When set to <c>true</c> then images are resized to fix the given <see cref="PageSettings.PaperWidth" />
    /// </summary>
    public bool ImageResize { get; set; }

    /// <summary>
    ///     When set to <c>true</c> then images are automatically rotated following the orientation
    ///     set in the EXIF information
    /// </summary>
    public bool ImageRotate { get; set; }

    /// <summary>
    ///     When set to <c>true</c>  the HTML is sanitized. All not allowed attributes
    ///     will be removed
    /// </summary>
    /// <remarks>
    ///     See https://github.com/mganss/HtmlSanitizer for all the default settings,<br />
    ///     Use <see cref="Sanitizer" /> if you want to control the sanitizer yourself
    /// </remarks>
    public bool SanitizeHtml { get; set; }

    /// <summary>
    ///     The timeout in milliseconds before this application aborts the downloading
    ///     of images when the option <see cref="ImageResize" /> and/or <see cref="ImageRotate" />
    ///     option is being used
    /// </summary>
    /// <remarks>
    ///     <see cref="DocumentHelper.FitPageToContentAsync"/><br />
    ///     <see cref="DocumentHelper.ValidateImagesAsync"/><br />
    /// </remarks>
    public int? ImageLoadTimeout { get; set; }

    /// <summary>
    ///     When set then these settings will be used when <see cref="SanitizeHtml" /> is
    ///     set to <c>true</c>
    /// </summary>
    /// <remarks>
    ///     See https://github.com/mganss/HtmlSanitizer for all the default settings
    /// </remarks>
    public HtmlSanitizer Sanitizer { get; set; }

    /// <summary>
    ///     Runs the given javascript after the webpage has been loaded and before it is converted
    ///     to PDF
    /// </summary>
    public string RunJavascript { get; set; }

    /// <summary>
    ///     When set then this directory is used to store temporary files.
    ///     For example files that are made in combination with <see cref="PreWrapExtensions" />
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Raised when the given directory does not exists</exception>
    public string TempDirectory
    {
        get => _tempDirectory;
        set
        {
            if (!Directory.Exists(value))
                throw new DirectoryNotFoundException($"The directory '{value}' does not exists");

            _tempDirectory = value;
            CurrentCacheDirectory = null;
        }
    }

    /// <summary>
    ///     Returns a reference to the temp directory
    /// </summary>
    private DirectoryInfo GetTempDirectory
    {
        get
        {
            CurrentTempDirectory = _tempDirectory == null
                ? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                : new DirectoryInfo(Path.Combine(_tempDirectory, Guid.NewGuid().ToString()));

            if (!CurrentTempDirectory.Exists)
                CurrentTempDirectory.Create();

            return CurrentTempDirectory;
        }
    }

    /// <summary>
    ///     When set then the temp folders are NOT deleted after conversion
    /// </summary>
    /// <remarks>
    ///     For debugging purposes
    /// </remarks>
    public bool DoNotDeleteTempDirectory { get; set; }

    /// <summary>
    ///     The directory used for temporary files (<see cref="GetTempDirectory"/>)
    /// </summary>
    public DirectoryInfo CurrentTempDirectory { get; internal set; }

    /// <summary>
    ///     The directory used for cached files (<see cref="GetCacheDirectory"/>)
    /// </summary>
    public DirectoryInfo CurrentCacheDirectory { get; internal set; }

    /// <summary>
    ///     Returns a <see cref="WebProxy" /> object
    /// </summary>
    private WebProxy WebProxy
    {
        get
        {
            if (_webProxy != null)
                return _webProxy;

            try
            {
                if (string.IsNullOrWhiteSpace(_proxyServer))
                    return null;

                NetworkCredential networkCredential = null;

                string[] bypassList = null;

                if (_proxyBypassList != null)
                    bypassList = _proxyBypassList.Split(';');

                if (!string.IsNullOrWhiteSpace(_userName))
                {
                    string userName = null;
                    string domain = null;

                    if (_userName.Contains("\\"))
                    {
                        domain = _userName.Split('\\')[0];
                        userName = _userName.Split('\\')[1];
                    }

                    networkCredential = !string.IsNullOrWhiteSpace(domain)
                        ? new NetworkCredential(userName, _password, domain)
                        : new NetworkCredential(userName, _password);
                }

                if (networkCredential != null)
                {
                    _logger?.WriteToLog(
                        $"Setting up web proxy with server '{_proxyServer}' and user '{_userName}'{(!string.IsNullOrEmpty(networkCredential.Domain) ? $" on domain '{networkCredential.Domain}'" : string.Empty)}");
                    _webProxy = new WebProxy(_proxyServer, true, bypassList, networkCredential);
                }
                else
                {
                    _webProxy = new WebProxy(_proxyServer, true, bypassList) { UseDefaultCredentials = true };
                    _logger?.WriteToLog($"Setting up web proxy with server '{_proxyServer}' with default credentials");
                }

                return _webProxy;
            }
            catch (Exception exception)
            {
                throw new Exception("Could not configure web proxy", exception);
            }
        }
    }

    /// <summary>
    ///     When set to <c>true</c> then a snapshot of the page is written to the
    ///     <see cref="SnapshotStream" /> property before the loaded page is converted
    ///     to PDF
    /// </summary>
    public bool CaptureSnapshot { get; set; }

    /// <summary>
    ///     The <see cref="System.IO.Stream" /> where to write the page snapshot when <see cref="CaptureSnapshot" />
    ///     is set to <c>true</c>
    /// </summary>
    public Stream SnapshotStream { get; set; }

    /// <summary>
    ///     When enabled network traffic is also logged
    /// </summary>
    public bool LogNetworkTraffic { get; set; }

    /// <summary>
    ///     Gets or sets the disk cache state
    /// </summary>
    public bool DiskCacheDisabled
    {
        get => !_useCache;
        set => _useCache = !value;
    }

    /// <summary>
    ///     Returns a reference to the cache directory
    /// </summary>
    private DirectoryInfo GetCacheDirectory
    {
        get
        {
            if (CurrentCacheDirectory != null)
                return CurrentCacheDirectory;

            if (_cacheDirectory != null)
                CurrentCacheDirectory = new DirectoryInfo(_cacheDirectory);
            else
                CurrentCacheDirectory = _tempDirectory == null
                    ? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                    : new DirectoryInfo(Path.Combine(_tempDirectory, Guid.NewGuid().ToString()));

            if (!CurrentTempDirectory.Exists)
                CurrentTempDirectory.Create();

            return CurrentCacheDirectory;
        }
    }

    /// <summary>
    ///     The timeout in milliseconds when waiting for a web socket to open
    /// </summary>
    /// <remarks>
    ///     Default 30 seconds
    /// </remarks>
    public int WebSocketTimeout { get; set; } = 30000;

    /// <summary>
    ///     Bij default we wait for the Page.loadEventFired to determine that the page is loaded.
    ///     In most cases this works fine but when you have a page that is loading a lot of interactive
    ///     resources this can sometimes result in a blank page. To prevent this you can set this to <c>true</c>
    ///     but your page will load slower
    /// </summary>
    public bool WaitForNetworkIdle { get; set; }

    /// <summary>
    ///     By default the Chromium based browser is started with the <c>--headless=new</c> argument.
    ///     If you don't want this then set this property to <c>true</c>
    /// </summary>
    /// <remarks>
    ///     https://developer.chrome.com/articles/new-headless/
    /// </remarks>
    public bool UseOldHeadlessMode
    {
        get => !_defaultChromiumArgument.Contains("--headless=new");
        set
        {
            for (var i = 0; i < _defaultChromiumArgument.Count; i++)
            {
                if (!_defaultChromiumArgument[i].StartsWith("--headless")) continue;
                _defaultChromiumArgument[i] = value ? "--headless" : "--headless=new";
                return;
            }
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    ///     Creates this object and sets it's needed properties
    /// </summary>
    /// <param name="chromiumExeFileName">
    ///     When set then this has to be the full path to the Google Chrome
    ///     or Microsoft Edge executable. When not set then then the converter tries to find Chrome.exe or
    ///     msEdge.exe by first looking in the path where this library exists. After that it tries to find
    ///     it by looking into the registry
    /// </param>
    /// <param name="userProfile">
    ///     If set then this directory will be used to store a user profile.
    ///     Leave blank or set to <c>null</c> if you want to use the default Chrome or Edge user profile location
    /// </param>
    /// <param name="logger">
    ///     When set then logging is written to this ILogger instance for all conversions at the Information
    ///     log level
    /// </param>
    /// <param name="useCache">When <c>true</c> (default) then Chrome or Edge uses it disk cache when possible</param>
    /// <param name="browser">
    ///     The Chromium based browser to use in this library, currently Google Chrome or Microsoft Edge are
    ///     supported
    /// </param>
    /// <exception cref="FileNotFoundException">Raised when <paramref name="chromiumExeFileName" /> does not exists</exception>
    /// <exception cref="DirectoryNotFoundException">
    ///     Raised when the <paramref name="userProfile" /> directory is given but does not exists
    /// </exception>
    public Converter(string chromiumExeFileName = null,
        string userProfile = null,
        ILogger logger = null,
        bool useCache = true,
        Enums.Browser browser = Enums.Browser.Chrome)
    {
        _preWrapExtensions = new List<string>();
        _logger = new Logger(logger, null);
        _useCache = useCache;
        _browserUsed = browser;

        ResetChromiumArguments();

        if (string.IsNullOrWhiteSpace(chromiumExeFileName))
            switch (browser)
            {
                case Enums.Browser.Chrome:
                    chromiumExeFileName = ChromeFinder.Find();
                    break;
                case Enums.Browser.Edge:
                    chromiumExeFileName = EdgeFinder.Find();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(browser), browser, null);
            }

        if (!File.Exists(chromiumExeFileName))
            throw new FileNotFoundException($"Could not find {BrowserName} in location '{chromiumExeFileName}'");

        _chromiumExeFileName = chromiumExeFileName;

        if (string.IsNullOrWhiteSpace(userProfile)) return;
        var userProfileDirectory = new DirectoryInfo(userProfile);
        if (!userProfileDirectory.Exists)
            throw new DirectoryNotFoundException($"The directory '{userProfileDirectory.FullName}' does not exists");

        _userProfileSet = true;
        _devToolsActivePortFile = Path.Combine(userProfileDirectory.FullName, "DevToolsActivePort");
        AddChromiumArgument("--user-data-dir", userProfileDirectory.FullName);
    }
    #endregion

    #region StartChromiumHeadlessAsync
    /// <summary>
    ///     Start Google Chrome or Microsoft Edge headless
    /// </summary>
    /// <remarks>
    ///     If Chrome or Edge is already running then this step is skipped
    /// </remarks>
    /// <returns><see cref="CancellationToken"/></returns>
    /// <exception cref="ChromiumException"></exception>
    private async Task StartChromiumHeadlessAsync(CancellationToken cancellationToken)
    {
        if (IsChromiumRunning)
        {
            _logger?.WriteToLog($"{BrowserName} is already running on process id {_chromiumProcess.Id} ... skipped");
            return;
        }

        var workingDirectory = Path.GetDirectoryName(_chromiumExeFileName);

        _logger?.WriteToLog($"Starting {BrowserName} from location '{_chromiumExeFileName}' with working directory '{workingDirectory}'");
        _logger?.WriteToLog($"\"{_chromiumExeFileName}\" {string.Join(" ", DefaultChromiumArguments)}");

        _chromiumProcess = new Process();
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _chromiumExeFileName,
            Arguments = string.Join(" ", DefaultChromiumArguments),
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(_userName))
        {
            string userName;
            var domain = string.Empty;

            if (_userName.Contains("\\"))
            {
                userName = _userName.Split('\\')[1];
                domain = _userName.Split('\\')[0];
            }
            else
            {
                userName = _userName;
            }

            if (!string.IsNullOrWhiteSpace(domain) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger?.WriteToLog($"Starting {BrowserName} with username '{userName}' on domain '{domain}'");
                processStartInfo.Domain = domain;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(domain) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    _logger?.WriteToLog($"Ignoring domain '{domain}' because this is only supported on Windows");

                _logger?.WriteToLog($"Starting {BrowserName} with username '{userName}'");
            }

            processStartInfo.UseShellExecute = false;
            processStartInfo.UserName = userName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrEmpty(_password))
                {
                    var secureString = new SecureString();
                    foreach (var c in _password)
                        secureString.AppendChar(c);

                    processStartInfo.Password = secureString;
                }

                processStartInfo.LoadUserProfile = true;
            }
            else
                _logger?.WriteToLog("Ignoring password and loading user profile because this is only supported on Windows");
        }

        using var chromiumWaitSignal = new SemaphoreSlim(0, 1);

        if (!_userProfileSet)
        {
            _chromiumProcess.ErrorDataReceived += OnChromiumProcessOnErrorDataReceived;

            _chromiumProcess.EnableRaisingEvents = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardError = true;
        }
        else if (File.Exists(_devToolsActivePortFile))
            File.Delete(_devToolsActivePortFile);

        string chromeException = null;
        _chromiumProcess.StartInfo = processStartInfo;
        _chromiumProcess.Exited += OnChromiumProcessOnExited;

        try
        {
            _chromiumProcess.Start();
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Could not start the {BrowserName} process due to the following reason: {ExceptionHelpers.GetInnerException(exception)}");
            throw;
        }

        _logger?.WriteToLog($"{BrowserName} process started");

        if (!_userProfileSet)
            _chromiumProcess.BeginErrorReadLine();
        else
        {
            var lines = await ReadDevToolsActiveFileAsync(cancellationToken).ConfigureAwait(false);
            var uri = new Uri($"ws://127.0.0.1:{lines[0]}{lines[1]}");
            // DevToolsActivePort
            ConnectToDevProtocol(uri, "dev tools active port file");
            chromiumWaitSignal.Release();
        }
        
        if (_conversionTimeout.HasValue)
        {
            var result = await chromiumWaitSignal.WaitAsync(_conversionTimeout.Value, cancellationToken).ConfigureAwait(false);
            if (!result)
                throw new ChromiumException($"A timeout of '{_conversionTimeout.Value}' milliseconds exceeded, could not make a connection to the Chromium dev tools");
        }
        else
        {
            var result = await chromiumWaitSignal.WaitAsync(30000, cancellationToken).ConfigureAwait(false);
            if (!result)
                throw new ChromiumException("A timeout of 30 seconds exceeded, could not make a connection to the Chromium dev tools");
        }

        // Remove events because we don't need them anymore
        _chromiumProcess.ErrorDataReceived -= OnChromiumProcessOnErrorDataReceived;
        _chromiumProcess.Exited -= OnChromiumProcessOnExited;

        if (!string.IsNullOrEmpty(chromeException))
            throw new ChromiumException(chromeException);

        _logger?.WriteToLog($"{BrowserName} started");
        return;

        #region Method internal events
        void OnChromiumProcessOnExited(object o, EventArgs eventArgs)
        {
            try
            {
                if (_chromiumProcess == null) return;
                _logger?.WriteToLog($"{BrowserName} exited unexpectedly, arguments used: {string.Join(" ", DefaultChromiumArguments)}");
                _logger?.WriteToLog($"Process id: {_chromiumProcess.Id}");
                _logger?.WriteToLog($"Process exit time: {_chromiumProcess.ExitTime:yyyy-MM-ddTHH:mm:ss.fff}");
                var exception = ExceptionHelpers.GetInnerException(Marshal.GetExceptionForHR(_chromiumProcess.ExitCode));
                chromeException = $"{BrowserName} exited unexpectedly{(!string.IsNullOrWhiteSpace(exception) ? ", {exception}" : string.Empty)}";
                _cancellationTokenSource?.Cancel();
            }
            finally
            {
                // ReSharper disable once AccessToDisposedClosure
                chromiumWaitSignal.Release();
            }
        }

        void OnChromiumProcessOnErrorDataReceived(object _, DataReceivedEventArgs args)
        {
            if (args.Data == null || string.IsNullOrEmpty(args.Data) || args.Data.StartsWith("[")) return;

            _logger?.WriteToLog($"Received Chromium error data: '{args.Data}'");

            if (!args.Data.StartsWith("DevTools listening on")) return;
            var uri = new Uri(args.Data.Replace("DevTools listening on ", string.Empty));
            ConnectToDevProtocol(uri, "data received from error stream");
            // ReSharper disable once AccessToDisposedClosure
            chromiumWaitSignal.Release();
        }
        #endregion
    }
    #endregion

    #region ReadDevToolsActiveFileAsync
    /// <summary>
    ///     Tries to read the content of the DevToolsActiveFile
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="CancellationToken"/></returns>
    /// <exception cref="ChromiumException"></exception>
    private async Task<string[]> ReadDevToolsActiveFileAsync(CancellationToken cancellationToken)
    {
        var tempTimeout = _conversionTimeout ?? 10000;
        var timeout = tempTimeout;

        _logger?.WriteToLog($"Waiting until file '{_devToolsActivePortFile}' exists with a timeout of {tempTimeout} milliseconds");

        while (true)
        {
            if (!File.Exists(_devToolsActivePortFile))
            {
                timeout -= 5;
                await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                if (timeout <= 0)
                    throw new ChromiumException($"A timeout of '{tempTimeout}' milliseconds exceeded, the file '{_devToolsActivePortFile}' did not exist");
            }
            else
            {
                try
                {
#if (NETSTANDARD2_0)
                    return File.ReadAllLines(_devToolsActivePortFile);
#else
                    return await File.ReadAllLinesAsync(_devToolsActivePortFile, cancellationToken).ConfigureAwait(false);
#endif
                }
                catch (Exception exception)
                {
                    timeout -= 5;
                    await Task.Delay(5, cancellationToken).ConfigureAwait(false);
                    if (timeout <= 0)
                        throw new ChromiumException($"A timeout of '{tempTimeout}' milliseconds exceeded, could not read the file '{_devToolsActivePortFile}'", exception);
                }
            }
        }
    }
    #endregion

    #region ConnectToDevProtocol
    /// <summary>
    ///     Connects to the Chromium dev protocol
    /// </summary>
    /// <param name="uri">The uri to connect to</param>
    /// <param name="readUriFrom">From where we did get the uri</param>
    private void ConnectToDevProtocol(Uri uri, string readUriFrom)
    {
        _logger?.WriteToLog($"Connecting to dev protocol on uri '{uri}', got uri from {readUriFrom}");
        _browser = new Browser(uri, WebSocketTimeout, _logger);
        _logger?.WriteToLog("Connected to dev protocol");
    }
    #endregion

    #region CheckIfOutputFolderExists
    /// <summary>
    ///     Checks if the path to the given <paramref name="outputFile" /> exists.
    ///     An <see cref="DirectoryNotFoundException" /> is thrown when the path is not valid
    /// </summary>
    /// <param name="outputFile"></param>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private static void CheckIfOutputFolderExists(string outputFile)
    {
        var directory = new FileInfo(outputFile).Directory;
        if (directory is { Exists: false })
            throw new DirectoryNotFoundException($"The path '{directory.FullName}' does not exists");
    }
    #endregion

    #region ResetChromiumArguments
    /// <summary>
    ///     Resets the <see cref="DefaultChromiumArguments" /> to their default settings
    /// </summary>
    public void ResetChromiumArguments()
    {
        _logger?.WriteToLog("Resetting Chromium arguments to default");

        _defaultChromiumArgument = new List<string>();
        
        AddChromiumArgument("--headless=new");                          // Use the new headless mode
        AddChromiumArgument("--block-new-web-contents");                // All pop-ups and calls to window.open will fail.
        AddChromiumArgument("--hide-scrollbars");                       // Hide scrollbars from screenshots
        AddChromiumArgument("--disable-domain-reliability");            // Disables Domain Reliability Monitoring, which tracks whether the browser has difficulty contacting Google-owned sites and uploads reports to Google.
        AddChromiumArgument("--disable-sync");                          // Disable syncing to a Google account
        AddChromiumArgument("--mute-audio");                            // Mute any audio
        AddChromiumArgument("--disable-background-networking");         // Disable various background network services, including extension updating,safe browsing service, upgrade detector, translate, UMA
        AddChromiumArgument("--disable-background-timer-throttling");   // Disable timers being throttled in background pages/tabs
        AddChromiumArgument("--disable-default-apps");                  // Disable installation of default apps
        AddChromiumArgument("--disable-extensions");                    // Disable all chrome extensions
        AddChromiumArgument("--disable-hang-monitor");                  // Suppresses hang monitor dialogs in renderer processes. This flag may allow slow unload handlers on a page to prevent the tab from closing.
        AddChromiumArgument("--disable-prompt-on-repost");              // Reloading a page that came from a POST normally prompts the user.
        AddChromiumArgument("--metrics-recording-only");                // Disable reporting to UMA, but allows for collection
        AddChromiumArgument("--no-first-run");                          // Skip first run wizards
        AddChromiumArgument("--no-default-browser-check");              // Disable the default browser check, do not prompt to set it as such
        AddChromiumArgument("--enable-automation");
        AddChromiumArgument("--no-pings");                              // Disable sending hyperlink auditing pings
        AddChromiumArgument("--noerrdialogs");                          // Suppresses all error dialogs when present.
        AddChromiumArgument("--run-all-compositor-stages-before-draw"); 
        AddChromiumArgument("--remote-debugging-port", "0");            // With a value of 0, Chrome will automatically select a useable port and will set navigator.webdriver to true.

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger?.WriteToLog("Detected Linux operating system, adding the parameter '--no-sandbox'");
            AddChromiumArgument("--no-sandbox");
        }

        SetWindowSize(WindowSize.HD_1366_768);
    }
    #endregion

    #region RemoveChromiumArgument
    /// <summary>
    ///     Removes the given <paramref name="argument" /> from <see cref="DefaultChromiumArguments" />
    /// </summary>
    /// <param name="argument">The Chromium argument</param>
    // ReSharper disable once UnusedMember.Local
    public void RemoveChromiumArgument(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Argument is null, empty or white space");

        if (argument.StartsWith("--headless"))
            throw new ArgumentException("Can't remove '--headless' argument, this argument is always needed");
        
        switch (argument)
        {
            case "--no-first-run":
                throw new ArgumentException("Can't remove '--no-first-run' argument, this argument is always needed");

            case "--remote-debugging-port=\"0\"":
                throw new ArgumentException(
                    "Can't remove '---remote-debugging-port=\"0\"' argument, this argument is always needed");
        }

        if (!_defaultChromiumArgument.Contains(argument)) return;
        _defaultChromiumArgument.Remove(argument);
        _logger?.WriteToLog($"Removed Chromium argument '{argument}'");
    }
    #endregion

    #region AddChromiumArgument
    /// <summary>
    ///     Adds an extra conversion argument to the <see cref="DefaultChromiumArguments" />
    /// </summary>
    /// <remarks>
    ///     This is a one time only default setting which can not be changed when doing multiple conversions.
    ///     Set this before doing any conversions. You can get all the set argument through the
    ///     <see cref="DefaultChromiumArguments" /> property
    /// </remarks>
    /// <param name="argument">The Chromium argument</param>
    public void AddChromiumArgument(string argument)
    {
        if (IsChromiumRunning)
            throw new ChromiumException(
                $"{BrowserName} is already running, you need to set the argument '{argument}' before staring the browser");

        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Argument is null, empty or white space");

        if (!_defaultChromiumArgument.Contains(argument, StringComparison.CurrentCultureIgnoreCase))
        {
            _logger?.WriteToLog($"Adding Chromium argument '{argument}'");
            _defaultChromiumArgument.Add(argument);
        }
        else
            _logger?.WriteToLog($"The Chromium argument '{argument}' has already been set, ignoring it");
    }

    /// <summary>
    ///     Adds an extra conversion argument with value to the <see cref="DefaultChromiumArguments" />
    ///     or replaces it when it already exists
    /// </summary>
    /// <remarks>
    ///     This is a one time only default setting which can not be changed when doing multiple conversions.
    ///     Set this before doing any conversions. You can get all the set argument through the
    ///     <see cref="DefaultChromiumArguments" /> property
    /// </remarks>
    /// <param name="argument">The Chromium argument</param>
    /// <param name="value">The argument value</param>
    public void AddChromiumArgument(string argument, string value)
    {
        if (IsChromiumRunning)
            throw new ChromiumException(
                $"{BrowserName} is already running, you need to set the argument '{argument}' before staring the browser");

        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Argument is null, empty or white space");

        for (var i = 0; i < _defaultChromiumArgument.Count; i++)
        {
            if (!_defaultChromiumArgument[i].StartsWith(argument + "=")) continue;

            _logger?.WriteToLog($"Updating Chromium argument '{_defaultChromiumArgument[i]}' with value '{value}'");
            _defaultChromiumArgument[i] = $"{argument}=\"{value}\"";
            return;
        }

        _logger?.WriteToLog($"Adding Chromium argument '{argument}=\"{value}\"'");
        _defaultChromiumArgument.Add($"{argument}=\"{value}\"");
    }
    #endregion

    #region SetProxyServer
    /// <summary>
    ///     Instructs Chromium to use the provided proxy server
    /// </summary>
    /// <param name="value"></param>
    /// <example>
    ///     &lt;scheme&gt;=&lt;uri&gt;[:&lt;port&gt;][;...] | &lt;uri&gt;[:&lt;port&gt;] | "direct://"
    ///     This tells Chromium to use a custom proxy configuration. You can specify a custom proxy configuration in three
    ///     ways:
    ///     1) By providing a semi-colon-separated mapping of list scheme to url/port pairs.
    ///     For example, you can specify:
    ///     "http=foopy:80;ftp=foopy2"
    ///     to use HTTP proxy "foopy:80" for http URLs and HTTP proxy "foopy2:80" for ftp URLs.
    ///     2) By providing a single uri with optional port to use for all URLs.
    ///     For example:
    ///     "foopy:8080"
    ///     will use the proxy at foopy:8080 for all traffic.
    ///     3) By using the special "direct://" value.
    ///     "direct://" will cause all connections to not use a proxy.
    /// </example>
    /// <remarks>
    ///     Set this parameter before starting Google Chrome or Microsoft Edge
    /// </remarks>
    public void SetProxyServer(string value)
    {
        _proxyServer = value;
        AddChromiumArgument("--proxy-server", value);
    }
    #endregion

    #region SetProxyBypassList
    /// <summary>
    ///     This tells Chromium to bypass any specified proxy for the given semi-colon-separated list of hosts.
    ///     This flag must be used (or rather, only has an effect) in tandem with <see cref="SetProxyServer" />.
    ///     Note that trailing-domain matching doesn't require "." separators so "*google.com" will match "igoogle.com" for
    ///     example.
    /// </summary>
    /// <param name="values"></param>
    /// <example>
    ///     "foopy:8080" --proxy-bypass-list="*.google.com;*foo.com;127.0.0.1:8080"
    ///     will use the proxy server "foopy" on port 8080 for all hosts except those pointing to *.google.com, those pointing
    ///     to *foo.com and those pointing to localhost on port 8080.
    ///     igoogle.com requests would still be proxied. ifoo.com requests would not be proxied since *foo, not *.foo was
    ///     specified.
    /// </example>
    /// <remarks>
    ///     Set this parameter before starting Google Chrome or Microsoft Edge
    /// </remarks>
    public void SetProxyBypassList(string values)
    {
        _proxyBypassList = values;
        AddChromiumArgument("--proxy-bypass-list", values);
    }
    #endregion

    #region SetProxyPacUrl
    /// <summary>
    ///     This tells Chromium to use the PAC file at the specified URL.
    /// </summary>
    /// <param name="value"></param>
    /// <example>
    ///     "http://wpad/windows.pac"
    ///     will tell Chromium to resolve proxy information for URL requests using the windows.pac file.
    /// </example>
    /// <remarks>
    ///     Set this parameter before starting Google Chrome or Microsoft Edge
    /// </remarks>
    public void SetProxyPacUrl(string value)
    {
        AddChromiumArgument("--proxy-pac-url", value);
    }
    #endregion

    #region SetUserAgent
    /// <summary>
    ///     This tells Chromium to use the given user-agent string
    /// </summary>
    /// <param name="value"></param>
    /// <remarks>
    ///     Set this parameter before starting Google Chrome or Microsoft Edge
    /// </remarks>
    public void SetUserAgent(string value)
    {
        AddChromiumArgument("--user-agent", value);
    }
    #endregion

    #region SetDiskCache
    /// <summary>
    ///     This tells Chromium to cache it's content to the given <paramref name="directory" /> instead of the user profile
    /// </summary>
    /// <param name="directory">The cache directory</param>
    /// <param name="size">The maximum size in megabytes for the cache directory, <c>null</c> to let Chromium decide</param>
    /// <remarks>
    ///     You can not share a cache folder between multiple instances that are running at the same time because a Google
    ///     Chrome or Microsoft Edge instance locks the cache for it self. If you want to use caching in a multi threaded
    ///     environment then assign a unique cache folder to each running Google Chrome or Microsoft Edge instance
    /// </remarks>
    public void SetDiskCache(string directory, long? size)
    {
        var temp = new DirectoryInfo(directory);

        if (!temp.Exists)
            throw new DirectoryNotFoundException($"The directory '{directory}' does not exists");

        _cacheDirectory = temp.FullName;

        // Chromium does not like a trailing slash
        AddChromiumArgument("--disk-cache-dir", _cacheDirectory.TrimEnd('\\', '/'));

        if (size.HasValue)
        {
            if (size.Value <= 0)
                throw new ArgumentException("Has to be a value of 1 or greater", nameof(size));

            _cacheSize = size.Value * 1024 * 1024;
            AddChromiumArgument("--disk-cache-size", (size.Value * 1024 * 1024).ToString());
        }

        _useCache = true;
    }
    #endregion

    #region SetUser
    /// <summary>
    ///     Sets the user under which Chromium wil run. This is useful if you are on a server and
    ///     the user under which the code runs doesn't have access to the internet.
    /// </summary>
    /// <param name="userName">The username with or without a domain name (e.g DOMAIN\USERNAME)</param>
    /// <param name="password">The password for the <paramref name="userName" /></param>
    /// <remarks>
    ///     Set this parameter before starting Google Chrome or Microsoft Edge. On systems other than
    ///     Windows the password can be left empty because this is only supported on Windows.
    /// </remarks>
    public void SetUser(string userName, string password = null)
    {
        _userName = userName;
        _password = password;
    }
    #endregion

    #region SetWindowSize
    /// <summary>
    ///     Sets the viewport size to use when converting
    /// </summary>
    /// <remarks>
    ///     This is a one time only default setting which can not be changed when doing multiple conversions.
    ///     Set this before doing any conversions.
    /// </remarks>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Raised when <paramref name="width" /> or <paramref name="height" /> is smaller then or zero
    /// </exception>
    public void SetWindowSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        AddChromiumArgument("--window-size", width + "," + height);
    }

    /// <summary>
    ///     Sets the window size to use when converting
    /// </summary>
    /// <param name="size"></param>
    public void SetWindowSize(WindowSize size)
    {
        switch (size)
        {
            case WindowSize.SVGA:
                AddChromiumArgument("--window-size", 800 + "," + 600);
                break;
            case WindowSize.WSVGA:
                AddChromiumArgument("--window-size", 1024 + "," + 600);
                break;
            case WindowSize.XGA:
                AddChromiumArgument("--window-size", 1024 + "," + 768);
                break;
            case WindowSize.XGAPLUS:
                AddChromiumArgument("--window-size", 1152 + "," + 864);
                break;
            case WindowSize.WXGA_5_3:
                AddChromiumArgument("--window-size", 1280 + "," + 768);
                break;
            case WindowSize.WXGA_16_10:
                AddChromiumArgument("--window-size", 1280 + "," + 800);
                break;
            case WindowSize.SXGA:
                AddChromiumArgument("--window-size", 1280 + "," + 1024);
                break;
            case WindowSize.HD_1360_768:
                AddChromiumArgument("--window-size", 1360 + "," + 768);
                break;
            case WindowSize.HD_1366_768:
                AddChromiumArgument("--window-size", 1366 + "," + 768);
                break;
            case WindowSize.OTHER_1536_864:
                AddChromiumArgument("--window-size", 1536 + "," + 864);
                break;
            case WindowSize.HD_PLUS:
                AddChromiumArgument("--window-size", 1600 + "," + 900);
                break;
            case WindowSize.WSXGA_PLUS:
                AddChromiumArgument("--window-size", 1680 + "," + 1050);
                break;
            case WindowSize.FHD:
                AddChromiumArgument("--window-size", 1920 + "," + 1080);
                break;
            case WindowSize.WUXGA:
                AddChromiumArgument("--window-size", 1920 + "," + 1200);
                break;
            case WindowSize.OTHER_2560_1070:
                AddChromiumArgument("--window-size", 2560 + "," + 1070);
                break;
            case WindowSize.WQHD:
                AddChromiumArgument("--window-size", 2560 + "," + 1440);
                break;
            case WindowSize.OTHER_3440_1440:
                AddChromiumArgument("--window-size", 3440 + "," + 1440);
                break;
            case WindowSize._4K_UHD:
                AddChromiumArgument("--window-size", 3840 + "," + 2160);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, null);
        }
    }
    #endregion

    #region WildCardToRegEx
    /// <summary>
    ///     Converts a list of strings like test*.txt to a form that can be used in a regular expression
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private List<string> ListToWildCardRegEx(IEnumerable<string> values)
    {
        return values.Select(RegularExpression.Escape).ToList();
    }
    #endregion

    #region SetUrlBlacklist
    /// <summary>
    ///     Sets one or more urls to blacklist when converting a page or file to PDF
    /// </summary>
    /// <remarks>
    ///     Use * as a wildcard, e.g. myurltoblacklist*
    /// </remarks>
    /// <param name="urls"></param>
    public void SetUrlBlacklist(IList<string> urls)
    {
        _urlBlacklist = ListToWildCardRegEx(urls);
    }
    #endregion

    #region GetUrlFromFile
    /// <summary>
    ///     Tries to read the url from a .url file
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool GetUrlFromFile(string fileName, out string url)
    {
        try
        {
            var lines = File.ReadAllLines(fileName);

            foreach (var line in lines)
            {
                var temp = line.ToLowerInvariant();
                if (!temp.StartsWith("url=")) continue;
                // ReSharper disable once ReplaceSubstringWithRangeIndexer
                url = line.Substring(4).Trim();
                return true;
            }
        }
        catch
        {
            // Ignore
        }

        url = null;
        return false;
    }
    #endregion

    #region ConvertAsync
    private async Task ConvertAsync(
        OutputFormat outputFormat,
        object input,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default)
            _cancellationTokenSource = new CancellationTokenSource();

        if (_logger == null)
            _logger = new Logger(logger, InstanceId);
        else if (logger != null)
        {
            _logger.InstanceId = InstanceId;
            _logger.InternalLogger = logger;
        }

        _conversionTimeout = conversionTimeout;

        var safeUrls = new List<string>();
        var inputUri = input as ConvertUri;
        var html = input as string;

        if (inputUri != null && inputUri.IsFile)
        {
            if (!File.Exists(inputUri.OriginalString))
                throw new FileNotFoundException($"The file '{inputUri.OriginalString}' does not exists");

            var ext = Path.GetExtension(inputUri.OriginalString);

            switch (ext.ToLowerInvariant())
            {
                case ".htm":
                case ".html":
                case ".mht":
                case ".mhtml":
                case ".svg":
                case ".xml":
                    // This is ok
                    break;

                case ".url":
                    if (GetUrlFromFile(inputUri.AbsolutePath, out var url))
                    {
                        _logger?.WriteToLog($"Read url '{url}' from URL file '{inputUri.AbsolutePath}'");
                        inputUri = new ConvertUri(url);
                    }

                    break;

                default:
                    if (!PreWrapExtensions.Contains(ext, StringComparison.InvariantCultureIgnoreCase))
                        throw new ConversionException(
                            $"The file '{inputUri.OriginalString}' with extension '{ext}' is not valid. " +
                            "If this is a text based file then add the extension to the PreWrapExtensions");
                    break;
            }
        }

        try
        {
            if (inputUri != null)
            {
                if (inputUri.IsFile && CheckForPreWrap(inputUri, out var preWrapFile))
                {
                    inputUri = new ConvertUri(preWrapFile);
                    _logger?.WriteToLog($"Adding url '{inputUri}' to the safe url list");
                    safeUrls.Add(inputUri.ToString());
                }

                if (ImageResize || ImageRotate || SanitizeHtml || pageSettings.PaperFormat == PaperFormat.FitPageToContent)
                {
                    var documentHelper = new DocumentHelper(GetTempDirectory, _useCache, GetCacheDirectory, _cacheSize, WebProxy, ImageLoadTimeout, _logger);

                    if (SanitizeHtml)
                    {
                        var result = await documentHelper.SanitizeHtmlAsync(inputUri, Sanitizer, safeUrls, cancellationToken).ConfigureAwait(false);
                        if (result.Success)
                        {
                            _logger?.WriteToLog($"Sanitization was successful changing input uri '{inputUri}' to '{result.OutputUri}'");
                            inputUri = result.OutputUri;
                            safeUrls = result.SafeUrls;
                        }
                        else
                        {
                            _logger?.WriteToLog($"Adding url '{inputUri}' to the safe url list");
                            safeUrls.Add(inputUri.ToString());
                        }
                    }

                    if (pageSettings.PaperFormat == PaperFormat.FitPageToContent)
                    {
                        _logger?.WriteToLog("The paper format 'FitPageToContent' is set, modifying html so that the PDF fits the HTML content");
                        var result = await documentHelper.FitPageToContentAsync(inputUri, cancellationToken).ConfigureAwait(false);
                        if (result.Success)
                        {
                            _logger?.WriteToLog($"Fitting the page to the content was successful changing input uri '{inputUri}' to '{result.OutputUri}'");
                            inputUri = result.OutputUri;
                            safeUrls.Add(input.ToString());
                        }
                    }

                    if (ImageResize || ImageRotate)
                    {
                        documentHelper.ResetTimeout();

                        var result = await documentHelper.ValidateImagesAsync(
                            inputUri,
                            ImageResize,
                            ImageRotate,
                            pageSettings,
                            safeUrls,
                            _urlBlacklist,
                            cancellationToken).ConfigureAwait(false);

                        if (result.Success)
                        {
                            _logger?.WriteToLog($"Image validation was successful changing input uri '{inputUri}' to '{result.OutputUri}'");
                            inputUri = result.OutputUri;
                            safeUrls.Add(inputUri.ToString());
                        }
                    }
                }
            }

            await StartChromiumHeadlessAsync(cancellationToken).ConfigureAwait(false);

            CountdownTimer countdownTimer = null;

            if (conversionTimeout.HasValue)
            {
                if (conversionTimeout <= 1)
                    throw new ArgumentOutOfRangeException($"The value for {nameof(countdownTimer)} has to be a value equal to 1 or greater");

                _logger?.WriteToLog($"Conversion timeout set to {conversionTimeout.Value} milliseconds");

                countdownTimer = new CountdownTimer(conversionTimeout.Value);
                countdownTimer.Start();
            }

            if (inputUri != null)
                _logger?.WriteToLog($"Loading {(inputUri.IsFile ? $"file {inputUri.OriginalString}" : $"url {inputUri}")}");

            await _browser.NavigateToAsync(safeUrls, _useCache, inputUri, html, countdownTimer, mediaLoadTimeout, _urlBlacklist, LogNetworkTraffic, WaitForNetworkIdle, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(waitForWindowStatus))
            {
                if (conversionTimeout.HasValue)
                {
                    _logger?.WriteToLog("Conversion timeout paused because we are waiting for a window.status");
                    countdownTimer.Stop();
                }

                _logger?.WriteToLog($"Waiting for window.status '{waitForWindowStatus}' or a timeout of {waitForWindowsStatusTimeout} milliseconds");
                var match = await _browser.WaitForWindowStatusAsync(waitForWindowStatus, waitForWindowsStatusTimeout, cancellationToken).ConfigureAwait(false);
                _logger?.WriteToLog(!match ? "Waiting timed out" : $"Window status equaled {waitForWindowStatus}");

                if (conversionTimeout.HasValue)
                {
                    _logger?.WriteToLog("Conversion timeout started again because we are done waiting for a window.status");
                    countdownTimer.Start();
                }
            }

            if (inputUri != null)
                _logger?.WriteToLog($"{(inputUri.IsFile ? "File" : "Url")} loaded");

            if (!string.IsNullOrWhiteSpace(RunJavascript))
            {
                _logger?.WriteToLog("Start running javascript");
                _logger?.WriteToLog($"Javascript code:{Environment.NewLine}{RunJavascript}");
                await _browser.RunJavascriptAsync(RunJavascript, cancellationToken).ConfigureAwait(false);
                _logger?.WriteToLog("Done running javascript");
            }

            if (CaptureSnapshot)
            {
                if (SnapshotStream == null)
                    throw new ConversionException("The property CaptureSnapshot has been set to true but there is no stream assigned to the SnapshotStream");

                _logger?.WriteToLog("Taking snapshot of the page");

                var snapshot = await _browser.CaptureSnapshotAsync(countdownTimer, cancellationToken).ConfigureAwait(false);
                using var memoryStream = new MemoryStream(snapshot.Bytes);
                memoryStream.Position = 0;

#if (NETSTANDARD2_0)
                await memoryStream.CopyToAsync(SnapshotStream).ConfigureAwait(false);
#else
                await memoryStream.CopyToAsync(SnapshotStream, cancellationToken).ConfigureAwait(false);
#endif

                _logger?.WriteToLog("Taken");
            }

            switch (outputFormat)
            {
                case OutputFormat.Pdf:
                    _logger?.WriteToLog("Converting to PDF");
                    await _browser.PrintToPdfAsync(outputStream, pageSettings, countdownTimer, cancellationToken).ConfigureAwait(false);

                    break;

                case OutputFormat.Image:
                {
                    _logger?.WriteToLog("Converting to image");

                    var snapshot = await _browser.CaptureScreenshotAsync(countdownTimer, cancellationToken).ConfigureAwait(false);
                    using var memoryStream = new MemoryStream(snapshot.Bytes);
                    memoryStream.Position = 0;

#if (NETSTANDARD2_0)
                    await memoryStream.CopyToAsync(outputStream).ConfigureAwait(false);
#else
                    await memoryStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
#endif

                    break;
                }
            }

            _logger?.WriteToLog("Converted");
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Error: {ExceptionHelpers.GetInnerException(exception)}'");

            if (exception.Message != "Input string was not in a correct format.")
                throw;
        }
        finally
        {
            if (CurrentTempDirectory != null)
            {
                CurrentTempDirectory.Refresh();
                if (CurrentTempDirectory.Exists && !DoNotDeleteTempDirectory)
                {
                    _logger?.WriteToLog($"Deleting temporary folder '{CurrentTempDirectory.FullName}'");
                    try
                    {
                        CurrentTempDirectory.Delete(true);
                    }
                    catch (Exception e)
                    {
                        _logger?.WriteToLog($"Error '{ExceptionHelpers.GetInnerException(e)}'");
                    }
                }
            }
        }
    }
    #endregion

    #region WriteSnapShot
    /// <summary>
    ///     Writes the snap shot to the given <paramref name="outputFile" />
    /// </summary>
    /// <param name="outputFile"></param>
    private void WriteSnapShot(string outputFile)
    {
        var snapShotOutputFile = Path.ChangeExtension(outputFile, ".mhtml");

        using (SnapshotStream)
        using (var fileStream = File.Open(snapShotOutputFile, FileMode.Create))
        {
            SnapshotStream.Position = 0;
            SnapshotStream.CopyTo(fileStream);
        }

        _logger?.WriteToLog($"Page snapshot written to output file '{snapShotOutputFile}'");
    }
    #endregion

    #region ConvertToPdf
    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to PDF
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" />
    /// </remarks>
    public void ConvertToPdf(
        ConvertUri inputUri,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToPdfAsync(
            inputUri, 
            outputStream, 
            pageSettings, 
            waitForWindowStatus, 
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }
    
    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to PDF
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)
    /// </remarks>
    public void ConvertToPdf(
        ConvertUri inputUri,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToPdfAsync(
            inputUri,
            outputFile,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to PDF
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /><br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public void ConvertToPdf(
        string html,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToPdfAsync(
            html,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }
    
    /// <summary>
    ///     Converts the given <paramref name="html" /> to PDF
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /><br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public void ConvertToPdf(
        string html,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToPdfAsync(
            html,
            outputFile,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }
    #endregion

    #region ConvertToPdfAsync
    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to PDF
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" />
    /// </remarks>
    public async Task ConvertToPdfAsync(
        ConvertUri inputUri,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        await ConvertAsync(
            OutputFormat.Pdf,
            inputUri,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger, 
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to PDF
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)
    /// </remarks>
    public async Task ConvertToPdfAsync(
        ConvertUri inputUri,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        CheckIfOutputFolderExists(outputFile);

        if (CaptureSnapshot)
            SnapshotStream = new MemoryStream();

        using var memoryStream = new MemoryStream();
        
        await ConvertToPdfAsync(inputUri, memoryStream, pageSettings, waitForWindowStatus, waitForWindowsStatusTimeout,
            conversionTimeout, mediaLoadTimeout, logger, cancellationToken).ConfigureAwait(false);

#if (NETSTANDARD2_0)
        using var fileStream = File.Open(outputFile, FileMode.Create);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
#else
        await using var fileStream = File.Open(outputFile, FileMode.Create);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
#endif

        _logger?.WriteToLog($"PDF written to output file '{outputFile}'");

        if (CaptureSnapshot)
            WriteSnapShot(outputFile);
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to PDF
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /><br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public async Task ConvertToPdfAsync(
        string html,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        await ConvertAsync(
            OutputFormat.Pdf,
            html,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to PDF
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)<br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public async Task ConvertToPdfAsync(
        string html,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        CheckIfOutputFolderExists(outputFile);

        if (CaptureSnapshot)
            SnapshotStream = new MemoryStream();

        using (var memoryStream = new MemoryStream())
        {
            await ConvertToPdfAsync(html, memoryStream, pageSettings, waitForWindowStatus,
                waitForWindowsStatusTimeout, conversionTimeout, mediaLoadTimeout, logger, cancellationToken).ConfigureAwait(false);

            memoryStream.Position = 0;

#if (NETSTANDARD2_0)
            using var fileStream = File.Open(outputFile, FileMode.Create);
            await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
#else
            await using var fileStream = File.Open(outputFile, FileMode.Create);
            await memoryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
#endif

            _logger?.WriteToLog($"PDF written to output file '{outputFile}'");
        }

        if (CaptureSnapshot)
            WriteSnapShot(outputFile);
    }
    #endregion

    #region ConvertToImage
    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to an image (png)
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" />
    /// </remarks>
    public void ConvertToImage(
        ConvertUri inputUri,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToImageAsync(
            inputUri,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to an image (png)
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /><br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public void ConvertToImage(
        string html,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToImageAsync(
            html,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to an image (png)
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when<paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// ///
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)
    /// </remarks>
    public void ConvertToImage(
        ConvertUri inputUri,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToImageAsync(
            inputUri,
            outputFile,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to an image (png)
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// ///
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)<br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public void ConvertToImage(
        string html,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null)
    {
        ConvertToImageAsync(
            html,
            outputFile,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger).GetAwaiter().GetResult();
    }
    #endregion

    #region ConvertToImageAsync
    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to an image (png)
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" />
    /// </remarks>
    public async Task ConvertToImageAsync(
        ConvertUri inputUri,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        await ConvertAsync(
            OutputFormat.Image,
            inputUri,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger, 
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to an image (png)
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputStream">The output stream</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /><br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public async Task ConvertToImageAsync(
        string html,
        Stream outputStream,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        await ConvertAsync(
            OutputFormat.Image,
            html,
            outputStream,
            pageSettings,
            waitForWindowStatus,
            waitForWindowsStatusTimeout,
            conversionTimeout,
            mediaLoadTimeout,
            logger, 
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Converts the given <paramref name="inputUri" /> to an image (png)
    /// </summary>
    /// <param name="inputUri">The webpage to convert</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when<paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// ///
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)
    /// </remarks>
    public async Task ConvertToImageAsync(
        ConvertUri inputUri,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        CheckIfOutputFolderExists(outputFile);

        if (CaptureSnapshot)
            SnapshotStream = new MemoryStream();

        using var memoryStream = new MemoryStream();
        
        await ConvertToImageAsync(inputUri, memoryStream, pageSettings, waitForWindowStatus,
            waitForWindowsStatusTimeout, conversionTimeout, mediaLoadTimeout, logger, cancellationToken).ConfigureAwait(false);

        memoryStream.Position = 0;

#if (NETSTANDARD2_0)
        using var fileStream = File.Open(outputFile, FileMode.Create);
        await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
#else
        await using var fileStream = File.Open(outputFile, FileMode.Create);
        await memoryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
#endif
        _logger?.WriteToLog($"Image written to output file '{outputFile}'");

        if (CaptureSnapshot)
            WriteSnapShot(outputFile);
    }

    /// <summary>
    ///     Converts the given <paramref name="html" /> to an image (png)
    /// </summary>
    /// <param name="html">The html</param>
    /// <param name="outputFile">The output file</param>
    /// <param name="pageSettings">
    ///     <see cref="PageSettings" />
    /// </param>
    /// <param name="waitForWindowStatus">
    ///     Wait until the javascript window.status has this value before
    ///     rendering the PDF
    /// </param>
    /// <param name="waitForWindowsStatusTimeout"></param>
    /// <param name="conversionTimeout">
    ///     An conversion timeout in milliseconds, if the conversion fails
    ///     to finished in the set amount of time then an <see cref="ConversionTimedOutException" /> is raised
    /// </param>
    /// <param name="mediaLoadTimeout">
    ///     When set a timeout will be started after the DomContentLoaded
    ///     event has fired. After a timeout the NavigateTo method will exit as if the page has been completely loaded
    /// </param>
    /// <param name="logger">
    ///     When set then this will give a logging for each conversion. Use the logger
    ///     option in the constructor if you want one log for all conversions
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <exception cref="ConversionTimedOutException">
    ///     Raised when <paramref name="conversionTimeout" /> is set and the
    ///     conversion fails to finish in this amount of time
    /// </exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    /// <remarks>
    ///     When the property <see cref="CaptureSnapshot" /> has been set then the snapshot is saved to the
    ///     property <see cref="SnapshotStream" /> and after that automatic saved to the <paramref name="outputFile" />
    ///     (the extension will automatic be replaced with .mhtml)<br />
    ///     Warning: At the moment this method does not support the properties <see cref="ImageResize" />,
    ///     <see cref="ImageRotate" /> and <see cref="SanitizeHtml" /><br />
    ///     Warning: At the moment this method does not support <see cref="PageSettings.PaperFormat" /> ==
    ///     <c>PaperFormat.FitPageToContent</c>
    /// </remarks>
    public async Task ConvertToImageAsync(
        string html,
        string outputFile,
        PageSettings pageSettings,
        string waitForWindowStatus = "",
        int waitForWindowsStatusTimeout = 60000,
        int? conversionTimeout = null,
        int? mediaLoadTimeout = null,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        CheckIfOutputFolderExists(outputFile);

        if (CaptureSnapshot)
            SnapshotStream = new MemoryStream();

        using var memoryStream = new MemoryStream();
        
        await ConvertToImageAsync(html, memoryStream, pageSettings, waitForWindowStatus,
            waitForWindowsStatusTimeout, conversionTimeout, mediaLoadTimeout, logger, cancellationToken).ConfigureAwait(false);

        memoryStream.Position = 0;

#if (NETSTANDARD2_0)
        using var fileStream = File.Open(outputFile, FileMode.Create);
        await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
#else
        await using var fileStream = File.Open(outputFile, FileMode.Create);
        await memoryStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
#endif

        _logger?.WriteToLog($"Image written to output file '{outputFile}'");

        if (CaptureSnapshot)
            WriteSnapShot(outputFile);
    }
    #endregion

    #region CheckForPreWrap
    /// <summary>
    ///     Checks if <see cref="PreWrapExtensions" /> is set and if the extension
    ///     is inside this list. When in the list then the file is wrapped
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outputFile"></param>
    /// <returns></returns>
    private bool CheckForPreWrap(ConvertUri inputFile, out string outputFile)
    {
        outputFile = inputFile.OriginalString;

        if (PreWrapExtensions.Count == 0)
            return false;

        var ext = Path.GetExtension(inputFile.LocalPath);

        if (!PreWrapExtensions.Contains(ext, StringComparison.InvariantCultureIgnoreCase))
            return false;

        var preWrapper = new PreWrapper(GetTempDirectory, _logger) { InstanceId = InstanceId };
        outputFile = preWrapper.WrapFile(inputFile.OriginalString, inputFile.Encoding);
        return true;
    }
    #endregion

    #region KillProcessAndChildren
    /// <summary>
    ///     Kill the process with given id and all it's children
    /// </summary>
    /// <param name="processId">The process id</param>
    private void KillProcessAndChildren(int processId)
    {
        if (processId == 0) return;

        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
        }
        catch (Exception exception)
        {
            if (!exception.Message.Contains("is not running"))
                _logger?.WriteToLog(exception.Message);
        }
    }
    #endregion

    #region InternalDispose
#if (NETSTANDARD2_0)
    private void InternalDispose()
#else
    private async Task InternalDisposeAsync()
#endif
    {
        if (_disposed)
            return;

        if (_browser != null)
            try
            {
                _logger?.WriteToLog($"Closing {BrowserName} browser gracefully");
#if (NETSTANDARD2_0)
                _browser.Dispose();
#else
                await _browser.DisposeAsync().ConfigureAwait(false);
#endif
                _browser = null;
            }
            catch (Exception exception)
            {
                _logger?.WriteToLog($"An error occurred while trying to close {BrowserName} gracefully, error '{ExceptionHelpers.GetInnerException(exception)}'");
            }

        var counter = 0;

        // Give Chrome 2 seconds to close
        while (counter < 200)
        {
            if (!IsChromiumRunning)
            {
                _logger?.WriteToLog($"{BrowserName} closed gracefully");
                break;
            }

            counter++;
            Thread.Sleep(10);
        }

        if (IsChromiumRunning)
        {
            // Sometimes Chrome does not close all processes so kill them
            _logger?.WriteToLog($"{BrowserName} did not close gracefully, closing it by killing it's process on id '{_chromiumProcess.Id}'");
            KillProcessAndChildren(_chromiumProcess.Id);
            _logger?.WriteToLog($"{BrowserName} killed");

            _chromiumProcess = null;
        }

        _disposed = true;
    }
    #endregion

    #region Dispose
    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
#if (NETSTANDARD2_0)
        InternalDispose();
#else
        InternalDisposeAsync().GetAwaiter().GetResult();
#endif
    }
    #endregion

#if (!NETSTANDARD2_0)    
    #region DisposeAsync
    /// <summary>
    ///     Disposes the running <see cref="_chromiumProcess" />
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await InternalDisposeAsync().ConfigureAwait(false);
    }
    #endregion
#endif
}