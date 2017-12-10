using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using AngleSharp.Services.Media;
using ChromeHtmlToPdfLib.Enums;
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
        ///     The uri that contains the Image files that we are checking
        /// </summary>
        private readonly Uri _sourceUri;

        /// <summary>
        ///     The local directory when the <see cref="_sourceUri"/> scheme is a file
        /// </summary>
        private readonly string _localDirectory;

        /// <summary>
        ///     The <see cref="PaperFormat"/> that we are converting to
        /// </summary>
        private readonly PaperFormat _paperFormat;

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
        /// <param name="sourceUri">The uri that contains the Image files that we are checking</param>
        /// <param name="paperFormat">The <see cref="PaperFormat"/> that we are converting to</param>
        /// <param name="tempDirectory">When set then this directory will be used for temporary files</param>
        /// <param name="logStream">When set then logging is written to this stream</param>
        /// <param name="webProxy">The webproxy to use when downloading</param>
        public ImageHelper(Uri sourceUri, 
                           PaperFormat paperFormat, 
                           DirectoryInfo tempDirectory = null,
                           Stream logStream = null,
                           WebProxy webProxy = null)
        {
            _sourceUri = sourceUri;
            _paperFormat = paperFormat;

            if (_sourceUri.Scheme == "file")
                _localDirectory = Path.GetDirectoryName(_sourceUri.OriginalString);

            _tempDirectory = tempDirectory;
            _logStream = logStream;
            _webProxy = webProxy;
        }
        #endregion

        #region ValidateImages
        /// <summary>
        /// Validates all images if they fit on the given <see cref="_paperFormat"/>.
        /// If an image does not fit then a local copy is maded of the <see cref="_sourceUri"/>
        /// file and the images that don't fit are also downloaded and resized.
        /// </summary>
        /// <param name="outputFile">The outputfile when this method returns <c>false</c> otherwise
        /// <c>null</c> is returned</param>
        /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
        /// <exception cref="WebException">Raised when the webpage from <see cref="_sourceUri"/> could not be downloaded</exception>
        public bool ValidateImages(out string outputFile)
        {
            WriteToLog("Validating all images if they fit the page");
            outputFile = null;

            var webpage = _sourceUri.Scheme == "file"
                ? File.ReadAllText(_sourceUri.OriginalString)
                : DownloadString(_sourceUri);

            var changed = false;

            var config = Configuration.Default.WithCss();
            var parser = new HtmlParser(config);
            var document = parser.Parse(webpage);
            var htmlImages = document.QuerySelectorAll("img");

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (IHtmlImageElement htmlImage in htmlImages)
            {
                var t = htmlImage.Attributes;
                var width = htmlImage.Attributes;
                var height = htmlImage.DisplayHeight;
                Image image = null;

                if (width <= 0 || height <= 0)
                {
                    image = GetImage(new Uri(htmlImage.Source));
                    width = image.Width;
                    height = image.Height;
                }

                var maxDimension = new ImageDimension(_paperFormat);
                if (width > maxDimension.Width || height > maxDimension.Height)
                {
                    var extension = Path.GetExtension(htmlImage.Source);
                    var fileName = GetTempFile(extension);
                    image = ScaleImage(image, maxDimension.Width);
                    image.Save(fileName);
                    htmlImage.DisplayWidth = image.Width;
                    htmlImage.DisplayHeight = image.Height;
                    htmlImage.Source = new Uri(fileName).ToString();
                    changed = true;
                }
            }

            if (!changed) return true;
            outputFile = GetTempFile(".html");
            File.WriteAllText(outputFile, document.Origin);
            return false;
        }
        #endregion

        #region GetImage
        /// <summary>
        /// Returns the <see cref="Image"/> for the given <paramref name="imageUri"/>
        /// </summary>
        /// <param name="imageUri"></param>
        /// <returns></returns>
        private Image GetImage(Uri imageUri) 
        {
            WriteToLog($"Gettimg image from uri '{imageUri}'");

            try
            {
                switch (imageUri.Scheme)
                {
                    case "https":
                    case "http:":
                        using (var webStream = WebClient.OpenRead(imageUri))
                            if (webStream != null)
                                return Image.FromStream(webStream, true, false);
                        break;

                    case "file":
                        var fileName = imageUri.OriginalString;

                        if (!File.Exists(fileName))
                            fileName = Path.Combine(_localDirectory, imageUri.AbsolutePath.Trim('/'));

                        if (File.Exists(fileName))
                            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                                return Image.FromStream(fileStream, true, false);

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
        public Image ScaleImage(Image image, int maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(newImage))
                g.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
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
                return null;;
            }
        }
        #endregion

        #region GetTempFile
        /// <summary>
        /// Returns a temporary file
        /// </summary>
        /// <param name="extension">The extension (e.g. .html)</param>
        /// <returns></returns>
        public string GetTempFile(string extension)
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
