## What is ChromeHtmlToPdf?

ChromeHtmlToPdf is a 100% managed C# .NET library and console application that can be used to convert HTML to PDF format with the use of Google Chrome

## License Information

ChromeHtmlToPdf is Copyright (C)2017 Kees van Spelde and is licensed under the MIT license:

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
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.

## Installing via NuGet

The easiest way to install ChromeHtmlToPdf is via NuGet.

In Visual Studio's Package Manager Console, simply enter the following command:

    Install-Package ChromeHtmlToPdf -Version 1.0.0 

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

### Converting from the command line

```csharp
ChromeHtmlToPdf.exe --input https://www.google.com --output c:\google.pdf
```
![screenshot](https://github.com/Sicos1977/ChromeHtmlToPdf/blob/master/console.png)

### Console app exit codes

0 = successful, 1 = an error occurred

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
