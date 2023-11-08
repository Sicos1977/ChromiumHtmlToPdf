//
// DocumentHelper.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2023 Magic-Sessions. (www.magic-sessions.com)
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
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Network;
using ChromiumHtmlToPdfLib.Loggers;
using ChromiumHtmlToPdfLib.Settings;
using Ganss.Xss;
using Svg;
using File = System.IO.File;
using Stream = System.IO.Stream;

// ReSharper disable AccessToDisposedClosure

namespace ChromiumHtmlToPdfLib.Helpers;

/// <summary>
///     This class contains helper methods
/// </summary>
internal class DocumentHelper
{
    #region Fields
    /// <summary>
    ///     <see cref="Loggers.Logger"/>
    /// </summary>
    private readonly Logger _logger;

    /// <summary>
    ///     The temp folder
    /// </summary>
    private readonly DirectoryInfo _tempDirectory;

    /// <summary>
    ///     When <c>true</c> then caching is enabled on the <see cref="WebClient" />
    /// </summary>
    private readonly bool _useCache;

    /// <summary>
    ///     The cache directory when <see cref="_useCache"/> is set to <c>true</c>, otherwise <c>null</c>
    /// </summary>
    private readonly FileSystemInfo _cacheDirectory;

    /// <summary>
    ///     The cache size when <see cref="_cacheSize"/> is set to <c>true</c>, otherwise <c>null</c>
    /// </summary>
    private readonly long _cacheSize;

    /// <summary>
    ///     The web proxy to use when downloading
    /// </summary>
    private readonly IWebProxy _webProxy;

    /// <summary>
    ///     When set then this timeout is used for loading images, <c>null</c> when no timeout is needed
    /// </summary>
    private readonly int _imageLoadTimeout;

    /// <summary>
    ///     Used when mediaTimeout is set
    /// </summary>
    private Stopwatch _stopwatch;
    #endregion

    #region Properties
    private FileCacheHandler FileCacheHandler
    {
        get
        {
            // ReSharper disable once InconsistentlySynchronizedField
            var httpClientHandler = new FileCacheHandler(_useCache, _cacheDirectory, _cacheSize, _logger)
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            if (_webProxy != null)
                httpClientHandler.Proxy = _webProxy;

            return httpClientHandler;
        }
    }

    /// <summary>
    ///     Used by Angle Sharp to download the webpage
    /// </summary>
    private IConfiguration Config =>
        Configuration.Default
            .With(new HttpClientRequester(new HttpClient(FileCacheHandler)))
            .WithTemporaryCookies()
            .WithDefaultLoader()
            .WithCss();

