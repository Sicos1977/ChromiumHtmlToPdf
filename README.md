## What is ChromeHtmlToPdf?

ChromeHtmlToPdf is a 100% managed C# .NETStandard 2.0 library and .NET Core 3.0 console application that can be used to convert HTML to PDF format with the use of Google Chrome

## Why did I make this?

I needed a replacement for wkHtmlToPdf, a great tool but the activity on this project is low and it's not 100% compatible with HTML5.

## License Information

ChromeHtmlToPdf is Copyright (C)2017-2020 Kees van Spelde and is licensed under the MIT license:

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

The easiest way to install ChromeHtmlToPdf is via NuGet.

In Visual Studio's Package Manager Console, simply enter the following command:

    Install-Package ChromeHtmlToPdf 

### Converting a file or url from code

```csharp
using (var converter = new Converter())
{
    converter.ConvertToPdf(new Uri("http://www.google.nl"), @"c:\google.pdf");
}

// Show the PDF
System.Diagnostics.Process.Start(@"c:\google.pdf");
```

### Converting from Internet Information Services (IIS)

- Download Chrome portable and extract it
- Let your website run under the ApplicationPool identity
- Copy the files to the same location as where your project exists on the webserver
- Reference the ChromeHtmlToPdfLib.dll from your webproject
- Call the converter.ConverToPdf method from code

Thats it.

If you get strange errors when starting Chrome than this is due to the account that is used to run your site. I had a simular problem and solved it by hosting ChromeHtmlToPdf in a Windows service and making calls to it with a WCF service.

### Converting from the command line

```csharp
ChromeHtmlToPdf.exe --input https://www.google.com --output c:\google.pdf
```
![screenshot](https://github.com/Sicos1977/ChromeHtmlToPdf/blob/master/console.png)

### Console app exit codes

0 = successful, 1 = an error occurred

### Pre compiled binaries

You can find pre compiled binaries for Windows, Unix and macOS over here 

https://github.com/Sicos1977/ChromeHtmlToPdf/releases/download/2.0.11/ChromeHtmlToPdf_211.zip
https://github.com/Sicos1977/ChromeHtmlToPdf/releases/download/2.1.6/ChromeHtmlToPdf_216.zip

Core Team
=========
    Sicos1977 (Kees van Spelde)

Support
=======
If you like my work then please consider a donation as a thank you.

<a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=NS92EXB2RDPYA" target="_blank"><img src="https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif" /></a>

## Reporting Bugs

Have a bug or a feature request? [Please open a new issue](https://github.com/Sicos1977/ChromeHtmlToPdf/issues).

Before opening a new issue, please search for existing issues to avoid submitting duplicates.
