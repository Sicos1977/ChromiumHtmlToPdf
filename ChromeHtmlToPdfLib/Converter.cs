//
// Converter.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
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
using System.Net.NetworkInformation;
using System.Security;
using System.Text;
using System.Threading;
using ChromeHtmlToPdfLib.Settings;
using Microsoft.Win32;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using ChromeHtmlToPdfLib.Enums;
using ChromeHtmlToPdfLib.Helpers;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// A converter class around Google Chrome headless to convert html to pdf
    /// </summary>
    public class Converter : IDisposable
    {
        #region Const
        private string ChromeMutexName = "ChromeHtmlToPdfMutex";
        #endregion

        #region Fields
        /// <summary>
        ///     When set then logging is written to this stream
        /// </summary>
        private static Stream _logStream;

        /// <summary>
        ///     Chrome with it's full path
        /// </summary>
        private readonly string _chromeExeFileName;

        /// <summary>
        ///     The default arguments that are passed to Chrome
        /// </summary>
        private List<string> _defaultArguments;

        /// <summary>
        ///     The user to use when starting Chrome, when blank then Chrome is started under the code running user
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
        ///     A webproxy
        /// </summary>
        private WebProxy _webProxy;

        /// <summary>
        ///     The process id under which Chrome is running
        /// </summary>
        private Process _chromeProcess;

        /// <summary>
        ///     The portrange to use when starting Chrome
        /// </summary>
        private readonly PortRangeSettings _portRange;

        /// <summary>
        ///     Handles the communication with Chrome devtools
        /// </summary>
        private Communicator _communicator;

        /// <summary>
        ///     Keeps track is we already disposed our resources
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     When set then this folder is used for temporary files
        /// </summary>
        private DirectoryInfo _tempDirectory;

        /// <summary>
        ///     Returns the location of Chrome
        /// </summary>
        private string _chromeLocation;

        /// <summary>
        ///     <see cref="PreWrapper"/>
        /// </summary>
        private PreWrapper _preWrapper;

        /// <summary>
        ///     <see cref="ImageHelper"/>
        /// </summary>
        private ImageHelper _imageHelper;
        #endregion

        #region Properties
        /// <summary>
        ///     Returns the list with default arguments that are send to Chrome when starting
        /// </summary>
        public List<string> DefaultArguments => _defaultArguments;

        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        ///     Used to add the extension of text based files that needed to be wrapped in an HTML PRE
        ///     tag so that they can be opened by Chrome
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
        public List<string> PreWrapExtensions { get; set; }

        /// <summary>
        ///     When set to <c>true</c> then images are resized to fix the given <see cref="PageSettings.PaperWidth"/>
        /// </summary>
        public bool ResizeImages { get; set; }

        /// <summary>
        ///     When set then this directory is used to store temporary files.
        ///     For example files that are made in combination with <see cref="PreWrapExtensions"/>
        /// </summary>
        /// <exception cref="DirectoryNotFoundException">Raised when the given directory does not exists</exception>
        public string TempDirectory {
            get { return _tempDirectory.FullName; }
            set
            {
                _tempDirectory = new DirectoryInfo(value);
                if (!_tempDirectory.Exists)
                    throw new DirectoryNotFoundException($"The directory '{value}' does not exists");
            }
        }

        /// <summary>
        ///     Returns the path to Chrome, <c>null</c> will be returned if Chrome could not be found
        /// </summary>
        /// <returns></returns>
        public string ChromePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_chromeLocation))
                    return _chromeLocation;

                var currentPath =
                    // ReSharper disable once AssignNullToNotNullAttribute
                    new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath;

                // ReSharper disable once AssignNullToNotNullAttribute
                var chrome = Path.Combine(currentPath, "chrome.exe");

                if (File.Exists(chrome))
                {
                    _chromeLocation = currentPath;
                    return _chromeLocation;
                }

                var key = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome",
                    "InstallLocation", string.Empty);

                if (key != null)
                {
                    chrome = Path.Combine(key.ToString(), "chrome.exe");
                    if (File.Exists(chrome))
                    {
                        _chromeLocation = key.ToString();
                        return _chromeLocation;
                    }
                }

                key = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\U‌​ninstall\Google Chrome",
                    "InstallLocation", string.Empty);

                if (key != null)
                {
                    chrome = Path.Combine(key.ToString(), "chrome.exe");
                    if (File.Exists(chrome))
                    {
                        _chromeLocation = key.ToString();
                        return _chromeLocation;
                    }
                }

                return null;
            }
        }

        /// <summary>
        ///     <see cref="PreWrapper"/>
        /// </summary>
        private PreWrapper PreWrapper
        {
            get
            {
                if (_preWrapper != null)
                    return _preWrapper;

                _preWrapper = new PreWrapper(_tempDirectory);
                return _preWrapper;
            }
        }

        /// <summary>
        /// Retourneerd een <see cref="WebProxy"/> object
        /// </summary>
        private WebProxy WebProxy
        {
            get
            {
                if (_webProxy != null)
                    return _webProxy;

                try
                {
                    string userName = null;
                    string domain = null;

                    if (_userName.Contains("\\"))
                    {
                        domain = _userName.Split('\\')[0];
                        userName = _userName.Split('\\')[1];
                    }

                    if (string.IsNullOrWhiteSpace(_proxyServer))
                        return null;

                    NetworkCredential networkCredential = null;

                    var bypassList = _proxyBypassList.Split(';');

                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        networkCredential = !string.IsNullOrWhiteSpace(domain)
                            ? new NetworkCredential(userName, _password, domain)
                            : new NetworkCredential(userName, _password);
                    }

                    return networkCredential != null
                        ? _webProxy = new WebProxy(_proxyServer, true, bypassList.ToArray(), networkCredential)
                        : _webProxy = new WebProxy(_proxyServer, true, bypassList.ToArray());

                }
                catch (Exception exception)
                {
                    throw new Exception("Could not configure webproxy", exception);
                }
            }
        }
        
        /// <summary>
        ///     <see cref="ImageHelper"/>
        /// </summary>
        private ImageHelper ImageHelper
        {
            get
            {
                if (_imageHelper != null)
                    return _imageHelper;

                _imageHelper = new ImageHelper(_tempDirectory, _logStream, WebProxy) {InstanceId = InstanceId};
                _imageHelper.InstanceId = InstanceId;
                return _imageHelper;
            }
        }
        #endregion

        #region Constructor & Destructor
        /// <summary>
        ///     Creates this object and sets it's needed properties
        /// </summary>
        /// <param name="chromeExeFileName">When set then this has to be tThe full path to the chrome executable.
        ///      When not set then then the converter tries to find Chrome.exe by first looking in the path
        ///      where this library exists. After that it tries to find it by looking into the registry</param>
        /// <param name="portRange">
        ///     Force the converter to pick a port from the given range. When not set then the port range 9222
        ///     - 9322 is used
        /// </param>
        /// <param name="userProfile">
        ///     If set then this directory will be used to store a user profile.
        ///     Leave blank or set to <c>null</c> if you want to use the default Chrome userprofile location
        /// </param>
        /// <param name="logStream">When set then logging is written to this stream</param>
        /// <exception cref="FileNotFoundException">Raised when <see cref="chromeExeFileName" /> does not exists</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     Raised when the <paramref name="userProfile" /> directory is given but
        ///     does not exists
        /// </exception>
        public Converter(string chromeExeFileName = null,
                         PortRangeSettings portRange = null,
                         string userProfile = null,
                         Stream logStream = null)
        {
            PreWrapExtensions = new List<string>();
            _logStream = logStream;

            ResetArguments();

            if (string.IsNullOrWhiteSpace(chromeExeFileName))
                chromeExeFileName = Path.Combine(ChromePath, "chrome.exe");

            if (string.IsNullOrEmpty(chromeExeFileName))
                throw new FileNotFoundException("Could not find chrome.exe");

            _chromeExeFileName = chromeExeFileName;
            _portRange = portRange;

            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                var userProfileDirectory = new DirectoryInfo(userProfile);
                if (!userProfileDirectory.Exists)
                    throw new DirectoryNotFoundException(
                        $"The directory '{userProfileDirectory.FullName}' does not exists");

                SetDefaultArgument("--user-data-dir", $"\"{userProfileDirectory.FullName}\"");
            }
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~Converter()
        {
            Dispose();
        }
        #endregion

        #region GetUnusedPort
        /// <summary>
        ///     Returns an unused port
        /// </summary>
        /// <param name="portRange">The port range to use</param>
        /// <returns></returns>
        private static int GetUnusedPort(PortRangeSettings portRange)
        {
            var startPort = 9222;
            var endPort = 9322;

            if (portRange != null)
            {
                startPort = portRange.Start;
                endPort = portRange.End;
            }

            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();
            var usedPorts = tcpEndPoints.Select(p => p.Port).ToList();
            var unusedPort = Enumerable.Range(startPort, endPort - startPort)
                .FirstOrDefault(port => !usedPorts.Contains(port));
            return unusedPort;
        }
        #endregion

        #region IsChromeRunning
        /// <summary>
        /// Returns <c>true</c> when Chrome is running
        /// </summary>
        /// <returns></returns>
        private bool IsChromeRunning()
        {
            if (_chromeProcess == null)
                return false;

            _chromeProcess.Refresh();
            return !_chromeProcess.HasExited;
        }
        #endregion

        #region StartChromeHeadless
        /// <summary>
        ///     Start Chrome headless with the debugger set to the given port
        /// </summary>
        /// <remarks>
        ///     If Chrome is already running then this step is skipped
        /// </remarks>
        /// <exception cref="ChromeException"></exception>
        private void StartChromeHeadless()
        {
            if (IsChromeRunning())
                return;

            WriteToLog($"Starting Chrome from location {_chromeExeFileName}");

            using (var mutex = new Mutex(false, ChromeMutexName))
            {
                var i = 1;

                while (true)
                {

                    bool mutexAcquired;
                    try
                    {
                        mutexAcquired = mutex.WaitOne();
                    }
                    catch (AbandonedMutexException)
                    {
                        mutexAcquired = true;
                    }

                    if (!mutexAcquired)
                    {
                        WriteToLog("Could not acquire Chrome locking mutext");
                        return;
                    }

                    try
                    {
                        // Find a free remote debugging port
                        var port = GetUnusedPort(_portRange);
                        SetDefaultArgument("--remote-debugging-port", port.ToString());

                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = _chromeExeFileName,
                            Arguments = string.Join(" ", _defaultArguments),
                            CreateNoWindow = true,
                            RedirectStandardInput = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Normal
                        };

                        if (!string.IsNullOrWhiteSpace(_userName))
                        {
                            var userName = string.Empty;

                            if (_userName.Contains("\\"))
                                userName = _userName.Split('\\')[1];

                            var domain = _userName.Split('\\')[0];

                            WriteToLog($"Starting Chrome with user '{userName}' on domain '{domain}'");

                            processStartInfo.Domain = domain;
                            processStartInfo.UserName = userName;

                            var secureString = new SecureString();
                            foreach (var t in _password)
                                secureString.AppendChar(t);

                            processStartInfo.Password = secureString;
                        }

                        _chromeProcess = Process.Start(processStartInfo);
                        if (_chromeProcess == null)
                            throw new ChromeException("Could not start Chrome");

                        WriteToLog($"Chrome started on port {port}");

                        _chromeProcess.WaitForInputIdle();

                        if (_chromeProcess.HasExited)
                        {
                            if (i >= 5)
                            {
                                WriteToLog("Chrome process: " + _chromeExeFileName);
                                WriteToLog("Arguments used: " + string.Join(" ", _defaultArguments));
                                var exception =
                                    ExceptionHelpers.GetInnerException(
                                        Marshal.GetExceptionForHR(_chromeProcess.ExitCode));
                                WriteToLog("Exception: " + exception);
                                throw new ChromeException(
                                    $"Could not start Chrome - retried {i} times, " + exception);
                            }

                            Thread.Sleep(i * 50);
                            i++;
                            continue;
                        }

                        var devProtocol = new Uri($"http://localhost:{port}");
                        WriteToLog($"Connecting to dev protocol on url '{devProtocol}'");
                        _communicator = new Communicator(devProtocol);
                        WriteToLog("Connected to dev protocol");
                        break;
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
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
            if (directory != null && !directory.Exists)
                throw new DirectoryNotFoundException($"The path '{directory.FullName}' does not exists");
        }
        #endregion

        #region ResetArguments
        /// <summary>
        ///     Resets the <see cref="DefaultArguments" /> to their default settings
        /// </summary>
        private void ResetArguments()
        {
            _defaultArguments = new List<string>();
            SetDefaultArgument("--headless");
            SetDefaultArgument("--disable-gpu");
            SetDefaultArgument("--hide-scrollbars");
            SetDefaultArgument("--mute-audio");
            SetDefaultArgument("--disable-background-networking");
            SetDefaultArgument("--disable-background-timer-throttling");
            //SetDefaultArgument("--disable-client-side-phishing-detection");
            SetDefaultArgument("--disable-default-apps");
            SetDefaultArgument("--disable-extensions");
            SetDefaultArgument("--disable-hang-monitor");
            //SetDefaultArgument("--disable-popup-blocking");
            SetDefaultArgument("--disable-prompt-on-repost");
            SetDefaultArgument("--disable-sync");
            SetDefaultArgument("--disable-translate");
            SetDefaultArgument("--metrics-recording-only");
            SetDefaultArgument("--no-first-run");
            SetDefaultArgument("--disable-crash-reporter");
            SetDefaultArgument("--allow-insecure-localhost");
            SetDefaultArgument("--hide-scrollbars");
            SetDefaultArgument("--safebrowsing-disable-auto-update");
            SetWindowSize(WindowSize.HD_1366_768);
        }
        #endregion

        #region RemoveArgument
        /// <summary>
        ///     Removes the given <paramref name="argument" /> from <see cref="_defaultArguments" />
        /// </summary>
        /// <param name="argument"></param>
        // ReSharper disable once UnusedMember.Local
        private void RemoveArgument(string argument)
        {
            if (_defaultArguments.Contains(argument))
                _defaultArguments.Remove(argument);
        }
        #endregion

        #region SetProxyServer
        /// <summary>
        ///     Instructs Chrome to use the provided proxy server
        /// </summary>
        /// <param name="value"></param>
        /// <example>
        ///     &lt;scheme&gt;=&lt;uri&gt;[:&lt;port&gt;][;...] | &lt;uri&gt;[:&lt;port&gt;] | "direct://"
        ///     This tells Chrome to use a custom proxy configuration. You can specify a custom proxy configuration in three ways:
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
        ///     Set this parameter before starting Chrome
        /// </remarks>
        public void SetProxyServer(string value)
        {
            _proxyServer = value;
            SetDefaultArgument("--proxy-server", value);
        }
        #endregion

        #region SetProxyBypassList
        /// <summary>
        ///     This tells chrome to bypass any specified proxy for the given semi-colon-separated list of hosts.
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
        ///     Set this parameter before starting Chrome
        /// </remarks>
        public void SetProxyBypassList(string values)
        {
            _proxyBypassList = values;
            SetDefaultArgument("--proxy-bypass-list", values);
        }
        #endregion

        #region SetProxyPacUrl
        /// <summary>
        ///     This tells Chrome to use the PAC file at the specified URL.
        /// </summary>
        /// <param name="value"></param>
        /// <example>
        ///     "http://wpad/windows.pac"
        ///     will tell Chrome to resolve proxy information for URL requests using the windows.pac file.
        /// </example>
        /// <remarks>
        ///     Set this parameter before starting Chrome
        /// </remarks>
        public void SetProxyPacUrl(string value)
        {
            SetDefaultArgument("--proxy-pac-url", value);
        }
        #endregion

        #region SetUserAgent
        /// <summary>
        ///     This tells Chrome to use the given user-agent string
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>
        ///     Set this parameter before starting Chrome
        /// </remarks>
        public void SetUserAgent(string value)
        {
            SetDefaultArgument("--user-agent", value);
        }
        #endregion

        #region SetUser
        /// <summary>
        ///     Sets the user under which Chrome wil run. This is usefull if you are on a server and
        ///     the user under which the code runs doesn't have access to the internet.
        /// </summary>
        /// <param name="userName">The username with or without a domain name (e.g DOMAIN\USERNAME)</param>
        /// <param name="password">The password for the <paramref name="userName" /></param>
        /// <remarks>
        ///     Set this parameter before starting Chrome
        /// </remarks>
        public void SetUser(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }
        #endregion

        #region SetArgument
        /// <summary>
        ///     Add's an extra conversion argument to the <see cref="_defaultArguments" />
        /// </summary>
        /// <remarks>
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        /// <param name="argument"></param>
        private void SetDefaultArgument(string argument)
        {
            if (!_defaultArguments.Contains(argument, StringComparison.CurrentCultureIgnoreCase))
                _defaultArguments.Add(argument);
        }

        /// <summary>
        ///     Add's an extra conversion argument with value to the <see cref="_defaultArguments" />
        ///     or replaces it when it already exists
        /// </summary>
        /// <remarks>
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        /// <param name="argument"></param>
        /// <param name="value"></param>
        private void SetDefaultArgument(string argument, string value)
        {
            if (IsChromeRunning())
                throw new ChromeException($"Chrome is already running, you need to set the parameter '{argument}' before staring Chrome");


            for (var i = 0; i < _defaultArguments.Count; i++)
            {
                if (!_defaultArguments[i].StartsWith(argument + "=")) continue;
                _defaultArguments[i] = argument + $"=\"{value}\"";
                return;
            }

            _defaultArguments.Add(argument + $"=\"{value}\"");
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
        ///     Raised when <paramref name="width" /> or
        ///     <paramref name="height" /> is smaller then or zero
        /// </exception>
        public void SetWindowSize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));

            SetDefaultArgument("--window-size", width + "," + height);
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
                    SetDefaultArgument("--window-size", 800 + "," + 600);
                    break;
                case WindowSize.WSVGA:
                    SetDefaultArgument("--window-size", 1024 + "," + 600);
                    break;
                case WindowSize.XGA:
                    SetDefaultArgument("--window-size", 1024 + "," + 768);
                    break;
                case WindowSize.XGAPLUS:
                    SetDefaultArgument("--window-size", 1152 + "," + 864);
                    break;
                case WindowSize.WXGA_5_3:
                    SetDefaultArgument("--window-size", 1280 + "," + 768);
                    break;
                case WindowSize.WXGA_16_10:
                    SetDefaultArgument("--window-size", 1280 + "," + 800);
                    break;
                case WindowSize.SXGA:
                    SetDefaultArgument("--window-size", 1280 + "," + 1024);
                    break;
                case WindowSize.HD_1360_768:
                    SetDefaultArgument("--window-size", 1360 + "," + 768);
                    break;
                case WindowSize.HD_1366_768:
                    SetDefaultArgument("--window-size", 1366 + "," + 768);
                    break;
                case WindowSize.OTHER_1536_864:
                    SetDefaultArgument("--window-size", 1536 + "," + 864);
                    break;
                case WindowSize.HD_PLUS:
                    SetDefaultArgument("--window-size", 1600 + "," + 900);
                    break;
                case WindowSize.WSXGA_PLUS:
                    SetDefaultArgument("--window-size", 1680 + "," + 1050);
                    break;
                case WindowSize.FHD:
                    SetDefaultArgument("--window-size", 1920 + "," + 1080);
                    break;
                case WindowSize.WUXGA:
                    SetDefaultArgument("--window-size", 1920 + "," + 1200);
                    break;
                case WindowSize.OTHER_2560_1070:
                    SetDefaultArgument("--window-size", 2560 + "," + 1070);
                    break;
                case WindowSize.WQHD:
                    SetDefaultArgument("--window-size", 2560 + "," + 1440);
                    break;
                case WindowSize.OTHER_3440_1440:
                    SetDefaultArgument("--window-size", 3440 + "," + 1440);
                    break;
                case WindowSize._4K_UHD:
                    SetDefaultArgument("--window-size", 3840 + "," + 2160);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
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

            var managedObjects =
                new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={processId}").Get();

            if (managedObjects.Count > 0)
            {
                foreach (var managedObject in managedObjects)
                    KillProcessAndChildren(Convert.ToInt32(managedObject["ProcessID"]));
            }

            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
            }
            catch (Exception exception)
            {
                WriteToLog(exception.Message);
            }
        }
        #endregion
        
        #region ConvertToPdf
        /// <summary>
        ///     Converts the given <paramref name="inputUri" /> to PDF
        /// </summary>
        /// <param name="inputUri">The webpage to convert</param>
        /// <param name="outputFile">The output file</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="waitForNetworkIdle">Wait until all external sources are loaded</param>
        /// <param name="waitForWindowStatus">Wait until the javascript window.status has this value before
        ///     rendering the PDF</param>
        /// <param name="waitForWindowsStatusTimeout"></param>
        /// <returns>The filename with full path to the generated PDF</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void ConvertToPdf(Uri inputUri,
                                 string outputFile,
                                 PageSettings pageSettings,
                                 bool waitForNetworkIdle,
                                 string waitForWindowStatus = "",
                                 int waitForWindowsStatusTimeout = 60000)
        {
            CheckIfOutputFolderExists(outputFile);

            if (inputUri.IsFile && !File.Exists(inputUri.OriginalString))
                throw new FileNotFoundException($"The file '{inputUri.OriginalString}' does not exists");

            FileInfo preWrappedFile = null;

            try
            {
                if (inputUri.IsFile && CheckForPreWrap(inputUri.LocalPath, out var preWrapFile))
                {
                    inputUri = new Uri(preWrapFile);
                    preWrappedFile = new FileInfo(preWrapFile);
                }
                else if (ResizeImages)
                {
                    if (!ImageHelper.ValidateImages(inputUri, pageSettings, out var imageFile))
                        inputUri = new Uri(imageFile);
                }
                
                StartChromeHeadless();

                WriteToLog("Loading " + (inputUri.IsFile ? "file " + inputUri.OriginalString : "url " + inputUri) +
                           (waitForNetworkIdle ? " and waiting until all resources are loaded" : string.Empty));

                _communicator.NavigateTo(inputUri, waitForNetworkIdle);

                if (!string.IsNullOrWhiteSpace(waitForWindowStatus))
                {
                    WriteToLog($"Waiting for window.status '{waitForWindowStatus}' or a timeout of {waitForWindowsStatusTimeout} milliseconds");
                    var match = _communicator.WaitForWindowStatus(waitForWindowStatus, waitForWindowsStatusTimeout);
                    WriteToLog(!match ? "Waiting timed out" : $"Window status equaled {waitForWindowStatus}");
                }

                WriteToLog((inputUri.IsFile ? "File" : "Url") + " loaded");

                WriteToLog("Converting to PDF");
                var pdfFileName = Path.ChangeExtension(outputFile, ".pdf");
                _communicator.PrintToPdf(pageSettings).SaveToFile(pdfFileName);
                WriteToLog("Converted");
            }
            finally
            {
                if (preWrappedFile != null)
                {
                    WriteToLog("Deleting prewrapped file");
                    preWrappedFile.Delete();
                }
            }
        }

        ///// <summary>
        /////     Converts the given <paramref name="inputFile" /> to JPG
        ///// </summary>
        ///// <param name="inputFile">The inputfile to convert to PDF</param>
        ///// <param name="outputFile">The output file</param>
        ///// <param name="pageSettings"><see cref="PageSettings"/></param>
        ///// <returns>The filename with full path to the generated PNG</returns>
        ///// <exception cref="DirectoryNotFoundException"></exception>
        //public void ConvertToPng(string inputFile, string outputFile, PageSettings pageSettings)
        //{
        //    CheckIfOutputFolderExists(outputFile);
        //    _communicator.NavigateTo(new Uri("file://" + inputFile), TODO);
        //    SetDefaultArgument("--screenshot", Path.ChangeExtension(outputFile, ".png"));
        //}

        ///// <summary>
        /////     Converts the given <paramref name="inputUri" /> to JPG
        ///// </summary>
        ///// <param name="inputUri">The webpage to convert</param>
        ///// <param name="outputFile">The output file</param>
        ///// <returns>The filename with full path to the generated PNG</returns>
        ///// <exception cref="DirectoryNotFoundException"></exception>
        //public void ConvertToPng(Uri inputUri, string outputFile)
        //{
        //    CheckIfOutputFolderExists(outputFile);
        //    _communicator.NavigateTo(inputUri, TODO);
        //    SetDefaultArgument("--screenshot", Path.ChangeExtension(outputFile, ".png"));
        //}
        #endregion

        #region CheckForPreWrap
        /// <summary>
        ///     Checks if <see cref="PreWrapExtensions"/> is set and if the extension
        ///     is inside this list. When in the list then the file is wrapped
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        /// <returns></returns>
        private bool CheckForPreWrap(string inputFile, out string outputFile)
        {
            outputFile = inputFile;

            if (PreWrapExtensions.Count == 0)
                return false;

            var ext = Path.GetExtension(inputFile);

            if (!PreWrapExtensions.Contains(ext, StringComparison.InvariantCultureIgnoreCase))
                return false;

            WriteToLog($"Prewrapping file '{inputFile}' to '{outputFile}'");

            outputFile = PreWrapper.WrapFile(inputFile);
            return true;
        }
        #endregion

        #region WriteToLog
        /// <summary>
        ///     Writes a line and linefeed to the <see cref="_logStream" />
        /// </summary>
        /// <param name="message">The message to write</param>
        private void WriteToLog(string message)
        {
            if (_logStream == null) return;
            var line = DateTime.Now.ToString("s") + (InstanceId != null ? " - " + InstanceId : string.Empty) + " - " +
                       message + Environment.NewLine;
            var bytes = Encoding.UTF8.GetBytes(line);
            _logStream.Write(bytes, 0, bytes.Length);
            _logStream.Flush();
        }
        #endregion

        #region Dispose
        /// <summary>
        ///     Disposes the running <see cref="_chromeProcess" />
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_disposed) return;
                WriteToLog("Stopping Chrome");
                if (_chromeProcess == null) return;
                _chromeProcess.Refresh();
                if (_chromeProcess.HasExited) return;
                KillProcessAndChildren(_chromeProcess.Id);
                _chromeProcess = null;
                WriteToLog("Chrome stopped");
            }
            catch (Exception exception)
            {
                WriteToLog(exception.Message);
            }

            _disposed = true;
        }
        #endregion
    }
}