    /// <summary>
    ///     Returns the time left when <see cref="_imageLoadTimeout" /> has been set
    /// </summary>
    private int TimeLeft
    {
        get
        {
            if (_imageLoadTimeout == 0)
                return 0;

            var result = _imageLoadTimeout - _stopwatch.ElapsedMilliseconds;

            if (result <= 0)
            {
                _stopwatch.Stop();
                result = 0;
            }

            return (int)result;
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets its needed properties
    /// </summary>
    /// <param name="tempDirectory">When set then this directory will be used for temporary files</param>
    /// <param name="useCache">When <c>true</c> then caching is enabled on the <see cref="WebClient" /></param>
    /// <param name="cacheDirectory">The cache directory when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="cacheSize">The cache size when <paramref name="useCache"/> is set to <c>true</c>, otherwise <c>null</c></param>
    /// <param name="webProxy">The web proxy to use when downloading</param>
    /// <param name="imageLoadTimeout">When set then this timeout is used for loading images, <c>null</c> when no timeout is needed</param>
    /// <param name="logger"><see cref="Logger"/></param>
    public DocumentHelper(
        DirectoryInfo tempDirectory,
        bool useCache,
        FileSystemInfo cacheDirectory, 
        long cacheSize,
        IWebProxy webProxy,
        int? imageLoadTimeout,
        Logger logger)
    {
        _tempDirectory = tempDirectory;
        _useCache = useCache;
        _cacheDirectory = cacheDirectory;
        _cacheSize = cacheSize;
        _webProxy = webProxy;
        _logger = logger;

        if (useCache)
            _logger?.WriteToLog($"Setting cache directory to '{cacheDirectory.FullName}' with a size of {FileManager.GetFileSizeString(cacheSize, CultureInfo.InvariantCulture)}");

        if (!imageLoadTimeout.HasValue) return;
        _imageLoadTimeout = imageLoadTimeout.Value;
        _logger?.WriteToLog($"Setting image load timeout to '{_imageLoadTimeout}' milliseconds");
    }

    ~DocumentHelper()
    {
        // Just in case
        _stopwatch?.Stop();
    }
    #endregion

    #region ParseValue
    /// <summary>
    ///     Parses the value from the given value
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

    #region SanitizeHtmlAsync
    /// <summary>
    ///     Sanitizes the HTML by removing all forbidden elements
    /// </summary>
    /// <param name="inputUri">The uri of the webpage</param>
    /// <param name="sanitizer">
    ///     <see cref="HtmlSanitizer" />
    /// </param>
    /// <param name="safeUrls">A list of safe URL's</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns></returns>
    public async Task<SanitizeHtmlResult> SanitizeHtmlAsync(ConvertUri inputUri, HtmlSanitizer sanitizer, List<string> safeUrls, CancellationToken cancellationToken)
    {
#if (NETSTANDARD2_0)
        using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#else
        await using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#endif
        var htmlChanged = false;
        var context = BrowsingContext.New(Config);

        IDocument document;

        try
        {
            document = inputUri.Encoding != null
                ? await context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false)
                : await context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Exception occurred in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
            return new SanitizeHtmlResult(false, inputUri, safeUrls);
        }
        
        _logger?.WriteToLog("Sanitizing HTML");

        sanitizer ??= new HtmlSanitizer();

        void OnSanitizerOnFilterUrl(object _, FilterUrlEventArgs args)
        {
            if (args.OriginalUrl == args.SanitizedUrl) return;
            _logger?.WriteToLog($"URL sanitized from '{args.OriginalUrl}' to '{args.SanitizedUrl}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingAtRule(object _, RemovingAtRuleEventArgs args)
        {
            _logger?.WriteToLog($"Removing CSS at-rule '{args.Rule.CssText}' from tag '{args.Tag.TagName}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingAttribute(object _, RemovingAttributeEventArgs args)
        {
            _logger?.WriteToLog($"Removing attribute '{args.Attribute.Name}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingComment(object _, RemovingCommentEventArgs args)
        {
            _logger?.WriteToLog($"Removing comment '{args.Comment.TextContent}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingCssClass(object _, RemovingCssClassEventArgs args)
        {
            _logger?.WriteToLog($"Removing CSS class '{args.CssClass}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingStyle(object _, RemovingStyleEventArgs args)
        {
            _logger?.WriteToLog($"Removing style '{args.Style.Name}' from tag '{args.Tag.TagName}', reason '{args.Reason}'");
            htmlChanged = true;
        }

        void OnSanitizerOnRemovingTag(object _, RemovingTagEventArgs args)
        {
            _logger?.WriteToLog($"Removing tag '{args.Tag.TagName}', reason '{args.Reason}'");
            htmlChanged = true;
        }

        try
        {
            sanitizer.FilterUrl += OnSanitizerOnFilterUrl;
            sanitizer.RemovingAtRule += OnSanitizerOnRemovingAtRule;
            sanitizer.RemovingAttribute += OnSanitizerOnRemovingAttribute;
            sanitizer.RemovingComment += OnSanitizerOnRemovingComment;
            sanitizer.RemovingCssClass += OnSanitizerOnRemovingCssClass;
            sanitizer.RemovingStyle += OnSanitizerOnRemovingStyle;
            sanitizer.RemovingTag += OnSanitizerOnRemovingTag;

            if (document is not IHtmlDocument htmlDocument)
                throw new InvalidCastException("Could not cast document to IHtmlDocument");

            sanitizer.SanitizeDom(htmlDocument);

            if (!htmlChanged)
            {
                _logger?.WriteToLog("HTML did not need any sanitization");
                return new SanitizeHtmlResult(false, inputUri, safeUrls);
            }
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Exception occurred in HtmlSanitizer: {ExceptionHelpers.GetInnerException(exception)}");
            return new SanitizeHtmlResult(false, inputUri, safeUrls);
        }
        finally
        {
            sanitizer.FilterUrl -= OnSanitizerOnFilterUrl;
            sanitizer.RemovingAtRule -= OnSanitizerOnRemovingAtRule;
            sanitizer.RemovingAttribute -= OnSanitizerOnRemovingAttribute;
            sanitizer.RemovingComment -= OnSanitizerOnRemovingComment;
            sanitizer.RemovingCssClass -= OnSanitizerOnRemovingCssClass;
            sanitizer.RemovingStyle -= OnSanitizerOnRemovingStyle;
            sanitizer.RemovingTag -= OnSanitizerOnRemovingTag;
        }

        _logger?.WriteToLog("HTML sanitized");

        var sanitizedOutputFile = GetTempFile(".htm");
        var outputUri = new ConvertUri(sanitizedOutputFile, inputUri.Encoding);
        var url = outputUri.ToString();
        _logger?.WriteToLog($"Adding url '{url}' to the safe url list");
        safeUrls.Add(url);

        try
        {
            if (document.BaseUrl != null && document.BaseUrl.Scheme.StartsWith("file"))
            {
                //var images = document.DocumentElement.Descendants().OfType<IHtmlImageElement>()
                //    .Where(x => x.NodeType == NodeType.Element);

                var images = document.DocumentElement.Descendents()
                    .Where(x => x.NodeType == NodeType.Element)
                    .OfType<IHtmlImageElement>();

                foreach (var image in images)
                {
                    var src = image.Source;

                    if (src == null) continue;

                    if (src.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                        src.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase)) continue;

                    _logger?.WriteToLog($"Updating image source to '{src}' and adding it to the safe url list");
                    safeUrls.Add(src);
                    image.Source = src;
                }
            }

            _logger?.WriteToLog($"Writing sanitized webpage to '{sanitizedOutputFile}'");

#if (NETSTANDARD2_0)
            using var fileStream = new FileStream(sanitizedOutputFile, FileMode.CreateNew, FileAccess.Write);
#else
            await using var fileStream = new FileStream(sanitizedOutputFile, FileMode.CreateNew, FileAccess.Write);
#endif
            if (inputUri.Encoding != null)
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#else
                await using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }
            else
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream);
#else
                await using var textWriter = new StreamWriter(fileStream);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }

            _logger?.WriteToLog("Sanitized webpage written");
            return new SanitizeHtmlResult(true, outputUri, safeUrls);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Could not write new html file '{sanitizedOutputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
            return new SanitizeHtmlResult(false, inputUri, safeUrls);
        }
    }
    #endregion

    #region ResetTimeout
    internal void ResetTimeout()
    {
        if (_imageLoadTimeout == 0)
            return;

        _stopwatch?.Stop();
        _stopwatch = Stopwatch.StartNew();
    }
    #endregion

    #region FitPageToContentAsync
    /// <summary>
    ///     Opens the webpage and adds code to make it fit the page
    /// </summary>
    /// <param name="inputUri">The uri of the webpage</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="FitPageToContentResult"/></returns>
    public async Task<FitPageToContentResult> FitPageToContentAsync(ConvertUri inputUri, CancellationToken cancellationToken)
    {
#if (NETSTANDARD2_0)
        using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#else
        await using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#endif

        using var context = BrowsingContext.New(Config);
        IDocument document;

        try
        {
            // ReSharper disable AccessToDisposedClosure
            document = inputUri.Encoding != null
                ? await context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false)
                : await context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false);
            // ReSharper restore AccessToDisposedClosure

            if (document is not Document htmlElementDocument)
                throw new InvalidCastException("Could not cast document to Document");
            
            var styleElement = new HtmlElement(htmlElementDocument, "style")
            {
                InnerHtml = "html, body " + Environment.NewLine +
                            "{" + Environment.NewLine +
                            "   width: fit-content;" + Environment.NewLine +
                            "   height: fit-content;" + Environment.NewLine +
                            "   margin: 0px;" + Environment.NewLine +
                            "   padding: 0px;" + Environment.NewLine +
                            "}" + Environment.NewLine
            };


            if (document.Head != null)
            {
                document.Head.AppendElement(styleElement);

                var pageStyleElement = new HtmlElement(htmlElementDocument, "style")
                {
                    Id = "pagestyle",
                    InnerHtml = "@page " + Environment.NewLine +
                                "{ " + Environment.NewLine +
                                "   size: 595px 842px ; " + Environment.NewLine +
                                "   margin: 0px " + Environment.NewLine +
                                "}" + Environment.NewLine
                };

                document.Head.AppendElement(pageStyleElement);
            }

            var pageElement = new HtmlElement(htmlElementDocument, "script")
            {
                InnerHtml = "window.onload = function () {" + Environment.NewLine +
                            "" + Environment.NewLine +
                            "   var page = document.getElementsByTagName('html')[0];" + Environment.NewLine +
                            "   var pageInfo = window.getComputedStyle(page);" + Environment.NewLine + "" +
                            Environment.NewLine +
                            "   var height = parseInt(pageInfo.height) + 10 + 'px';" + Environment.NewLine +
                            "" + Environment.NewLine +
                            "   var pageCss = '@page { size: ' + pageInfo.width + ' ' + height + '; margin: 0; }'" +
                            Environment.NewLine +
                            "   document.getElementById('pagestyle').innerHTML = pageCss;" + Environment.NewLine +
                            "}" + Environment.NewLine
            };

            document.Body?.AppendElement(pageElement);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Exception occurred in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
            return new FitPageToContentResult(false, inputUri);
        }

        var outputFile = GetTempFile(".htm");
        var outputUri = new ConvertUri(outputFile, inputUri.Encoding);

        try
        {
            _logger?.WriteToLog($"Writing changed webpage to '{outputFile}'");

#if (NETSTANDARD2_0)
            using var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
#else
            await using var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
#endif            
            if (inputUri.Encoding != null)
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#else
                await using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }
            else
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream);
#else
                await using var textWriter = new StreamWriter(fileStream);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }

            _logger?.WriteToLog("Changed webpage written");
            return new FitPageToContentResult(true, outputUri);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Could not write new html file '{outputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
            return new FitPageToContentResult(false, inputUri);
        }
    }
    #endregion

    #region ValidateImagesAsync
    /// <summary>
    ///     Validates all images if they are rotated correctly (when <paramref name="rotate" /> is set
    ///     to <c>true</c>) and fit on the given <paramref name="pageSettings" />.
    ///     If an image does need to be rotated or does not fit then a local copy is made of
    ///     the <paramref name="inputUri" /> file.
    /// </summary>
    /// <param name="inputUri">The uri of the webpage</param>
    /// <param name="resize">When set to <c>true</c> then an image is resized when needed</param>
    /// <param name="rotate">
    ///     When set to <c>true</c> then the EXIF information of an
    ///     image is read and when needed the image is automatic rotated
    /// </param>
    /// <param name="pageSettings"><see cref="PageSettings" /></param>
    /// <param name="safeUrls">A list with URL's that are safe to load</param>
    /// <param name="urlBlacklist">A list of URL's that need to be blocked (use * as a wildcard)</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
    /// <exception cref="WebException">Raised when the webpage from <paramref name="inputUri" /> could not be downloaded</exception>
    public async Task<ValidateImagesResult> ValidateImagesAsync(
        ConvertUri inputUri,
        bool resize,
        bool rotate,
        PageSettings pageSettings,
        List<string> safeUrls,
        List<string> urlBlacklist,
        CancellationToken cancellationToken)
    {
        using var graphics = Graphics.FromHwnd(IntPtr.Zero);
#if (NETSTANDARD2_0)
        using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#else
        await using var webpage = inputUri.IsFile ? OpenFileStream(inputUri.OriginalString) : await OpenDownloadStream(inputUri).ConfigureAwait(false);
#endif
        
        _logger?.WriteToLog($"DPI settings for image, x: '{graphics.DpiX}' and y: '{graphics.DpiY}'");
        var maxWidth = (pageSettings.PaperWidth - pageSettings.MarginLeft - pageSettings.MarginRight) * graphics.DpiX;
        var maxHeight = (pageSettings.PaperHeight - pageSettings.MarginTop - pageSettings.MarginBottom) * graphics.DpiY;

        string localDirectory = null;

        if (inputUri.IsFile)
            localDirectory = Path.GetDirectoryName(inputUri.OriginalString);

        var htmlChanged = false;
        var context = BrowsingContext.New(Config);
        IDocument document;

        try
        {
            document = inputUri.Encoding != null
                // ReSharper disable AccessToDisposedClosure
                ? await context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}").Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false)
                : await context.OpenAsync(m => m.Content(webpage).Address(inputUri.ToString()), cancel: cancellationToken).ConfigureAwait(false);
                // ReSharper restore AccessToDisposedClosure
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Exception occurred in AngleSharp: {ExceptionHelpers.GetInnerException(exception)}");
            return new ValidateImagesResult(false, inputUri);
        }

        _logger?.WriteToLog("Validating all images if they need to be rotated and if they fit the page");
        var unchangedImages = new List<IHtmlImageElement>();
        var absoluteUri = inputUri.AbsoluteUri.Substring(0, inputUri.AbsoluteUri.LastIndexOf('/') + 1);

        // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
        foreach (var htmlImage in document.Images)
        {
            var imageChanged = false;

            if (string.IsNullOrWhiteSpace(htmlImage.Source))
            {
                _logger?.WriteToLog($"HTML image tag '{htmlImage.TagName}' has no image source '{htmlImage.Source}'");
                continue;
            }

            Image image = null;

            var source = htmlImage.Source.Contains("?") ? htmlImage.Source.Split('?')[0] : htmlImage.Source;
            var isSafeUrl = safeUrls.Contains(source);
            var isAbsoluteUri = source.StartsWith(absoluteUri, StringComparison.InvariantCultureIgnoreCase);

            if (!RegularExpression.IsRegExMatch(urlBlacklist, source, out var matchedPattern) || isAbsoluteUri || isSafeUrl)
            {
                if (isAbsoluteUri)
                    _logger?.WriteToLog($"The url '{source}' has been allowed because it start with the absolute uri '{absoluteUri}'");
                else if (isSafeUrl)
                    _logger?.WriteToLog($"The url '{source}' has been allowed because it is on the safe url list");
                else
                    _logger?.WriteToLog($"The url '{source}' has been allowed because it did not match anything on the url blacklist");
            }
            else
            {
                _logger?.WriteToLog($"The url '{source}' has been blocked by url blacklist pattern '{matchedPattern}'");
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
                    image = await GetImageAsync(htmlImage.Source, localDirectory).ConfigureAwait(false);

                    if (image == null) continue;

                    if (RotateImageByExifOrientationData(image))
                    {
                        htmlImage.DisplayWidth = image.Width;
                        htmlImage.DisplayHeight = image.Height;
                        _logger?.WriteToLog($"Image rotated and saved to location '{fileName}'");
                        image.Save(fileName);
                        htmlImage.DisplayWidth = image.Width;
                        htmlImage.DisplayHeight = image.Height;
                        htmlImage.SetStyle(string.Empty);
                        var newSrc = new Uri(fileName).ToString();
                        _logger?.WriteToLog($"Adding url '{newSrc}' to the safe url list");
                        safeUrls.Add(newSrc);
                        htmlImage.Source = newSrc;
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
                        ICssStyleDeclaration style = null;

                        try
                        {
                            style = context.Current.GetComputedStyle(htmlImage);
                        }
                        catch (Exception exception)
                        {
                            _logger?.WriteToLog($"Could not get computed style from html image, exception: '{exception.Message}'");
                        }

                        if (style != null)
                        {
                            width = ParseValue(style.GetPropertyValue("width"));
                            height = ParseValue(style.GetPropertyValue("height"));
                        }
                    }

                    // If we don't know the image size then get if from the image itself
                    if (width <= 0 || height <= 0)
                    {
                        image ??= await GetImageAsync(htmlImage.Source, localDirectory).ConfigureAwait(false);
                        if (image == null) continue;
                        width = image.Width;
                        height = image.Height;
                    }

                    if (width > maxWidth || height > maxHeight)
                    {
                        // If we did not load the image already then load it

                        image ??= await GetImageAsync(htmlImage.Source, localDirectory).ConfigureAwait(false);
                        if (image == null) continue;
                        
                        var ratio = maxWidth / image.Width;

                        _logger?.WriteToLog($"Rescaling image with current width {image.Width}, height {image.Height} and ratio {ratio}");

                        var newWidth = (int)(image.Width * ratio);
                        if (newWidth == 0) newWidth = 1;
                        var newHeight = (int)(image.Height * ratio);
                        if (newHeight == 0) newHeight = 1;

                        _logger?.WriteToLog($"Image rescaled to new width {newWidth} and height {newHeight}");
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
            return new ValidateImagesResult(false, inputUri);

        foreach (var unchangedImage in unchangedImages)
        {
            using var image = await GetImageAsync(unchangedImage.Source, localDirectory).ConfigureAwait(false);
            
            if (image == null)
            {
                _logger?.WriteToLog($"Could not load unchanged image from location '{unchangedImage.Source}'");
                continue;
            }

            var extension = Path.GetExtension(unchangedImage.Source.Contains("?")
                ? unchangedImage.Source.Split('?')[0]
                : unchangedImage.Source);
            var fileName = GetTempFile(extension);

            try
            {
                image.Save(fileName);
                var newSrc = new Uri(fileName).ToString();
                safeUrls.Add(newSrc);
                unchangedImage.Source = newSrc;
                _logger?.WriteToLog($"Unchanged image saved to location '{fileName}'");
            }
            catch (Exception exception)
            {
                _logger?.WriteToLog($"Could not write unchanged image because of an exception: {ExceptionHelpers.GetInnerException(exception)}");
            }
        }

        var outputFile = GetTempFile(".htm");
        var outputUri = new ConvertUri(outputFile, inputUri.Encoding);
        safeUrls.Add(outputUri.ToString());

        try
        {
            _logger?.WriteToLog($"Writing changed webpage to '{outputFile}'");

#if (NETSTANDARD2_0)
            using var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
#else
            await using var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write);
#endif      
            
            if (inputUri.Encoding != null)
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#else
                await using var textWriter = new StreamWriter(fileStream, inputUri.Encoding);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }
            else
            {
#if (NETSTANDARD2_0)
                using var textWriter = new StreamWriter(fileStream);
#else
                await using var textWriter = new StreamWriter(fileStream);
#endif
                document.ToHtml(textWriter, new HtmlMarkupFormatter());
            }
            
            _logger?.WriteToLog("Changed webpage written");

            return new ValidateImagesResult(true, outputUri);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Could not write new html file '{outputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
            return new ValidateImagesResult(false, inputUri);
        }
    }
    #endregion

    #region GetImageAsync
    /// <summary>
    ///     Returns the <see cref="Image" /> for the given <paramref name="imageSource" />
    /// </summary>
    /// <param name="imageSource"></param>
    /// <param name="localDirectory"></param>
    /// <returns></returns>
    private async Task<Image> GetImageAsync(string imageSource, string localDirectory)
    {
        if (imageSource.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger?.WriteToLog("Decoding image from base64 string");

            try
            {
                var base64Data = Regex.Match(imageSource, "data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
                var binaryData = Convert.FromBase64String(base64Data);

                using var stream = new MemoryStream(binaryData);
                var image = Image.FromStream(stream);
                _logger?.WriteToLog("Image decoded");
                return image;
            }
            catch (Exception exception)
            {
                _logger?.WriteToLog($"Error decoding image: {ExceptionHelpers.GetInnerException(exception)}");
                return null;
            }
        }

        try
        {
            _logger?.WriteToLog($"Getting image from url '{imageSource}'");

            var imageUri = new Uri(imageSource);

            if (imageUri.IsFile)
            {
                var fileName = imageUri.LocalPath;

                if (!File.Exists(fileName))
                    fileName = Path.Combine(localDirectory, Path.GetFileName(imageUri.LocalPath));

                if (File.Exists(fileName))
                {
                    var fileStream = OpenFileStream(fileName);
                    return fileStream == null ? null : Image.FromStream(fileStream, true, false);
                }
            }

            switch (imageUri.Scheme)
            {
                case "https":
                case "http":
                {
#if (NETSTANDARD2_0)
                    using var webStream = await OpenDownloadStream(imageUri, true).ConfigureAwait(false);
#else
                    await using var webStream = await OpenDownloadStream(imageUri, true).ConfigureAwait(false);
#endif
                    var extension = Path.GetExtension(imageUri.AbsolutePath);
                    if (extension.ToLowerInvariant() != ".svg") 
                        return Image.FromStream(webStream, true, false);
                    
                    var svgDocument = SvgDocument.Open<SvgDocument>(webStream);
                    return svgDocument.Draw();
                }

                case "file":
                    _logger?.WriteToLog("Ignoring local file");
                    break;

                default:
                    _logger?.WriteToLog($"Unsupported scheme {imageUri.Scheme} to get image");
                    return null;
            }
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Getting image failed with exception: {ExceptionHelpers.GetInnerException(exception)}");
        }

        return null;
    }
    #endregion

    #region RotateImageByExifOrientationData
    /// <summary>
    ///     Rotate the given bitmap according to Exif Orientation data
    /// </summary>
    /// <param name="image">source image</param>
    /// <param name="updateExifData">
    ///     Set it to <c>true</c> to update image Exif data after rotation
    ///     (default is <c>false</c>)
    /// </param>
    /// <returns>Returns <c>true</c> when the image is rotated</returns>
    private bool RotateImageByExifOrientationData(Image image, bool updateExifData = true)
    {
        const int orientationId = 0x0112;
        if (!((IList)image.PropertyIdList).Contains(orientationId)) return false;

        var item = image.GetPropertyItem(orientationId);

        if (item?.Value == null)
        {
            _logger?.WriteToLog("Could not get orientation information from exif");
            return false;
        }

        RotateFlipType rotateFlipType;
        _logger?.WriteToLog("Checking image rotation");

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
        _logger?.WriteToLog($"Image rotated with {rotateFlipType}");

        // Remove Exif orientation tag (if requested)
        if (updateExifData) image.RemovePropertyItem(orientationId);
        return true;
    }
    #endregion

    #region OpenFileStream
    /// <summary>
    ///     Open the file to the given <paramref name="fileName" /> and returns it as a stream
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private Stream OpenFileStream(string fileName)
    {
        try
        {
            _logger?.WriteToLog($"Opening stream to file '{fileName}'");
            var result = File.OpenRead(fileName);
            return result;
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Opening stream failed with exception: {ExceptionHelpers.GetInnerException(exception)}");
            return null;
        }
    }
    #endregion

    #region OpenDownloadStream
    /// <summary>
    ///     Opens a download stream to the given <paramref name="sourceUri" />
    /// </summary>
    /// <param name="sourceUri"></param>
    /// <param name="checkTimeout"></param>
    /// <returns></returns>
    private async Task<Stream> OpenDownloadStream(Uri sourceUri, bool checkTimeout = false)
    {
        try
        {
            using var client = new HttpClient(FileCacheHandler);

            if (_stopwatch != null && checkTimeout)
            {
                var timeLeft = TimeLeft;
                _logger?.WriteToLog($"Opening stream to url '{sourceUri}'{(_imageLoadTimeout != 0 ? $" with a timeout of {timeLeft} milliseconds" : string.Empty)}");

                if (timeLeft == 0)
                {
                    _logger?.WriteToLog($"Image load has timed out, skipping opening stream to url '{sourceUri}'");
                    return null;
                }

                client.Timeout = TimeSpan.FromMilliseconds(timeLeft);
            }
            else            
                _logger?.WriteToLog($"Opening stream to url '{sourceUri}'");
            
            var response = await client.GetAsync(sourceUri).ConfigureAwait(false);
            
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger?.WriteToLog($"Opening stream failed with exception: {ExceptionHelpers.GetInnerException(exception)}");
            return null;
        }
    }
    #endregion

    #region GetTempFile
    /// <summary>
    ///     Returns a temporary file
    /// </summary>
    /// <param name="extension">The extension (e.g. .html)</param>
    /// <returns></returns>
    private string GetTempFile(string extension)
    {
        var tempFile = Guid.NewGuid() + extension;
        return Path.Combine(_tempDirectory?.FullName ?? Path.GetTempPath(), tempFile);
    }
    #endregion
}