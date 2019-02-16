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
using AngleSharp.Html.Dom;
using AngleSharp.Xhtml;
using ChromeHtmlToPdfLib.Settings;
using Image = System.Drawing.Image;

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    ///     This class contains helper methods for image
    /// </summary>
    public class ImageHelper
    {
        #region Fields
        /// <summary>
        ///     When set then logging is written to this stream
        /// </summary>
        private readonly Stream _logStream;

        /// <summary>
        ///     An unique id that can be used to identify the logging of the converter when
        ///     calling the code from multiple threads and writing all the logging to the same file
        /// </summary>
        public string InstanceId { get; set; }

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
        /// <param name="logStream">When set then logging is written to this stream</param>
        /// <param name="webProxy">The web proxy to use when downloading</param>
        /// <param name="timeout"></param>
        public ImageHelper(DirectoryInfo tempDirectory = null,
                           Stream logStream = null,
                           WebProxy webProxy = null,
                           int? timeout = null)
        {
            _tempDirectory = tempDirectory;
            _logStream = logStream;
            _webProxy = webProxy;
            _timeout = timeout ?? 30000;
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

        #region GetImageFromBase64
        private Image GetImageFromBase64(string data)
        {
            WriteToLog("Decoding image from base64 string");

            try
            {
                var base64Data = Regex.Match(data, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
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
        /// image is read and when needed the image is automatic rotated</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="outputUri">The outputUri when this method returns <c>false</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
        /// <exception cref="WebException">Raised when the webpage from <paramref name="inputUri"/> could not be downloaded</exception>
        public bool ValidateImages(ConvertUri inputUri,
                                   bool resize,
                                   bool rotate,
                                   PageSettings pageSettings,
                                   out ConvertUri outputUri)
        {
            WriteToLog("Validating all images if they need to be rotated and if they fit the page");
            outputUri = null;

            string localDirectory = null;

            if (inputUri.IsFile)
                localDirectory = Path.GetDirectoryName(inputUri.OriginalString);

            var webpage = inputUri.IsFile
                ? inputUri.Encoding != null
                    ? File.ReadAllText(inputUri.OriginalString, inputUri.Encoding)
                    : File.ReadAllText(inputUri.OriginalString)
                : DownloadString(inputUri);

            /*
             * const convertPixelToInches = (value, dpi) => {
      let inches = value/ dpi;
      return `${inches}in`; // Calculate inches value and round it up.
    }
             */

            var maxWidth = (pageSettings.PaperWidth - pageSettings.MarginLeft - pageSettings.MarginRight) * 96.0;
            var maxHeight = (pageSettings.PaperHeight - pageSettings.MarginTop - pageSettings.MarginBottom) * 96.0;

            var htmlChanged = false;
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);

            var document = inputUri.Encoding != null
                ? context.OpenAsync(m => m.Content(webpage).Header("Content-Type", $"text/html; charset={inputUri.Encoding.WebName}")).Result
                : context.OpenAsync(m => m.Content(webpage)).Result;

            var unchangedImages = new List<IHtmlImageElement>();

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
                var extension = Path.GetExtension(htmlImage.Source.Contains("?")
                    ? htmlImage.Source.Split('?')[0]
                    : htmlImage.Source);
                var fileName = GetTempFile(extension);

                try
                {
                    // The local width and height attributes always go before css width and height
                    var width = htmlImage.DisplayWidth;
                    var height = htmlImage.DisplayHeight;

                    if (rotate)
                    {
                        image = htmlImage.Source.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase)
                            ? GetImageFromBase64(htmlImage.Source)
                            : GetImage(new Uri(htmlImage.Source), localDirectory);

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
                            {
                                image = htmlImage.Source.StartsWith("data:",
                                    StringComparison.InvariantCultureIgnoreCase)
                                    ? GetImageFromBase64(htmlImage.Source)
                                    : GetImage(new Uri(htmlImage.Source), localDirectory);
                            }

                            if (image == null) continue;
                            width = image.Width;
                            height = image.Height;
                        }

                        if (width > maxWidth || height > maxHeight)
                        {
                            // If we did not load the image already then load it

                            if (image == null)
                                image = htmlImage.Source.StartsWith("data:",
                                    StringComparison.InvariantCultureIgnoreCase)
                                    ? GetImageFromBase64(htmlImage.Source)
                                    : GetImage(new Uri(htmlImage.Source), localDirectory);

                            if (image == null) continue;

                            image = ScaleImage(image, (int) maxWidth);
                            WriteToLog($"Image resized to width {image.Width} and height {image.Height} and saved to location '{fileName}'");
                            image.Save(fileName);
                            htmlImage.DisplayWidth = image.Width;
                            htmlImage.DisplayHeight = image.Height;
                            htmlImage.SetStyle(string.Empty);
                            htmlImage.Source = new Uri(fileName).ToString();
                            htmlChanged = true;
                            imageChanged = true;
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
                return true;

            foreach (var unchangedImage in unchangedImages)
            {
                var imageSource = new Uri(unchangedImage.Source);
                using(var image = GetImage(imageSource, localDirectory))
                {
                    if (localDirectory != null)
                    {
                        var fileName = Path.Combine(localDirectory, Path.GetFileName(imageSource.ToString()));
                        unchangedImage.Source = new Uri(fileName).ToString();
                    }
                    else
                    {
                        var extension = Path.GetExtension(unchangedImage.Source.Contains("?")
                            ? unchangedImage.Source.Split('?')[0]
                            : unchangedImage.Source);
                        var fileName = GetTempFile(extension);

                        WriteToLog($"Unchanged image saved to location '{fileName}'");
                        image.Save(fileName);
                        unchangedImage.Source = new Uri(fileName).ToString();
                    }
                }
            }

            var outputFile = GetTempFile(".htm");
            outputUri = new ConvertUri(outputFile, inputUri.Encoding);

            try
            {
                using (var fileStream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
                {
                    if (inputUri.Encoding != null)
                    {
                        using (var textWriter = new StreamWriter(fileStream, inputUri.Encoding))
                            document.ToHtml(textWriter, new AutoSelectedMarkupFormatter());
                    }
                    else
                        using (var textWriter = new StreamWriter(fileStream))
                            document.ToHtml(textWriter, new AutoSelectedMarkupFormatter());

                }

                return false;
            }
            catch (Exception exception)
            {
                WriteToLog($"Could not generate new html file '{outputFile}', error: {ExceptionHelpers.GetInnerException(exception)}");
                return true;
            }
        }
        #endregion

        #region GetImage
        /// <summary>
        /// Returns the <see cref="Image"/> for the given <paramref name="imageUri"/>
        /// </summary>
        /// <param name="imageUri"></param>
        /// <param name="localDirectory"></param>
        /// <returns></returns>
        private Image GetImage(Uri imageUri, string localDirectory) 
        {
            WriteToLog($"Getting image from uri '{imageUri}'");

            try
            {
                if (imageUri.IsLoopback || imageUri.IsFile)
                {
                    var fileName = imageUri.OriginalString;

                    if (!File.Exists(fileName))
                        fileName = Path.Combine(localDirectory, imageUri.AbsolutePath.Trim('/'));

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
        /// <param name="maxWidth"></param>
        /// <returns></returns>
        private Image ScaleImage(Image image, double maxWidth)
        {
            var ratio = maxWidth / image.Width;
            var newWidth = (int)(image.Width * ratio);
            if (newWidth == 0) newWidth = 1;
            var newHeight = (int)(image.Height * ratio);
            if (newHeight == 0) newHeight = 1;
            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphic = Graphics.FromImage(newImage))
                graphic.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
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

        #region DownloadString
        /// <summary>
        ///     Downloads from the given <paramref name="sourceUri"/> and it as a string
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <returns></returns>
        private string DownloadString(Uri sourceUri)
        {
            try
            {
                WriteToLog($"Downloading from uri '{sourceUri}'");
                var result = WebClient.DownloadStringTaskAsync(sourceUri).Timeout(_timeout).GetAwaiter().GetResult();
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
            if (_logStream == null) return;
            var line = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + (InstanceId != null ? " - " + InstanceId : string.Empty) + " - " +
                       message + Environment.NewLine;
            var bytes = Encoding.UTF8.GetBytes(line);
            _logStream.Write(bytes, 0, bytes.Length);
            _logStream.Flush();
        }
        #endregion
    }
}
