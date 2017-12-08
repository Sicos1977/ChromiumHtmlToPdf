//
// PreWrapper.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Web;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    ///     Wraps a file in HTML PRE tags
    /// </summary>
    internal class PreWrapper
    {
        #region Fields
        /// <summary>
        ///     The temp folder
        /// </summary>
        private readonly DirectoryInfo _tempDirectory;
        #endregion

        #region Property
        /// <summary>
        ///     When set then this option will be used for white space wrapping
        /// </summary>
        /// <remarks>
        ///     Default set to pre-wrap
        /// </remarks>
        public string WhiteSpace { get; set; } = "pre-wrap";

        /// <summary>
        ///     When set then this font will be used when &lt;PRE&gt; wrapping.
        ///     Otherwise the default system font will be used.
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        ///     When set then this style will be used when &lt;PRE&gt; wrapping.
        ///     Otherwise the default system font style will be used.
        /// </summary>
        public string FontStyle { get; set; }

        /// <summary>
        ///     When set then this font size will be used when &lt;PRE&gt; wrapping.
        ///     Otherwise the default system font size will be used.
        /// </summary>

        public string FontSize { get; set; }

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
        public PreWrapper(DirectoryInfo tempDirectory = null)
        {
            _tempDirectory = tempDirectory;
        }
        #endregion

        #region WrapFile
        /// <summary>
        ///     Wraps the given <paramref name="inputFile"/> in HTML pre tags
        /// </summary>
        /// <param name="inputFile"></param>
        /// <returns>The wrapped HTML file</returns>
        public string WrapFile(string inputFile)
        {
            var temp = Path.GetFileName(inputFile) ?? string.Empty;
            var title = HttpUtility.HtmlEncode(temp);
            var tempFile = GetTempFile;

            using (var writer = new StreamWriter(tempFile))
            using (var reader = new StreamReader(inputFile))
            {
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine($"<title>{title}</title>");
                writer.WriteLine("<style>");
                writer.WriteLine("  pre {");
                writer.WriteLine($"  white-space: { WhiteSpace };");
                if (!string.IsNullOrWhiteSpace(FontFamily))
                    writer.WriteLine($"  font-family: { FontFamily };");
                if (!string.IsNullOrWhiteSpace(FontFamily))
                    writer.WriteLine($"  font-style: { FontStyle };");
                if (!string.IsNullOrWhiteSpace(FontFamily))
                    writer.WriteLine($"  font-size: { FontSize };");
                writer.WriteLine("  }");
                writer.WriteLine("</style>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine("<pre>");

                while (!reader.EndOfStream)
                    writer.WriteLine(reader.ReadLine());

                writer.WriteLine("</pre>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }

            return tempFile;
        }
        #endregion
    }
}
