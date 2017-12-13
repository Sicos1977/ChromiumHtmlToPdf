using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using AngleSharp;
using ChromeHtmlToPdfLib.Settings;
//using MetadataExtractor;
//using Directory = System.IO.Directory;
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
        /// Validates all images if they fit on the given <paramref name="pageSettings"/>.
        /// If an image does not fit then a local copy is maded of the <paramref name="sourceUri"/>
        /// file and the images that don't fit are also downloaded and resized.
        /// </summary>
        /// <param name="sourceUri">The uri of the webpage</param>
        /// <param name="pageSettings"><see cref="PageSettings"/></param>
        /// <param name="outputFile">The outputfile when this method returns <c>false</c> otherwise
        ///     <c>null</c> is returned</param>
        /// <returns>Returns <c>false</c> when the images dit not fit the page, otherwise <c>true</c></returns>
        /// <exception cref="WebException">Raised when the webpage from <paramref name="sourceUri"/> could not be downloaded</exception>
        public bool ValidateImages(Uri sourceUri, PageSettings pageSettings, out string outputFile)
        {
            WriteToLog("Validating all images if they fit the page");
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
            var imagesResized = 0;
            var config = Configuration.Default.WithCss();
            var context = BrowsingContext.New(config);
            var document = context.OpenAsync(m => m.Content(webpage)).Result;

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (var htmlImage in document.Images)
            {
                // The local width and height attributes always go before css width and height
                var width = htmlImage.DisplayWidth;
                var height = htmlImage.DisplayHeight;

                if (height == 0 && width == 0)
                {
                    var style = context.Current.GetComputedStyle(htmlImage);
                    if (style != null)
                    {
                        width = ParseValue(style.Width);
                        height = ParseValue(style.Height);
                    }
                }

                Image image = null;
                // If we don't know the image size then get if from the image itself
                if (width <= 0 || height <= 0)
                {
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

                    image = ScaleImage(image, (int) maxWidth);
                    image.Save(fileName);
                    htmlImage.DisplayWidth = image.Width;
                    htmlImage.DisplayHeight = image.Height;
                    htmlImage.Source = new Uri(fileName).ToString();
                    imagesResized += 1;
                    changed = true;
                }
            }

            if (!changed)
            {
                WriteToLog("All images fit the page, there were no changes needed");
                return true;
            }

            WriteToLog($"{imagesResized} image {(imagesResized == 1 ? "was" : "were")} resized to fit the page");

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
                switch (imageUri.Scheme)
                {
                    case "https":
                    case "http:":
                        //GetImageOrientation(WebClient.OpenRead(imageUri));

                        using (var webStream = WebClient.OpenRead(imageUri))
                            if (webStream != null)
                                return Image.FromStream(webStream, true, false);
                        break;

                    case "file":
                        var fileName = imageUri.OriginalString;

                        if (!File.Exists(fileName))
                            fileName = Path.Combine(localDirectory, imageUri.AbsolutePath.Trim('/'));

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

        //private void GetImageOrientation(Stream stream)
        //{
        //    var directories = ImageMetadataReader.ReadMetadata(stream);
        //    foreach (var directory in directories)
        //    {
        //        var name = directory.Name;
        //    }
        //}

        #region ScaleImage
        /// <summary>
        /// Scales the image to the prefered max width
        /// </summary>
        /// <param name="image"></param>
        /// <param name="maxWidth"></param>
        /// <returns></returns>
        private Image ScaleImage(Image image, double maxWidth)
        {
            var ratio = maxWidth / image.Width;

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
