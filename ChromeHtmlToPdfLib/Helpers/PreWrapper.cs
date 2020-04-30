//
// PreWrapper.cs
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
using System.IO;
using System.Net;
using System.Text;
using System.Web;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    ///     Wraps a file in HTML PRE tags
    /// </summary>
    internal class PreWrapper
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
        /// <param name="logStream"></param>
        public PreWrapper(DirectoryInfo tempDirectory, Stream logStream)
        {
            _tempDirectory = tempDirectory;
            _logStream = logStream;
        }
        #endregion

        #region WrapFile
        /// <summary>
        ///     Wraps the given <paramref name="inputFile"/> in HTML pre tags
        /// </summary>
        /// <param name="inputFile">The input file</param>
        /// <param name="encoding">The encoding used in the input file</param>
        /// <returns>The wrapped HTML file</returns>
        public string WrapFile(string inputFile, Encoding encoding)
        {
            var temp = Path.GetFileName(inputFile) ?? string.Empty;
            var title = WebUtility.HtmlEncode(temp);
            var tempFile = GetTempFile;
            
            WriteToLog($"Reading text file '{inputFile}'");

            if (encoding == null)
            {
                Ude.CharsetDetector charsetDetector = new Ude.CharsetDetector();
                using (var fileStream = File.OpenRead(inputFile))
                {
                    WriteToLog("Trying to detect encoding");
                    charsetDetector.Feed(fileStream);
                    charsetDetector.DataEnd();
                    if (charsetDetector.Charset != null)
                    {
                        try
                        {
                            encoding = Encoding.GetEncoding(charsetDetector.Charset);
                        }
                        catch 
                        {
                            Console.WriteLine("Detection failed assuming standard encoding");
                            encoding = Encoding.Default;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Detection failed assuming standard encoding");
                        encoding = Encoding.Default;
                    }
                }
            }

            var streamReader = new StreamReader(inputFile, encoding);

            WriteToLog($"File is '{streamReader.CurrentEncoding.WebName}' encoded");

            var writeEncoding = new UnicodeEncoding(!BitConverter.IsLittleEndian, true);

            using (var writer = new StreamWriter(tempFile, false, writeEncoding))
            using (streamReader)
            {
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine($"   <meta charset=\"{writeEncoding.WebName}\">");
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

                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    if (line != null)
                        writer.WriteLine(HttpUtility.HtmlEncode(line));
                }

                writer.WriteLine("</pre>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }

            WriteToLog($"File pre wrapped and written to temporary file '{tempFile}'");

            return tempFile;
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
