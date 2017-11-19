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

namespace ChromeHtmlToPdfLib
{
    public class Converter : IDisposable
    {
        #region Const
        private string ChromeMutexName = "ChromeHtmlToPdfMutex";
        #endregion

        #region Fields
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
        #endregion

        #region Properties
        /// <summary>
        ///     Returns the list with default arguments that are send to Chrome when starting
        /// </summary>
        public List<string> DefaultArguments => _defaultArguments;
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
            if (_chromeProcess != null)
            {
                _chromeProcess.Refresh();
                if (!_chromeProcess.HasExited)
                    return;
            }

            var i = 1;
            using (var mutex = new Mutex(false, ChromeMutexName))
            {
                while (true)
                {

                    Console.Write("Starting Chrome ... ");
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
                        Console.Write("could not acquire Chrome locking mutext" + Environment.NewLine);
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
                                userName = _userName.Split('\\')[0];

                            var domain = _userName.Split('\\')[1];

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

                        _chromeProcess.WaitForInputIdle();

                        if (_chromeProcess.HasExited)
                        {
                            if (i >= 5)
                                throw new ChromeException("Could not start Chrome");

                            Thread.Sleep(i * 50);
                            i++;
                            continue;
                        }

                        _communicator = new Communicator(new Uri($"http://localhost:{port}"));
                        Console.Write($"started on port {port}" + Environment.NewLine);
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
            SetDefaultArgument("--disable-client-side-phishing-detection");
            SetDefaultArgument("--disable-default-apps");
            SetDefaultArgument("--disable-extensions");
            SetDefaultArgument("--disable-hang-monitor");
            SetDefaultArgument("--disable-popup-blocking");
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
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        public void SetProxyServer(string value)
        {
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
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        public void SetProxyBypassList(List<string> values)
        {
            SetDefaultArgument("--proxy-bypass-list", string.Join(",", values));
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
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        public void SetProxyPacUrl(string value)
        {
            SetDefaultArgument("--proxy-pac-url", value);
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
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        public void SetUser(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }
        #endregion

        #region UseMobileScreen
        /// <summary>
        ///     Tells Chrome to simulate a mobile screen
        /// </summary>
        /// <param name="value">Set to <c>false</c> to remove the mobile screen setting</param>
        /// <remarks>
        ///     This is a one time only default setting which can not be changed when doing multiple conversions.
        ///     Set this before doing any conversions.
        /// </remarks>
        public void UseMobileScreen(bool value = true)
        {
            if (value)
                SetDefaultArgument("--user-agent",
                    "Mozilla/5.0 (Linux; U; Android 4.0.3; ko-kr; LG-L160L Build/IML74K) AppleWebkit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30");
            else
                RemoveArgument("--user-agent");
        }
        #endregion

        #region KillAllChromeProcesses
        /// <summary>
        ///     This will kill all running Chrome processes
        /// </summary>
        /// <returns>
        ///     Returns the output of this function
        /// </returns>
        public static string KillAllChromeProcesses()
        {
            var result = new StringBuilder();
            result.AppendLine("=== START KILLING Chrome PROCESSES ===");

            foreach (var process in Process.GetProcessesByName("chrome"))
                try
                {
                    result.AppendLine($"Killing Chrome process with id {process.Id}");
                    process.Kill();
                    result.AppendLine($"Killed Chrome process with id {process.Id}");
                }
                catch (Exception exception)
                {
                    result.AppendLine($"Could not kill Chrome process with id {process.Id}, error {exception.Message}");
                }

            result.AppendLine("=== END KILLING Chrome PROCESSES ===");
            return result.ToString();
        }
        #endregion

        #region Constructor & Destructor
        /// <summary>
        ///     Creates this object and sets it's needed properties
        /// </summary>
        /// <param name="chromeExeFileName">The full path to the chrome executable</param>
        /// <param name="portRange">
        ///     Force the converter to pick a port from the given range. When not set then the port range 9222
        ///     - 9322 is used
        /// </param>
        /// <param name="userProfile">
        ///     If set then this directory will be used to store a user profile.
        ///     Leave blank or set to <c>null</c> if you want to use the default Chrome userprofile location
        /// </param>
        /// <exception cref="FileNotFoundException">Raised when <see cref="chromeExeFileName" /> does not exists</exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     Raised when the <paramref name="userProfile" /> directory is given but
        ///     does not exists
        /// </exception>
        public Converter(string chromeExeFileName, PortRangeSettings portRange = null,
            string userProfile = null)
        {
            ResetArguments();

            var chrome = new FileInfo(chromeExeFileName);
            if (!chrome.Exists)
                throw new FileNotFoundException("Could not find chrome.exe");

            _chromeExeFileName = chromeExeFileName;
            _portRange = portRange;

            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                var userProfileDirectory = new DirectoryInfo(userProfile);
                if (!userProfileDirectory.Exists)
                    throw new DirectoryNotFoundException(
                        $"The directory '{userProfileDirectory.FullName}' does not exists");

                SetDefaultArgument("user-data-dir" , $"\"{userProfileDirectory.FullName}\"");
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

        #region ConvertToPdf
        /// <summary>
        ///     Converts the given <paramref name="inputUri" /> to PDF
        /// </summary>
        /// <param name="inputUri">The webpage to convert</param>
        /// <param name="outputFile">The output file</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="waitForNetworkIdle">Wait until all external sources are loaded</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void ConvertToPdf(Uri inputUri, string outputFile, PageSettings pageSettings, bool waitForNetworkIdle)
        {
            CheckIfOutputFolderExists(outputFile);

            var isFile = inputUri.Scheme == "file";

            if (isFile && !File.Exists(inputUri.OriginalString))
                throw new FileNotFoundException($"The file '{inputUri.OriginalString}' does not exists");

            StartChromeHeadless();

            Console.Write("Loading " + (isFile ? "file" : "url") + $" {inputUri} " + (waitForNetworkIdle ? "and wait until all resources are loaded" : string.Empty) + " ... ");
            _communicator.NavigateTo(inputUri, waitForNetworkIdle);
            Console.Write("loaded" + Environment.NewLine);

            Console.Write("Converting to PDF ... ");            
            _communicator.PrintToPdf(pageSettings).SaveToFile(outputFile);
            Console.Write("converted" + Environment.NewLine);
        }
        #endregion

        #region ConvertToPng
        ///// <summary>
        /////     Converts the given <paramref name="inputFile" /> to JPG
        ///// </summary>
        ///// <param name="inputFile">The inputfile to convert to PDF</param>
        ///// <param name="outputFile">The output file</param>
        ///// <param name="pageSettings"><see cref="PageSettings"/></param>
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
        ///// <exception cref="DirectoryNotFoundException"></exception>
        //public void ConvertToPng(Uri inputUri, string outputFile)
        //{
        //    CheckIfOutputFolderExists(outputFile);
        //    _communicator.NavigateTo(inputUri, TODO);
        //    SetDefaultArgument("--screenshot", Path.ChangeExtension(outputFile, ".png"));
        //}
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
                Console.Write("Stopping Chrome ... ");
                if (_chromeProcess == null) return;
                _chromeProcess.Refresh();
                if (_chromeProcess.HasExited) return;
                _chromeProcess.Kill();
                _chromeProcess = null;
                Console.Write("stopped" + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _disposed = true;
        }
        #endregion
    }
}