﻿ChromiumHtmlToPdf
==============

## What is ChromiumHtmlToPdf?

ChromiumHtmlToPdf is a 100% managed C# .NETStandard 2.0 library and .NET 8 console application (that also works on Linux and macOS) that can be used to convert HTML to PDF format with the use of Google Chromemium (Google Chrome and Microsoft Edge browser)

From version 4.0 and up the library is now fully async but you can still use it without this if you want.

## Why did I make this?

I needed a replacement for wkHtmlToPdf, a great tool but the project is archived on GitHub and no new features are added anymore, also it's not 100% compatible with HTML5.

## License Information

ChromiumHtmlToPdf is Copyright (C)2017-2025 Kees van Spelde (Magic-Sessions) and is licensed under the MIT license:

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

## Installing via NuGet

[![NuGet](https://img.shields.io/nuget/v/ChromeHtmlToPdf.svg?style=flat-square)](https://www.nuget.org/packages/ChromeHtmlToPdf)

The easiest way to install ChromiumHtmlToPdf is via NuGet (Yes I know the nuget package has another name, this is because there is already a package with the new name I used).

In Visual Studio's Package Manager Console, simply enter the following command:

    Install-Package ChromeHtmlToPdf

### Converting a file or url from code

```csharp
var pageSettings = new PageSettings()
using (var converter = new Converter())
{
    converter.ConvertToPdf(new Uri("http://www.google.nl"), @"c:\google.pdf", pageSettings);
}

// Show the PDF
System.Diagnostics.Process.Start(@"c:\google.pdf");
```

or if you want to do it the async way

```csharp
var pageSettings = new PageSettings()
using var converter = new Converter();
await converter.ConvertToPdfAsync(new Uri("http://www.google.nl"), @"c:\google.pdf", pageSettings);

// Show the PDF
System.Diagnostics.Process.Start(@"c:\google.pdf");
```

### Enable extra logging when having weird issues with Chrome or Edge

When Chromium exits unexpectedly it does this most of the time without giving a meaningfull error. 
When this is the case than try to turn on Chromium Debug logging to get extra information about why it crashes

```c#
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
public bool EnableChromiumLogging { get; set; }
```

### Converting from Internet Information Services (IIS)

- Download Google Chrome or Microsoft Edge portable and extract it
- Let your website run under the ApplicationPool identity
- Copy the files to the same location as where your project exists on the webserver
- Reference the ChromeHtmlToPdfLib.dll from your webproject
- Call the converter.ConverToPdf method from code

Thats it.

If you get strange errors when starting Google Chrome or Microsoft Edge than this is due to the account that is used to run your site. I had a simular problem and solved it by hosting ChromiumHtmlToPdf in a Windows service and making calls to it with a WCF service.

### Converting from the command line

```
ChromiumHtmlToPdfConsole --input https://www.google.com --output c:\google.pdf
```
![screenshot](https://github.com/Sicos1977/ChromiumHtmlToPdf/blob/master/console.png)

### Console app exit codes

0 = successful, 1 = an error occurred

Installing on Linux or macOS
============================

### Installing .NET

See this url about how to install .NET on Linux

https://docs.microsoft.com/en-us/dotnet/core/install/linux

And this url about how to install .NET on macOS

https://docs.microsoft.com/en-us/dotnet/core/install/macos

### Installing Chrome

See this url about how to install Chrome on Linux

https://support.google.com/chrome/a/answer/9025903?hl=en

And this url about how to install Chrome on macOS

https://support.google.com/chrome/a/answer/7550274?hl=en

### Example installing Chrome on Linux Ubuntu

```
wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | sudo apt-key add -

sudo sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list'

sudo apt-get update

sudo apt-get install google-chrome-stable

google-chrome --version

google-chrome --no-sandbox --user-data-dir
```

Pre compiled binaries
=====================

You can find pre compiled binaries for Windows, Linux and macOS over here

Latest version (.net 8)
---------------
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/4.4.0/ChromiumHtmlToPdf_4_4_0.zip

.NET 8.0 for the console app
---------------------------------
The console app needs .NET 8 to run, you can download this framework from here

https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Documentation
-------------

Example usage:
    ChromiumHtmlToPdf --input https://www.google.nl --output c:\google.pdf

| **Option**                       | **Description**                                                                                                                                                                                                                                                       |
|----------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `--input`                        | Required. The input content, URL, or file.                                                                                                                                                                                                                            |
| `--input-is-list`                | Indicates `--input` is a list of URLs/files. Use `--output` for the location of converted files. Use `|` to specify an output file (e.g., `inputfile.html|myoutputfile.pdf`). Defaults to input file name if no output file is provided.                              |
| `--output`                       | Required. The output file.                                                                                                                                                                                                                                           |
| `--browser`                      | Required. Specifies the browser to use (default: Chrome or Edge).                                                                                                                                                                                                   |
| `--landscape`                    | (Default: false) Sets paper orientation to landscape.                                                                                                                                                                                                               |
| `--display-headerfooter`         | (Default: false) Displays header and footer.                                                                                                                                                                                                                        |
| `--header-template`              | Specifies a custom HTML template for the header. Overrides all other --header-* options.                                                                                                                                                                            |
| `--footer-template`              | Specifies a custom HTML template for the footer. Overrides all other --footer-* options.                                                                                                                                                                            |
| `--header-left`                  | Text to print in the left corner of the header. |
| `--header-center`                | Text to print in the center of the header. |
| `--header-right`                 | Text to print in the right corner of the header. |
| `--header-font-name`             | The font name to use for the header. |
| `--header-font-size`             | The font size (in pt) to use for the header. |
| `--footer-left`                  | Text to print in the left corner of the footer. |
| `--footer-center`                | Text to print in the center of the footer. |
| `--footer-right`                 | Text to print in the right corner of the footer. |
| `--footer-font-name`             | The font name to use for the footer. |
| `--footer-font-size`             | The font size (in pt) to use for the footer. |
| `--print-background`             | (Default: false) Prints background graphics.                                                                                                                                                                                                                        |
| `--scale`                        | (Default: 1) Specifies the webpage rendering scale.                                                                                                                                                                                                                 |
| `--paper-format`                 | (Default: Letter) Specifies paper format, overriding `--paper-width` and `--paper-height`. Valid values: Letter, Legal, Tabloid, Ledger, A0-A6, FitPageToContent.                                                                                                 |
| `--paper-width`                  | (Default: 8.5) Sets paper width in inches.                                                                                                                                                                                                                          |
| `--paper-height`                 | (Default: 11) Sets paper height in inches.                                                                                                                                                                                                                          |
| `--no-margins`                   | Removes margins by enabling Chromium's `--no-margins` parameter.                                                                                                                                                                                                    |
| `--window-size`                  | (Default: HD_1366_768) Specifies window size, overriding `--window-width` and `--window-height`. Valid values: SVGA, WSVGA, XGA, WXGA, FHD, 4K_UHD, etc.                                                                                                           |
| `--window-width`                 | (Default: 1366) Specifies window width in pixels.                                                                                                                                                                                                                   |
| `--window-height`                | (Default: 768) Specifies window height in pixels.                                                                                                                                                                                                                   |
| `--user-agent`                   | Overrides default user-agent string for Chromium.                                                                                                                                                                                                                   |
| `--margin-top`                   | (Default: 0.4) Top margin in inches.                                                                                                                                                                                                                                |
| `--margin-bottom`                | (Default: 0.4) Bottom margin in inches.                                                                                                                                                                                                                             |
| `--margin-left`                  | (Default: 0.4) Left margin in inches.                                                                                                                                                                                                                               |
| `--margin-right`                 | (Default: 0.4) Right margin in inches.                                                                                                                                                                                                                              |
| `--pageranges`                   | Specifies pages to print (e.g., '1-5, 8, 11-13').                                                                                                                                                                                                                   |
| `--chromium-location`            | Specifies Chrome/Edge location. Defaults to executable folder or registry.                                                                                                                                                                                          |
| `--chromium-userprofile`         | Specifies location for Chromium user profile.                                                                                                                                                                                                                       |
| `--proxy-server`                 | Configures Chromium to use a custom proxy server.                                                                                                                                                                                                                   |
| `--proxy-bypass-list`            | Specifies hosts to bypass proxy. Requires `--proxy-server`. Format: `*.google.com;*foo.com;127.0.0.1:8080`.                                                                                                                                                          |
| `--proxy-pac-url`                | Specifies PAC file URL for proxy (e.g., `http://wpad/windows.pac`).                                                                                                                                                                                                  |
| `--user`                         | Runs browser under a specific user (used with `--password`).                                                                                                                                                                                                        |
| `--password`                     | Specifies password for `--user`.                                                                                                                                                                                                                                    |
| `--tempfolder`                   | Specifies folder for temporary files.                                                                                                                                                                                                                               |
| `--multi-threading`              | (Default: false) Enables multi-threading (requires `--input-is-list`).                                                                                                                                                                                              |
| `--max-concurrency-level`        | (Default: 0) Limits concurrency level for multi-threading.                                                                                                                                                                                                          |
| `--wait-for-window-status`       | Waits for `window.status` to match a string before conversion.                                                                                                                                                                                                      |
| `--wait-for-window-status-timeout`| (Default: 60000) Timeout for `--wait-for-window-status`.                                                                                                                                                                                                            |
| `--timeout`                      | Specifies timeout (ms) before aborting conversion.                                                                                                                                                                                                                  |
| `--media-load-timeout`           | Specifies timeout (ms) for media load after DOM content is loaded.                                                                                                                                                                                                  |
| `--pre-wrap-file-extensions`     | Wraps files in HTML `<PRE>` tags.                                                                                                                                                                                                                                   |
| `--encoding`                     | Specifies encoding for `--input` file.                                                                                                                                                                                                                              |
| `--image-resize`                 | (Default: false) Resizes images to fit page width.                                                                                                                                                                                                                  |
| `--image-rotate`                 | (Default: false) Rotates images per EXIF data.                                                                                                                                                                                                                      |
| `--image-load-timeout`           | (Default: 30000) Timeout for downloading images.                                                                                                                                                                                                                    |
| `--sanitize-html`                | (Default: false) Removes HTML elements that could lead to XSS.                                                                                                                                                                                                     |
| `--logfile`                      | Specifies log file (wildcards: `{PID}`, `{DATE}`, `{TIME}`).                                                                                                                                                                                                        |
| `--run-javascript`               | Runs JavaScript after loading webpage but before PDF conversion.                                                                                                                                                                                                   |
| `--url-blacklist`                | Blocks specified URLs (e.g., `*.google.com;*foo.com`).                                                                                                                                                                                                              |
| `--snapshot`                     | Saves webpage snapshot as `.mhtml` alongside `.pdf`.                                                                                                                                                                                                                |
| `--log-network-traffic`          | Enables logging of network traffic.                                                                                                                                                                                                                               |
| `--disk-cache-disabled`          | (Default: false) Disables disk cache.                                                                                                                                                                                                                              |
| `--disk-cache-directory`         | Specifies directory for disk cache.                                                                                                                                                                                                                                |
| `--disk-cache-size`              | Specifies size of disk cache (MB).                                                                                                                                                                                                                                 |
| `--web-socket-timeout`           | Specifies WebSocket timeout (ms).                                                                                                                                                                                                                                 |
| `--wait-for-network-idle`        | Waits until network is idle before conversion.                                                                                                                                                                                                                      |
| `--help`                         | Displays help information.                                                                                                                                                                                                                                         |
| `--version`                      | Displays version information.                                                                                                                                                                                                                                     |
| `--no-sandbox`                   | Never use a sandbox.                                                                                                                                                                                                                                     |
| `-enable-chromium-logging`       | Enables Chromium logging; The output will be saved to the file chrome_debug.log in Chrome's user data directory. Logs are overwritten each time you restart Chromium.                                                                                                                                                                                                                                     |
| `--disable-gpu`                   | Passes --disable-gpu to Chromium. This should be useful on common server hardware.                                                                                                                                                                                                                                     |
| `--ignore-certificate-errors`                   | Passes --ignore-certificate-errors to Chromium. Useful when generating from internal web server.                                                                                                                                                                                                                                     |
| `--disable-crash-reporter`                   | Passes --disable-crash-reporter and --no-crashpad to Chromium.                                                                                                                                                                                                                                     |

Older versions (.net 8)
--------------
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/4.3.2/ChromiumHtmlToPdf_4_3_2.zip

Older versions (.net core 3.1 - end of life)
--------------
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/4.2.11/ChromiumHtmlToPdf_4_2_11.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/4.1.2/ChromiumHtmlToPdf_v4_0_2.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/4.0.1/ChromiumHtmlToPdf_v4_0_1.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/3.0.0/ChromiumHtmlToPdf_v3_0_0.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.6.5/ChromeHtmltoPdf_265.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.6.4/ChromeHtmlToPDF_264.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.6.0/ChromeHtmlToPDF_260.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.5.33/ChromeHtmlToPdf_253.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.5.1/ChromeHtmlToPdf_251.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.2/ChromeHtmlToPdf_220.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.1.6/ChromeHtmlToPdf_216.zip
https://github.com/Sicos1977/ChromiumHtmlToPdf/releases/download/2.0.11/ChromeHtmlToPdf_211.zip

.NET Core 3.1 for the console app (end of life)
---------------------------------
The console app needs .NET Core 3.1 to run, you can download this framework from here

https://dotnet.microsoft.com/en-us/download/dotnet/3.1

Installing it from scoop the package manager
============================================

See this for more information about scoop --> https://scoop.sh/#/

Just run the command from any PowerShell window (thanks to https://github.com/arnos-stuff)

```
scoop install https://gist.githubusercontent.com/arnos-stuff/4f9b2d92d812b25d0ee8335c543cba78/raw/cfa861ab3078a20c69157ab45daf33f26005fd63/chrome-html-to-pdf.json
```

Logging
=======

From version 2.5.0 ChromiumHtmlToPdfLib uses the Microsoft ILogger interface (https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=dotnet-plat-ext-5.0). You can use any logging library that uses this interface.

ChromiumHtmlToPdfLib has some build in loggers that can be found in the ```ChromiumHtmlToPdfLib.Logger``` namespace.

For example

```csharp
var logger = !string.IsNullOrWhiteSpace(<some logfile>)
                ? new ChromiumHtmlToPdfLib.Loggers.Stream(File.OpenWrite(<some logfile>))
                : new ChromiumHtmlToPdfLib.Loggers.Console();
```

Setting a common Google Chrome or Microsoft Edge cache directory
================================================================

You can not share a cache directory between a Google Chrome or Microsoft Edge instances because the first instance that is using the cache directory will lock it for its own use. The most efficient way to make optimal use of a cache directory is to create one for each instance that you are running.

I'm using Google Chrome from a WCF service and used the class below to make optimal use of cache directories. The class will create an instance id that I use to create a cache directory for each running Chrome instance. When the instance shuts down the instance id is put back in a stack so that the next executing instance can use this directory again.

```csharp
public static class InstanceId
{
    #region Fields
    private static readonly ConcurrentStack<string> ConcurrentStack;
    #endregion

    #region Constructor
    static InstanceId()
    {
        ConcurrentStack = new ConcurrentStack<string>();

        for(var i = 100000; i > 0; i--)
            ConcurrentStack.Push(i.ToString().PadLeft(6, '0'));
    }
    #endregion

    #region Pop
    /// <summary>
    /// Returns an instance id and pops it from the <see cref="ConcurrentStack"/>
    /// </summary>
    /// <returns></returns>
    public static string Pop()
    {
        if (ConcurrentStack.TryPop(out var instanceId))
            return instanceId;

        throw new Exception("Instance id stack is empty");
    }
    #endregion

    #region Push
    /// <summary>
    /// Pushes the <paramref name="instanceId"/> back on top of the <see cref="ConcurrentStack"/>
    /// </summary>
    /// <param name="instanceId"></param>
    public static void Push(string instanceId)
    {
        ConcurrentStack.Push(instanceId);
    }
    #endregion
}
```

Using it in a Docker container
==============================

```
# Suppress an apt-key warning about standard out not being a terminal. Use in this script is safe.
ENV APT_KEY_DONT_WARN_ON_DANGEROUS_USAGE=DontWarn

# export DEBIAN_FRONTEND="noninteractive"
ENV DEBIAN_FRONTEND noninteractive

# Install deps + add Chrome Stable + purge all the things
RUN apt-get update && apt-get install -y \
	apt-transport-https \
	ca-certificates \
	curl \
	gnupg \
	--no-install-recommends \
	&& curl -sSL https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
	&& echo "deb [arch=amd64] https://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
	&& apt-get update && apt-get install -y \
	google-chrome-stable \
	--no-install-recommends \
	&& apt-get purge --auto-remove -y curl gnupg \
	&& rm -rf /var/lib/apt/lists/*

# Chrome Driver
RUN apt-get update && \
    apt-get install -y unzip && \
    wget https://chromedriver.storage.googleapis.com/2.31/chromedriver_linux64.zip && \
    unzip chromedriver_linux64.zip && \
    mv chromedriver /usr/bin && rm -f chromedriver_linux64.zip
```

See this issue for more information --> https://github.com/Sicos1977/ChromiumHtmlToPdf/issues/39

# Using this library on Linux or from a docker container

To make the library work the flag --no-sandbox will be set by default (on Windows this flag is not set). The library automaticly detect on which system you are running the code and sets the flag when needed. If for whatever reason you get a converting error then check if this flag is set and if not then add it manually.

```csharp
converter.AddChromiumArgument("--no-sandbox")
```

When Chrome crashes for unknown reasons in a docker container
=============================================================

On most desktop Linux distributions, the default /dev/shm partition is large enough. However, on many cloud providers using Docker containers (such as the Google App Engine Flexible Environment) or Heroku, the default /dev/shm size is appreciably smaller (64MB and 5MB, respectively). On these platforms it's impossible to change the size of /dev/shm, which makes using Chrome difficult or impossible. This is particularly an issue for those who want to take advantage of its new headless mode.

If it is not possible to change the partition size than add the flag `--disable-dev-shm-usage` to tell Chrome not to use this parition

```csharp
converter.AddChromiumArgument("--disable-dev-shm-usage")
```

Core Team
=========
    Sicos1977 (Kees van Spelde)

## Reporting Bugs

Have a bug or a feature request? [Please open a new issue](https://github.com/Sicos1977/ChromiumHtmlToPdf/issues).

Before opening a new issue, please search for existing issues to avoid submitting duplicates.
