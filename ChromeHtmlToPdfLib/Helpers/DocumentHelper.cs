//
// DocumentHelper.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2019 Magic-Sessions. (www.magic-sessions.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using ChromeHtmlToPdfLib.Settings;
using Ganss.XSS;
using Image = System.Drawing.Image;
// ReSharper disable ConvertToUsingDeclaration
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    ///     This class contains helper methods
    /// </summary>
    public class DocumentHelper
    {
        #region Fields
        /// <summary>
        ///     When set then logging is written to this stream
        /// </summary>
        private readonly Stream _logStream;

        /// <summary>
        ///     The temp folder
        /// </summary>
        private readonly DirectoryInfo _tempDirectory;

        /// <summary>
        ///     The web client to use when downloading from the Internet
        /// </summary>
        private WebClient _webClient;

        /// <summary>
        ///     The web proxy to use
        /// </summary>
        private readonly WebProxy _webProxy;

        /// <summary>
        ///     The timeout in milliseconds before this application aborts the downloading
        ///     of images
        /// </summary>
        private readonly int _timeout;
        #endregion

        #region Properties
        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public string InstanceId { get; set; }

        /// <summary>
        ///     The web client to use when downloading from the Internet
        /// </summary>
        private WebClient WebClient
        {
            get
            {
                if (_webClient != null)
                    return _webClient;

                _webClient = _webProxy != null
                    ? new WebClient {Proxy = _webProxy}
                    : new WebClient();

                return _webClient;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        ///     Makes this object and sets its needed properties
        /// </summary>
        /// <param name="tempDirectory">When set then this directory will be used for temporary files</param>
        /// <param name="webProxy">The web proxy to use when downloading</param>
        /// <param name="timeout"></param>
        /// <param name="logStream"></param>
        public DocumentHelper(DirectoryInfo tempDirectory,
            WebProxy webProxy,
            int? timeout,
            Stream logStream)
        {
            _tempDirectory = tempDirectory;
            _webProxy = webProxy;
            _timeout = timeout ?? 30000;
            _logStream = logStream;
        }
        #endregion

        #region ParseValue
        /// <summary>
        /// Parses the value from the given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private int ParseValue(string value)
        {
            value = value.Replace("px", string.Empty);
            value = value.Replace(" ", string.Empty);
            return int.TryParse(value, out var result) ? result : 0;
        }
        #endregion

        #region SanitizeHtml
        /// <summary>
        /// Sanitizes the HTML by removing all forbidden elements
        /// </summary>
        /// <param name="inputUri">The uri of the webpage</param>
        /// <param name="sanitizer"><see cref="HtmlSanitizer"/></param>
        /// <param name="outputUri">The outputUri when this method returns <c>false</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <returns></returns>
        public bool SanitizeHtml(
            ConvertUri inputUri,
            HtmlSanitizer sanitizer, 
            out ConvertUri outputUri)
        {
            outputUri = null;

            using (var webpage = inputUri.IsFile
                ? File.OpenRead(inputUri.OriginalString)
                : DownloadStream(inputUri))
            {

                var htmlChanged = false;
                var config = Configuration.Default.WithCss();
                var context = BrowsingContext.New(config);

                IDocument document;

                try
                {
                    // ReSharper disable AccessToDisposedClosure
                    document = inputUri.Encoding != null
                        ? context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString())).Result
                        : context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString())).Result;
                    // ReSharper restore AccessToDisposedClosure
                }
                catch (Exception exception)
                {
                    WriteToLog($"Exception occured in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }

                WriteToLog("Sanitizing HTML");

                if (sanitizer == null)
                    sanitizer = new HtmlSanitizer();

                sanitizer.FilterUrl += delegate(object sender, FilterUrlEventArgs args)
                {
                    if (args.OriginalUrl != args.SanitizedUrl)
                    {
                        WriteToLog($"URL sanitized from '{args.OriginalUrl}' to '{args.SanitizedUrl}'");
                        htmlChanged = true;
                    }
                };

                sanitizer.RemovingAtRule += delegate(object sender, RemovingAtRuleEventArgs args)
                {
                    WriteToLog($"Removing CSS at-rule '{args.Rule.CssText}' from tag '{args.Tag.TagName}'");
                    htmlChanged = true;
                };

                sanitizer.RemovingAttribute += delegate(object sender, RemovingAttributeEventArgs args)
                {
                    WriteToLog(
                        $"Removing attribute '{args.Attribute.Name}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
                    htmlChanged = true;
                };

                sanitizer.RemovingComment += delegate(object sender, RemovingCommentEventArgs args)
                {
                    WriteToLog($"Removing comment '{args.Comment.TextContent}'");
                    htmlChanged = true;
                };

                sanitizer.RemovingCssClass += delegate(object sender, RemovingCssClassEventArgs args)
                {
                    WriteToLog(
                        $"Removing CSS class '{args.CssClass}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
                    htmlChanged = true;
                };

                sanitizer.RemovingStyle += delegate(object sender, RemovingStyleEventArgs args)
                {
                    WriteToLog(
                        $"Removing style '{args.Style.Name}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
                    htmlChanged = true;
                };

                sanitizer.RemovingTag += delegate(object sender, RemovingTagEventArgs args)
                {
                    WriteToLog($"Removing tag '{args.Tag.TagName}', reason '{args.Reason}'");
                    htmlChanged = true;
                };

                sanitizer.SanitizeDom(document as IHtmlDocument);

                WriteToLog("HTML sanitized");

                if (!htmlChanged)
                    return false;

                var sanitizedOutputFile = GetTempFile(".htm");
                outputUri = new ConvertUri(sanitizedOutputFile, inputUri.Encoding);

                try
                {
                    WriteToLog($"Writing sanitized webpage to '{sanitizedOutputFile}'");

                    using (var fileStream =
                        new FileStream(sanitizedOutputFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        if (inputUri.Encoding != null)
                        {
                            using (var textWriter = new StreamWriter(fileStream, inputUri.Encoding))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());
                        }
                        else
                            using (var textWriter = new StreamWriter(fileStream))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());
                    }

                    WriteToLog("Sanitized webpage written");
                    return true;
                }
                catch (Exception exception)
                {
                    WriteToLog($"Could not write new html file '{sanitizedOutputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }
            }
        }
        #endregion

        #region FitPageToContent
        /// <summary>
        /// Sanitizes the HTML by removing all forbidden elements
        /// </summary>
        /// <param name="inputUri">The uri of the webpage</param>
        /// <param name="outputUri">The outputUri when this method returns <c>false</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <returns></returns>
        public bool FitPageToContent(ConvertUri inputUri, out ConvertUri outputUri)
        {
            outputUri = null;

            using (var webpage = inputUri.IsFile
                ? File.OpenRead(inputUri.OriginalString)
                : DownloadStream(inputUri))
            {
                var config = Configuration.Default.WithCss();
                var context = BrowsingContext.New(config);

                IDocument document;

                try
                {
                    // ReSharper disable AccessToDisposedClosure
                    document = inputUri.Encoding != null
                        ? context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString())).Result
                        : context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString())).Result;
                    // ReSharper restore AccessToDisposedClosure

                    var styleElement = new HtmlElement(document as Document, "style")
                    {
                        InnerHtml = "html, body " + Environment.NewLine +
                                    "{" + Environment.NewLine +
                                    "   width: fit-content;" + Environment.NewLine +
                                    "   height: fit-content;" + Environment.NewLine +
                                    "   margin: 0px;" + Environment.NewLine +
                                    "   padding: 0px;" + Environment.NewLine +
                                    "}" + Environment.NewLine

                    };

                    document.Head.AppendElement(styleElement);

                    var pageStyleElement = new HtmlElement(document as Document, "style")
                    {
                        Id = "pagestyle",
                        InnerHtml = "@page " + Environment.NewLine +
                                    "{ " + Environment.NewLine +
                                    "   size: 595px 842px ; " + Environment.NewLine +
                                    "   margin: 0px " + Environment.NewLine +
                                    "}" + Environment.NewLine

                    };

                    document.Head.AppendElement(pageStyleElement);
                    
                    var pageElement = new HtmlElement(document as Document, "script")
                    {
                        InnerHtml = "window.onload = function () {" + Environment.NewLine +
                                    "" + Environment.NewLine +
                                    "   var page = document.getElementsByTagName('html')[0];" + Environment.NewLine +
                                    "   var pageInfo = window.getComputedStyle(page);" + Environment.NewLine +
                                    "" + Environment.NewLine +
                                    "    var height = parseInt(pageInfo.height) + 10 + 'px';" +
                                    Environment.NewLine +
                                    "" + Environment.NewLine +
                                    "    var pageCss = '@page { size: ' + pageInfo.width + ' ' + height + '; margin: 0; }'" +
                                    Environment.NewLine +
                                    "    document.getElementById('pagestyle').innerHTML = pageCss;" + Environment.NewLine +
                                    "}" + Environment.NewLine
                    };

                    document.Body.AppendElement(pageElement);
                }
                catch (Exception exception)
                {
                    WriteToLog($"Exception occured in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }

                var outputFile = GetTempFile(".htm");
                outputUri = new ConvertUri(outputFile, inputUri.Encoding);

                try
                {
                    WriteToLog($"Writing changed webpage to '{outputFile}'");

                    using (var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        if (inputUri.Encoding != null)
                        {
                            using (var textWriter = new StreamWriter(fileStream, inputUri.Encoding))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());
                        }
                        else
                            using (var textWriter = new StreamWriter(fileStream))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());

                    }

                    WriteToLog("Changed webpage written");
                    return true;
                }
                catch (Exception exception)
                {
                    WriteToLog($"Could not write new html file '{outputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }
            }
        }
        #endregion

        #region ValidateImages
        /// <summary>
        /// Validates all images if they are rotated correctly (when <paramref name="rotate"/> is set
        /// to <c>true</c>) and fit on the given <paramref name="pageSettings"/>.
        /// If an image does need to be rotated or does not fit then a local copy is made of 
        /// the <paramref name="inputUri"/> file.
        /// </summary>
        /// <param name="inputUri">The uri of the webpage</param>
        /// <param name="resize">When set to <c>true</c> then an image is resized when needed</param>
        /// <param name="rotate">When set to <c>true</c> then the EXIF information of an
        ///     image is read and when needed the image is automatic rotated</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="outputUri">The outputUri when this method returns <c>true</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <param name="urlBlacklist">A list of URL's that need to be blocked (use * as a wildcard)</param>
        /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
        /// <exception cref="WebException">Raised when the webpage from <paramref name="inputUri"/> could not be downloaded</exception>
        public bool ValidateImages(
            ConvertUri inputUri,
            bool resize,
            bool rotate,
            PageSettings pageSettings,
            out ConvertUri outputUri,
            List<string> urlBlacklist = null)
        {
            outputUri = null;

            using (var webpage = inputUri.IsFile
                ? File.OpenRead(inputUri.OriginalString)
                : DownloadStream(inputUri))
            {
                var maxWidth = (pageSettings.PaperWidth - pageSettings.MarginLeft - pageSettings.MarginRight) * 96.0;
                var maxHeight = (pageSettings.PaperHeight - pageSettings.MarginTop - pageSettings.MarginBottom) * 96.0;

                string localDirectory = null;

                if (inputUri.IsFile)
                    localDirectory = Path.GetDirectoryName(inputUri.OriginalString);

                var htmlChanged = false;
                var config = Configuration.Default.WithCss();
                var context = BrowsingContext.New(config);

                IDocument document;

                try
                {
                    // ReSharper disable AccessToDisposedClosure
                    document = inputUri.Encoding != null
                        ? context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString())).Result
                        : context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString())).Result;
                    // ReSharper restore AccessToDisposedClosure
                }
                catch (Exception exception)
                {
                    WriteToLog($"Exception occured in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }

                WriteToLog("Validating all images if they need to be rotated and if they fit the page");
                var unchangedImages = new List<IHtmlImageElement>();
                var absoluteUri = inputUri.AbsoluteUri.Substring(0, inputUri.AbsoluteUri.LastIndexOf('/') + 1);

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
                foreach (var htmlImage in document.Images)
                {
                    var imageChanged = false;

                    if (string.IsNullOrWhiteSpace(htmlImage.Source))
                    {
                        WriteToLog($"HTML image tag '{htmlImage.TagName}' has no image source '{htmlImage.Source}'");
                        continue;
                    }

                    Image image = null;
                    var source = htmlImage.Source.Contains("?")
                        ? htmlImage.Source.Split('?')[0]
                        : htmlImage.Source;

                    if (!RegularExpression.IsRegExMatch(urlBlacklist, source, out var matchedPattern) ||
                        source.StartsWith(absoluteUri, StringComparison.InvariantCultureIgnoreCase))
                    {
                        WriteToLog($"The url '{source}' has been allowed");
                    }
                    else
                    {
                        WriteToLog($"The url '{source}' has been blocked by url blacklist pattern '{matchedPattern}'");
                        continue;
                    }

                    var extension = Path.GetExtension(FileManager.RemoveInvalidFileNameChars(source));

                    var fileName = GetTempFile(extension);

                    try
                    {
                        // The local width and height attributes always go before css width and height
                        var width = htmlImage.DisplayWidth;
                        var height = htmlImage.DisplayHeight;

                       
                        if (rotate)
                        {
                            image = GetImage(htmlImage.Source, localDirectory);

                            if (image == null) continue;

                            if (RotateImageByExifOrientationData(image))
                            {
                                htmlImage.DisplayWidth = image.Width;
                                htmlImage.DisplayHeight = image.Height;
                                WriteToLog($"Image rotated and saved to location '{fileName}'");
                                image.Save(fileName);
                                htmlImage.DisplayWidth = image.Width;
                                htmlImage.DisplayHeight = image.Height;
                                htmlImage.SetStyle(string.Empty);
                                htmlImage.Source = new Uri(fileName).ToString();
                                htmlChanged = true;
                                imageChanged = true;
                            }

                            width = image.Width;
                            height = image.Height;
                        }

                        if (resize)
                        {
                            if (height == 0 && width == 0)
                            {
                                var style = context.Current.GetComputedStyle(htmlImage);
                                if (style != null)
                                {
                                    width = ParseValue(style.GetPropertyValue("width"));
                                    height = ParseValue(style.GetPropertyValue("height"));
                                }
                            }

                            // If we don't know the image size then get if from the image itself
                            if (width <= 0 || height <= 0)
                            {
                                if (image == null)
                                    image = GetImage(htmlImage.Source, localDirectory);

                                if (image == null) continue;
                                width = image.Width;
                                height = image.Height;
                            }

                            if (width > maxWidth || height > maxHeight)
                            {
                                // If we did not load the image already then load it

                                if (image == null)
                                    image = GetImage(htmlImage.Source, localDirectory);

                                if (image == null) continue;

                                ScaleImage(image, (int) maxWidth, out var newWidth, out var newHeight);
                                WriteToLog($"Image rescaled to width {newWidth} and height {newHeight}");
                                htmlImage.DisplayWidth = newWidth;
                                htmlImage.DisplayHeight = newHeight;
                                htmlImage.SetStyle(string.Empty);
                                htmlChanged = true;
                            }
                        }
                    }
                    finally
                    {
                        image?.Dispose();
                    }

                    if (!imageChanged)
                        unchangedImages.Add(htmlImage);
                }
                
                if (!htmlChanged)
                    return false;

                foreach (var unchangedImage in unchangedImages)
                {
                    using (var image = GetImage(unchangedImage.Source, localDirectory))
                    {
                        if (image == null)
                        {
                            WriteToLog($"Could not load unchanged image from location '{unchangedImage.Source}'");
                            continue;
                        }

                        var extension = Path.GetExtension(unchangedImage.Source.Contains("?")
                            ? unchangedImage.Source.Split('?')[0]
                            : unchangedImage.Source);
                        var fileName = GetTempFile(extension);

                        WriteToLog($"Unchanged image saved to location '{fileName}'");
                        image.Save(fileName);
                        unchangedImage.Source = new Uri(fileName).ToString();
                    }
                }

                var outputFile = GetTempFile(".htm");
                outputUri = new ConvertUri(outputFile, inputUri.Encoding);

                try
                {
                    WriteToLog($"Writing changed webpage to '{outputFile}'");

                    using (var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        if (inputUri.Encoding != null)
                        {
                            using (var textWriter = new StreamWriter(fileStream, inputUri.Encoding))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());
                        }
                        else
                            using (var textWriter = new StreamWriter(fileStream))
                                document.ToHtml(textWriter, new HtmlMarkupFormatter());

                    }

                    WriteToLog("Changed webpage written");

                    return true;
                }
                catch (Exception exception)
                {
                    WriteToLog($"Could not write new html file '{outputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
                    return false;
                }
            }
        }
        #endregion

        #region GetImage
        /// <summary>
        /// Returns the <see cref="Image"/> for the given <paramref name="imageSource"/>
        /// </summary>
        /// <param name="imageSource"></param>
        /// <param name="localDirectory"></param>
        /// <returns></returns>
        private Image GetImage(string imageSource, string localDirectory) 
        {
            if (imageSource.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase))
            {
                WriteToLog("Decoding image from base64 string");

                try
                {
                    var base64Data = Regex.Match(imageSource, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                    var binaryData = Convert.FromBase64String(base64Data);

                    using (var stream = new MemoryStream(binaryData))
                    {
                        var image = Image.FromStream(stream);
                        WriteToLog("Image decoded");
                        return image;
                    }
                }
                catch (Exception exception)
                {
                    WriteToLog($"Error decoding image: {ExceptionHelpers.GetInnerException(exception)}");
                    return null;
                }
            }

            try
            {
                WriteToLog($"Getting image from uri '{imageSource}'");

                var imageUri = new Uri(imageSource);

                if (imageUri.IsFile)
                {
                    var fileName = imageUri.LocalPath;

                    if (!File.Exists(fileName))
                        fileName = Path.Combine(localDirectory, Path.GetFileName(imageUri.LocalPath));

                    if (File.Exists(fileName))
                    {
                        var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        return Image.FromStream(fileStream, true, false);
                    }
                }

                switch (imageUri.Scheme)
                {
                    case "https":
                    case "http":
                        using (var webStream = WebClient.OpenReadTaskAsync(imageUri).Timeout(_timeout).GetAwaiter().GetResult())
                        {
                            if (webStream != null)
                                return Image.FromStream(webStream, true, false);
                        }
                        break;

                    default:
                        WriteToLog($"Unsupported scheme {imageUri.Scheme} to get image");
                        return null;
                }
            }
            catch (Exception exception)
            {
                WriteToLog("Getting image failed with exception: " + ExceptionHelpers.GetInnerException(exception));
            }

            return null;
        }
        #endregion

        #region ScaleImage
        /// <summary>
        /// Scales the image to the preferred max width
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxWidth">The maximum width</param>
        /// <param name="newWidth">Returns the new width</param>
        /// <param name="newHeight">Return the new height</param>
        /// <returns></returns>
        private void ScaleImage(Image image, double maxWidth, out int newWidth, out int newHeight)
        {
            var ratio = maxWidth / image.Width;
            newWidth = (int)(image.Width * ratio);
            if (newWidth == 0) newWidth = 1;
            newHeight = (int)(image.Height * ratio);
            if (newHeight == 0) newHeight = 1;
        }
        #endregion

        #region RotateImageByExifOrientationData
        /// <summary>
        /// Rotate the given bitmap according to Exif Orientation data
        /// </summary>
        /// <param name="image">source image</param>
        /// <param name="updateExifData">Set it to <c>true</c> to update image Exif data after rotation 
        /// (default is <c>false</c>)</param>
        /// <returns>Returns <c>true</c> when the image is rotated</returns>
        private bool RotateImageByExifOrientationData(Image image, bool updateExifData = true)
        {
            const int orientationId = 0x0112;
            if (!((IList) image.PropertyIdList).Contains(orientationId)) return false;

            var item = image.GetPropertyItem(orientationId);
            RotateFlipType rotateFlipType;
            WriteToLog("Checking image rotation");

            switch (item.Value[0])
            {
                case 2:
                    rotateFlipType = RotateFlipType.RotateNoneFlipX;
                    break;
                case 3:
                    rotateFlipType = RotateFlipType.Rotate180FlipNone;
                    break;
                case 4:
                    rotateFlipType = RotateFlipType.Rotate180FlipX;
                    break;
                case 5:
                    rotateFlipType = RotateFlipType.Rotate90FlipX;
                    break;
                case 6:
                    rotateFlipType = RotateFlipType.Rotate90FlipNone;
                    break;
                case 7:
                    rotateFlipType = RotateFlipType.Rotate270FlipX;
                    break;
                case 8:
                    rotateFlipType = RotateFlipType.Rotate270FlipNone;
                    break;
                default:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
            }

            if (rotateFlipType == RotateFlipType.RotateNoneFlipNone)
                return false;

            image.RotateFlip(rotateFlipType);
            WriteToLog($"Image rotated with {rotateFlipType}");
                
            // Remove Exif orientation tag (if requested)
            if (updateExifData) image.RemovePropertyItem(orientationId);
            return true;
        }       
        #endregion

        #region DownloadStream
        /// <summary>
        ///     Downloads from the given <paramref name="sourceUri"/> and it as a string
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <returns></returns>
        private Stream DownloadStream(Uri sourceUri)
        {
            try
            {
                WriteToLog($"Downloading from uri '{sourceUri}'");
                var result = WebClient.OpenReadTaskAsync(sourceUri).Timeout(_timeout).GetAwaiter().GetResult();
                WriteToLog("Downloaded");
                return result;
            }
            catch (Exception exception)
            {
                WriteToLog("Downloading failed with exception: " + ExceptionHelpers.GetInnerException(exception));
                return null;
            }
        }
        #endregion

        #region GetTempFile
        /// <summary>
        /// Returns a temporary file
        /// </summary>
        /// <param name="extension">The extension (e.g. .html)</param>
        /// <returns></returns>
        private string GetTempFile(string extension)
        {
            var tempFile = Guid.NewGuid() + extension;
            return Path.Combine(_tempDirectory?.FullName ?? Path.GetTempPath(), tempFile);
        }
        #endregion

        #region WriteToLog
        /// <summary>
        ///     Writes a line and linefeed to the <see cref="_logStream" />
        /// </summary>
        /// <param name="message">The message to write</param>
        private void WriteToLog(string message)
        {
            try
            {
                if (_logStream == null || !_logStream.CanWrite) return;
                var line = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") +
                           (InstanceId != null ? " - " + InstanceId : string.Empty) + " - " +
                           message + Environment.NewLine;
                var bytes = Encoding.UTF8.GetBytes(line);
                _logStream.Write(bytes, 0, bytes.Length);
                _logStream.Flush();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }
        #endregion
    }
}
