using System;
using System.IO;
using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    ///     This class contains helper methods for image
    /// </summary>
    internal class ImageHelper
    {
        #region Fields
        /// <summary>
        ///     The temp folder
        /// </summary>
        private readonly DirectoryInfo _tempDirectory;
        #endregion

        #region Properties
        /// <summary>
        ///     Returns the temporary HTML file
        /// </summary>
        public string GetTempFile
        {
            get
            {
                var tempFile = Guid.NewGuid() + ".html";
                return Path.Combine(_tempDirectory?.FullName ?? Path.GetTempPath(), tempFile);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        ///     Makes this object and sets its needed properties
        /// </summary>
        /// <param name="tempDirectory">When set then this directory will be used for temporary files</param>
        public ImageHelper(DirectoryInfo tempDirectory = null)
        {
            _tempDirectory = tempDirectory;
        }
        #endregion

        private void GetAllImageTags()
        {
            var parser = new HtmlParser();
            var document = parser.Parse("<h1>Some example source</h1><p>This is a paragraph element");
            //Do something with document like the following

            Console.WriteLine("Serializing the (original) document:");
            Console.WriteLine(document.DocumentElement.OuterHtml);

            var p = document.CreateElement("p");
            p.TextContent = "This is another paragraph.";

            Console.WriteLine("Inserting another element in the body ...");
            document.Body.AppendChild(p);

            Console.WriteLine("Serializing the document again:");
            Console.WriteLine(document.DocumentElement.OuterHtml);
        }
    }
}
