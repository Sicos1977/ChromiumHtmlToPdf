﻿//
// Options.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2025 Magic-Sessions. (www.magic-sessions.com)
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

using System.Collections.Generic;
using ChromiumHtmlToPdfLib.Enums;
using CommandLine;
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ChromiumHtmlToPdfConsole;

/// <summary>
///     The parameters that can be used when calling this application
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class Options
{
    #region Properties
    /// <summary>
    ///     The input url or file
    /// </summary>
    [Option("input", Required = true, HelpText = "The input content, url or file")]
    public string Input { get; set; } = null!;

    /// <summary>
    ///     A file with input urls and/or files
    /// </summary>
    [Option("input-is-list", Required = false,
        HelpText =
            "Tells this app that --input is a list of input urls and/or files. Use the --output parameter to " +
            "give a location where to write information about the converted files, e.g. c:\\myconvertedfiles." +
            "Use a | sign to supply an output file, e.g. inputfile.html|myoutputfile.pdf. When no outputfile" +
            "is given then the input file is used as the outputfile where the extension is replaced with .pdf")]
    public bool InputIsList { get; set; }

    /// <summary>
    ///     The output file
    /// </summary>
    [Option("output", Required = true, HelpText = "The output file")]
    public string Output { get; set; } = null!;

    /// <summary>
    ///     The browser to use (chrome (default) or edge
    /// </summary>
    [Option("browser", Required = true, HelpText = "The browser to use (chrome (default) or edge")]
    public string Browser { get; set; } = null!;

    /// <summary>
    ///     Paper orientation. Defaults to false.
    /// </summary>
    [Option("landscape", Default = false, Required = false, HelpText = "Paper orientation")]
    public bool Landscape { get; set; }

    /// <summary>
    ///     Display header and footer. Defaults to false.
    /// </summary>
    [Option("display-headerfooter", Default = false, Required = false, HelpText = "Display header and footer")]
    public bool DisplayHeaderFooter { get; set; }

    /// <summary>
    ///     Print background graphics. Defaults to false.
    /// </summary>
    [Option("print-background", Required = false, Default = false, HelpText = "Print background graphics")]
    public bool PrintBackground { get; set; }

    /// <summary>
    ///     Scale of the webpage rendering. Defaults to 1.
    /// </summary>
    [Option("scale", Default = 1.0, Required = false, HelpText = "Scale of the webpage rendering")]
    public double Scale { get; set; }

    /// <summary>
    ///     The paper size to use, when this option is set is will override <see cref="PaperWidth" /> and
    ///     <see cref="PaperHeight" />
    /// </summary>
    [Option("paper-format", Required = false, Default = PaperFormat.Letter,
        HelpText = "Paper format to use, when set then this will override --paper-width and --paper-height")]
    public PaperFormat PaperFormat { get; set; }

    /// <summary>
    ///     Paper width in inches. Defaults to 8.5 inches.
    /// </summary>
    [Option("paper-width", Default = 8.5, Required = false, HelpText = "Paper width in inches")]
    public double PaperWidth { get; set; }

    /// <summary>
    ///     Paper height in inches. Defaults to 11 inches.
    /// </summary>
    [Option("paper-height", Default = 11.0, Required = false, HelpText = "Paper height in inches")]
    public double PaperHeight { get; set; }

    /// <summary>
    ///     A file with input urls and/or files
    /// </summary>
    [Option("no-margins", Required = false, HelpText = "Tells this app that the parameter '--no-margins' needs to be set on Chromium")]
    public bool NoMargins { get; set; }

    /// <summary>
    ///     The widow size to use, when this option is set it will override <see cref="WindowWidth" /> and
    ///     <see cref="WindowHeight" />
    /// </summary>
    [Option("window-size", Required = false, Default = WindowSize.HD_1366_768,
        HelpText = "Window size to use, when set then this will override --window-width and --window-height")]
    public WindowSize WindowSize { get; set; }

    /// <summary>
    ///     Window width in pixel.
    /// </summary>
    [Option("window-width", Default = 1366, Required = false, HelpText = "Window width in pixels")]
    public int WindowWidth { get; set; }

    /// <summary>
    ///     Window height in pixels.
    /// </summary>
    [Option("window-height", Default = 768, Required = false, HelpText = "Window height in pixels")]
    public int WindowHeight { get; set; }

    /// <summary>
    ///     Use the given user-agent
    /// </summary>
    [Option("user-agent", Required = false,
        HelpText = "Let Chromium know that we want to use the given user-agent string instead of the default one")]
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Top margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    [Option("margin-top", Default = 0.4, Required = false, HelpText = "Top margin in inches")]
    public double MarginTop { get; set; }

    /// <summary>
    ///     Bottom margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    [Option("margin-bottom", Default = 0.4, Required = false, HelpText = "Top margin in inches")]
    public double MarginBottom { get; set; }

    /// <summary>
    ///     Left margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    [Option("margin-left", Default = 0.4, Required = false, HelpText = "Left margin in inches")]
    public double MarginLeft { get; set; }

    /// <summary>
    ///     Right margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    [Option("margin-right", Default = 0.4, Required = false, HelpText = "Right margin in inches")]
    public double MarginRight { get; set; }

    /// <summary>
    ///     Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which means print all pages.
    /// </summary>
    [Option("pageranges", Required = false, HelpText = "Paper ranges to print, e.g., '1-5, 8, 11-13'")]
    public string? PageRanges { get; set; }

    /// <summary>
    ///     The location for Chrome or Edge, when not set then the registry is accessed to get the needed information
    /// </summary>
    [Option("chromium-location", Required = false,
        HelpText =
            "The location for Chrome or Edge, when not set then this tool first looks inside the folder where " +
            "it is executed from if it can find Chromium (portable) otherwise the registry is accessed " +
            "to get the needed information")]
    public string? ChromiumLocation { get; set; }

    /// <summary>
    ///     The location for Chrome or Edge, when not set then the registry is accessed to get the needed information
    /// </summary>
    [Option("chromium-userprofile", Required = false,
        HelpText =
            "The location where Chromium can store it's user profile")]
    public string? ChromiumUserProfile { get; set; }

    /// <summary>
    ///     This tells Chromium to use a custom proxy configuration
    /// </summary>
    [Option("proxy-server", Required = false, HelpText = "This tells Chromium to use a custom proxy configuration")]
    public string? ProxyServer { get; set; }

    /// <summary>
    ///     This tells Chromium to bypass any specified proxy for the given semi-colon-separated list of hosts. This flag must be
    ///     used (or rather, only has an effect) in
    ///     tandem with --proxy-server. For example "*.google.com;*foo.com;127.0.0.1:8080"
    /// </summary>
    [Option("proxy-bypass-list", Required = false,
        HelpText =
            "This tells Chromium to bypass any specified proxy for the given semi-colon-separated list of hosts. " +
            "This flag must be used (or rather, only has an effect) in tandem with --proxy-server. " +
            "For example \"*.google.com;*foo.com;127.0.0.1:8080\""
    )]
    public string? ProxyByPassList { get; set; }

    /// <summary>
    ///     This tells Chromium to use the PAC file at the specified URL. For example "http://wpad/windows.pac"
    /// </summary>
    [Option("proxy-pac-url", Required = false,
        HelpText =
            "This tells Chromium to use the PAC file at the specified URL. For example \"http://wpad/windows.pac\"")]
    public string? ProxyPacUrl { get; set; }

    /// <summary>
    ///     Run Chrome or Edge under this user. This option is used in combination with "--password"
    /// </summary>
    [Option("user", Required = false,
        HelpText = "Run Chrome or Edge under this user. This option is used in combination with --password")]
    public string? User { get; set; }

    /// <summary>
    ///     The password needed for --user
    /// </summary>
    [Option("password", Required = false, HelpText = "The password needed for --user")]
    public string? Password { get; set; }

    /// <summary>
    ///     The password needed for --user
    /// </summary>
    [Option("tempfolder", Required = false, HelpText = "A folder where this tool can put temporary files")]
    public string? TempFolder { get; set; }

    /// <summary>
    ///     Use multi threading when converting. Only useful if the parameter --input-is-list is used
    /// </summary>
    [Option("multi-threading", Required = false, Default = false,
        HelpText = "Use multi threading when converting. Only useful if the parameter --input-is-list is used")]
    public bool UseMultiThreading { get; set; }

    /// <summary>
    ///     Limits the concurrency level when doing multi threading conversions. This property is only used when
    ///     --input-is-list and --multi-threading is set to <c>true</c>. When not set then the system decides about
    ///     how many threads will be used
    /// </summary>
    [Option("max-concurrency-level", Required = false, Default = 0,
        HelpText = "Limits the concurrency level when doing multi threading conversions. This parameter is only used when " +
                   "--input-is-list and --multi-threading is set to true. When not set then the system decides about " +
                   "how many threads will be used")]
    public int MaxConcurrencyLevel { get; set; }

    /// <summary>
    ///     Wait for the javascript window.status to equal the given string before starting PDF conversion
    /// </summary>
    [Option("wait-for-window-status", Required = false,
        HelpText = "Wait for the javascript window.status to equal the given string before starting PDF conversion")]
    public string? WaitForWindowStatus { get; set; }
    
    /// <summary>
    ///     The timeout when waiting for the parameter <see cref="WaitForWindowStatus"/>
    /// </summary>
    [Option("wait-for-window-status-timeout", Required = false, Default = 60000,
        HelpText = "The timeout when waiting for the parameter --wait-for-windows-status")]
    public int WaitForWindowStatusTimeOut { get; set; }
    
    /// <summary>
    ///     The timeout in milliseconds before this application aborts the conversion
    /// </summary>
    [Option("timeout", Required = false,
        HelpText = "The timeout in milliseconds before this application aborts the conversion")]
    public int? Timeout { get; set; }

    /// <summary>
    ///     The time to wait in milliseconds for media (stylesheets, images and sub frames) to load after the dom content has
    ///     been loaded. When the timeout is exceeded the tool will start the conversion.
    /// </summary>
    [Option("media-load-timeout", Required = false,
        HelpText = "The time to wait in milliseconds for media (stylesheets, images and sub frames) to load " +
                   "after the dom content has been loaded. When the timeout is exceeded the tool will start the conversion.")]
    public int? MediaLoadTimeout { get; set; }

    /// <summary>
    ///     The files to wrap in an HTML file with a &lt;PRE&gt; tag
    /// </summary> 
    [Option("pre-wrap-file-extensions", Required = false,
        HelpText = "The files to wrap in a HTML file with a <PRE> tag")]
    public IEnumerable<string>? PreWrapFileExtensions { get; set; }

    /// <summary>
    ///     The encoding that is used for the <see cref="Input"/> file
    /// </summary>
    [Option("encoding", Required = false,
        HelpText = "The encoding that is used for the --input file")]
    public string? Encoding { get; set; }

    /// <summary>
    ///     Resize images so that they fit the width of the page
    /// </summary>
    [Option("image-resize", Required = false, Default = false,
        HelpText = "Resize images so that they fit the width of the page")]
    public bool ImageResize { get; set; }
    
    /// <summary>
    ///     Rotate images according to the EXIF orientation information
    /// </summary>
    [Option("image-rotate", Required = false, Default = false,
        HelpText = "Rotate images according to the EXIF orientation information")]
    public bool ImageRotate { get; set; }

    /// <summary>
    ///     The timeout in milliseconds before this application aborts the downloading
    ///     of images when the option <see cref="ImageResize"/> and/or <see cref="ImageRotate"/>
    ///     is being used
    /// </summary>
    [Option("image-load-timeout", Required = false, Default = 30000,
        HelpText = "The timeout in milliseconds before this application aborts the downloading " +
                   "of images when the option --ImageResize and/or --ImageRotate is being used")]
    public int? ImageLoadTimeout { get; set; }

    /// <summary>
    ///     When set to <c>true</c> this will remove all HTML that can lead to XSS attacks
    /// </summary>
    [Option("sanitize-html", Required = false, Default = false,
        HelpText = "When set this will remove all HTML that can lead to XSS attacks")]

    public bool SanitizeHtml { get; set; }

    /// <summary>
    ///     When set then the logging gets written to this file instead of the console
    /// </summary>
    [Option("logfile", Required = false,
        HelpText = "When set then the logging gets written to this file instead of the console " +
                   "(Wildcards {PID}, {DATE}, {TIME})")]
    public string? LogFile { get; set; }

    /// <summary>
    ///     Runs the given javascript after the webpage has been loaded
    /// </summary>
    [Option("run-javascript", Required = false,
        HelpText = "Runs the given javascript after the webpage has been loaded and before it is converted to PDF")]
    public string? RunJavascript { get; set; }

    /// <summary>
    ///     This tells chrome to bypass any specified proxy for the given semi-colon-separated list of hosts. This flag must be
    ///     used (or rather, only has an effect) in
    ///     tandem with --proxy-server. For example "*.google.com;*foo.com;127.0.0.1:8080"
    /// </summary>
    [Option("url-blacklist", Required = false,
        HelpText =
            "This tells Chromium to blacklist certain urls by blocking them. " +
            "For example \"*.google.com;*foo.com;\""
    )]
    public string? UrlBlacklist { get; set; }

    /// <summary>
    ///     This will capture a snapshot of the webpage (before it is converted to PDF) and save this to disk with
    ///     the same name (but different extension .mhtml) that is selected for the --output parameter
    /// </summary>
    [Option("snapshot", Required = false,
        HelpText =
            "This will capture a snapshot of the webpage (before it is converted to PDF) and " +
            "save this to disk with the same name (but different extension .mhtml) that is " +
            "selected for the --output parameter, e.g. --output test.pdf --> test.mhtml"
    )]
    public bool Snapshot { get; set; }

    /// <summary>
    ///     This will capture a snapshot of the webpage (before it is converted to PDF) and save this to disk with
    ///     the same name (but different extension .mhtml) that is selected for the --output parameter
    /// </summary>
    [Option("log-network-traffic", Required = false, HelpText = "When enabled network traffic is also logged")]
    public bool LogNetworkTraffic { get; set; }

    /// <summary>
    ///     When set to <c>true</c> then caching is disabled
    /// </summary>
    [Option("disk-cache-disabled", Required = false, Default = false, HelpText = "When set to true then we won't use a disk cache")]
    public bool DiskCacheDisabled { get; set; }
    
    /// <summary>
    ///     When set then Chrome or Edge uses this directory for caching
    /// </summary>
    [Option("disk-cache-directory", Required = false, HelpText = "When set then Chrome or Edge uses this directory for caching")]
    public string? DiskCacheDirectory { get; set; }

    /// <summary>
    ///     When set then Chrome or Edge uses this directory for caching
    /// </summary>
    [Option("disk-cache-size", Required = false, HelpText = "The size for the '--disk-cache-directory' in megabytes")]
    public long? DiskCacheSize { get; set; }

    /// <summary>
    ///     The timeout in milliseconds to wait for a websocket connection (default 30000 milliseconds)
    /// </summary>
    [Option("web-socket-timeout", Required = false, HelpText = "The web socket timeout in milliseconds")]
    public int? WebSocketTimeout { get; set; }

    /// <summary>
    ///     By default, we wait for the Page.loadEventFired to determine that the page is loaded.
    ///     In most cases this works fine but when you have a page that is loading a lot of interactive
    ///     resources this can sometimes result in a blank page. To prevent this you can set this to <c>true</c>
    ///     but your page will load slower
    /// </summary>
    [Option("wait-for-network-idle", Required = false, HelpText = "Wait until the network is idle")]
    public bool WaitForNetworkIdle { get; set; }

    /// <summary>
    ///     By default, we detect if we are running Windows or Linux and use a sandbox when running on Windows.<br/>
    ///     When still don't want to use a sandbox on Windows then set this to <c>true</c>
    /// </summary>
    [Option("no-sandbox", Required = false, HelpText = "Never use a sandbox")]
    public bool NoSandbox { get; set; }

    /// <summary>
    ///     Enables Chromium logging;<br/>
    ///     - The output will be saved to the file <b>chrome_debug.log</b> in Chrome's user data directory<br/>
    ///     - Logs are overwritten each time you restart chrome<br/>
    /// </summary>
    /// <remarks>
    ///     If the environment variable CHROME_LOG_FILE is set, Chrome will write its debug log to its specified location.<br/>
    ///     Example: Setting CHROME_LOG_FILE to "chrome_debug.log" will cause the log file to be written to the Chrome process's<br/>
    ///     current working directory while setting it to "D:\chrome_debug.log" will write the log to the root of your computer's D: drive.
    /// </remarks>
    [Option("enable-chromium-logging", Required = false, HelpText = "Enables Chromium logging; The output will be saved to the file chrome_debug.log in Chrome's user data directory. Logs are overwritten each time you restart Chromium")]
    public bool EnableChromiumLogging { get; set; }

    /// <summary>
    ///     Passes --disable-gpu to Chromium. This should be useful on common server hardware.
    /// </summary>
    [Option("disable-gpu", Required = false, HelpText = "Disables GPU hardware acceleration.")]
    public bool DisableGpu { get; set; }

    /// <summary>
    ///     Passes --ignore-certificate-errors to Chromium. Useful when generating from internal web server.
    /// </summary>
    [Option("ignore-certificate-errors", Required = false, HelpText = "Ignores certificate-related errors.")]
    public bool IgnoreCertificateErrors { get; set; }

    /// <summary>
    ///     Passes --disable-crash-reporter and --no-crashpad to Chromium.
    /// </summary>
    [Option("disable-crash-reporter", Required = false, HelpText = "Passes --disable-crash-reporter and --no-crashpad to Chromium.")]
    public bool DisableCrashReporter { get; set; }
    #endregion
}
