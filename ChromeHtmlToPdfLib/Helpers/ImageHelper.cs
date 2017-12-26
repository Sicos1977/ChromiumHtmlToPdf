using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using AngleSharp;
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
        private static Stream _logStream;

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

                _webClient = new WebClient {Proxy = _webProxy};
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
        /// <param name="webProxy">The webproxy to use when downloading</param>
        public ImageHelper(DirectoryInfo tempDirectory = null,
                           Stream logStream = null,
                           WebProxy webProxy = null)
        {
            _tempDirectory = tempDirectory;
            _logStream = logStream;
            _webProxy = webProxy;
        }
        #endregion

        #region ParseValue
        /// <summary>
        /// Parses the value from the given value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int ParseValue(string value)
        {
            value = value.Replace("px", string.Empty);
            value = value.Replace(" ", string.Empty);
            return int.TryParse(value, out int result) ? result : 0;
        }
        #endregion

        #region ValidateImages
        /// <summary>
        /// Validates all images if they are rotated correctly (when <paramref name="rotate"/> is set
        /// to <c>true</c>) and fit on the given <paramref name="pageSettings"/>.
        /// If an image does need to be rotated or does not fit then a local copy is maded of 
        /// the <paramref name="sourceUri"/> file.
        /// </summary>
        /// <param name="sourceUri">The uri of the webpage</param>
        /// <param name="resize">When set to <c>true</c> then an image is resized when needed</param>
        /// <param name="rotate">When set to <c>true</c> then the EXIF information of an
        /// image is read and when needed the image is automaticly rotated</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="outputFile">The outputfile when this method returns <c>false</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
        /// <exception cref="WebException">Raised when the webpage from <paramref name="sourceUri"/> could not be downloaded</exception>
        public bool ValidateImages(Uri sourceUri,
                                   bool resize,
                                   bool rotate,
                                   PageSettings pageSettings, 
                                   out string outputFile)
        {
            WriteToLog("Validating all images if they need to be rotated and if they fit the page");
            outputFile = null;

            string localDirectory = null;

            if (sourceUri.IsFile)
                localDirectory = Path.GetDirectoryName(sourceUri.OriginalString);

            var webpage = sourceUri.IsFile
                ? File.ReadAllText(sourceUri.OriginalString)
                : DownloadString(sourceUri);

            var maxWidth = pageSettings.PaperWidth * 96.0;
            var maxHeight = pageSettings.PaperHeight * 96.0;

            var changed = false;
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(m => m.Content(webpage)).Result;

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (var htmlImage in document.Images)
            {
                Image image = null;

                try
                {
                    // The local width and height attributes always go before css width and height
                    var width = htmlImage.DisplayWidth;
                    var height = htmlImage.DisplayHeight;

                    if (rotate)
                    {
                        image = GetImage(new Uri(htmlImage.Source), localDirectory);
                        if (RotateImageByExifOrientationData(image))
                        {
                            htmlImage.DisplayWidth = image.Width;
                            htmlImage.DisplayHeight = image.Height;
                        }
                        width = image.Width;
                        height = image.Height;
                        changed = true;
                    }

                    if (!resize) continue;

                    if (height == 0 && width == 0)
                    {
                        var style = context.Current.GetComputedStyle(htmlImage);
                        if (style != null)
                        {
                            width = ParseValue(style.Width);
                            height = ParseValue(style.Height);
                        }
                    }

                    // If we don't know the image size then get if from the image itself
                    if (width <= 0 || height <= 0)
                    {
                        if (image == null)
                            image = GetImage(new Uri(htmlImage.Source), localDirectory);
                        width = image.Width;
                        height = image.Height;
                    }

                    if (width > maxWidth || height > maxHeight)
                    {
                        var extension = Path.GetExtension(htmlImage.Source.Contains("?")
                            ? htmlImage.Source.Split('?')[0]
                            : htmlImage.Source);

                        var fileName = GetTempFile(extension);

                        // If we did not load the image already then load it
                        if (image == null)
                            image = GetImage(new Uri(htmlImage.Source), localDirectory);

                        image = ScaleImage(image, (int)maxWidth);
                        WriteToLog($"Image resized to width {image.Width} and height {image.Height}");
                        image.Save(fileName);
                        htmlImage.DisplayWidth = image.Width;
                        htmlImage.DisplayHeight = image.Height;
                        htmlImage.Source = new Uri(fileName).ToString();
                        changed = true;
                    }
                }
                finally
                {
                    image?.Dispose();
                }
            }

            if (!changed)
                return true;

            outputFile = GetTempFile(".html");

            using (var textWriter = new StreamWriter(outputFile))
                document.ToHtml(textWriter, new AutoSelectedMarkupFormatter());

            return false;
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
                        using (var webStream = WebClient.OpenRead(imageUri))
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
        /// Scales the image to the prefered max width
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxWidth"></param>
        /// <returns></returns>
        private static Image ScaleImage(Image image, double maxWidth)
        {
            var ratio = maxWidth / image.Width;
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);
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
                var result = WebClient.DownloadString(sourceUri);
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
            var line = DateTime.Now.ToString("s") + (InstanceId != null ? " - " + InstanceId : string.Empty) + " - " +
                       message + Environment.NewLine;
            var bytes = Encoding.UTF8.GetBytes(line);
            _logStream.Write(bytes, 0, bytes.Length);
            _logStream.Flush();
        }
        #endregion
    }
}
