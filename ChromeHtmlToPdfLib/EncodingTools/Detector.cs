using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ChromeHtmlToPdfLib.EncodingTools.Multilang;

namespace ChromeHtmlToPdfLib.EncodingTools
{
    public class Detector
    {
        #region Fields
        // this only contains ascii, default windows code page and unicode
        public int[] PreferedEncodingsForStream;

        // this contains all codepages, sorted by preference and byte usage 
        public int[] PreferedEncodings;

        // this contains all codepages, sorted by preference and byte usage 
        public int[] AllEncodings;
        #endregion

        #region Constructor
        /// <summary>
        /// Static constructor that fills the default preferred codepages
        /// </summary>
        public Detector()
        {
            var streamEcodings = new List<int>();
            var allEncodings = new List<int>();
            var mimeEcodings = new List<int>();

            // asscii - most simple so put it in first place...
            streamEcodings.Add(Encoding.ASCII.CodePage);
            mimeEcodings.Add(Encoding.ASCII.CodePage);
            allEncodings.Add(Encoding.ASCII.CodePage);


            // add default 2nd for all encodings
            allEncodings.Add(Encoding.Default.CodePage);
            // default is single byte?
            if (Encoding.Default.IsSingleByte)
            {
                // put it in second place
                streamEcodings.Add(Encoding.Default.CodePage);
                mimeEcodings.Add(Encoding.Default.CodePage);
            }


            // prefer JIS over JIS-SHIFT (JIS is detected better than JIS-SHIFT)
            // this one does include cyrilic (strange but true)
            allEncodings.Add(50220);
            mimeEcodings.Add(50220);
            

            // always allow unicode flavours for streams (they all have a preamble)
            streamEcodings.Add(Encoding.Unicode.CodePage);
            foreach (var enc in Encoding.GetEncodings())
            {
                if (!streamEcodings.Contains(enc.CodePage))
                {
                    var encoding = Encoding.GetEncoding(enc.CodePage);
                    if (encoding.GetPreamble().Length > 0)
                        streamEcodings.Add(enc.CodePage);
                }
            }


            // stream is done here
            PreferedEncodingsForStream = streamEcodings.ToArray();
            
            // all singlebyte encodings
            foreach (var encoding in Encoding.GetEncodings())
            {
                if (!encoding.GetEncoding().IsSingleByte)
                    continue;

                if (!allEncodings.Contains(encoding.CodePage))
                    allEncodings.Add(encoding.CodePage);

                // only add iso and IBM encodings to mime encodings 
                if (encoding.CodePage <= 1258)
                {
                    mimeEcodings.Add(encoding.CodePage);
                }
            }

            // add the rest (multibyte)
            foreach (var encoding in Encoding.GetEncodings())
            {
                if (!encoding.GetEncoding().IsSingleByte)
                {
                    if (!allEncodings.Contains(encoding.CodePage))
                        allEncodings.Add(encoding.CodePage);

                    // only add iso and IBM encodings to mime encodings 
                    if (encoding.CodePage <= 1258)
                    {
                        mimeEcodings.Add(encoding.CodePage);
                    }
                }
            }

            // add unicodes
            mimeEcodings.Add(Encoding.Unicode.CodePage);


            PreferedEncodings = mimeEcodings.ToArray();
            AllEncodings = allEncodings.ToArray();
        }
        #endregion

        #region IsAscii
        /// <summary>
        /// Checks if specified string data is acii data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool IsAscii(string data)
        {
            // assume empty string to be ascii
            if (string.IsNullOrEmpty(data))
                return true;

            foreach (var c in data)
            {
                if (c > 127)
                    return false;
            }

            return true;
        }
        #endregion

        #region GetMostEfficientEncoding
        /// <summary>
        /// Gets the best Encoding for usage in mime encodings
        /// </summary>
        /// <param name="input">text to detect</param>
        /// <returns>the suggested encoding</returns>
        public Encoding GetMostEfficientEncoding(string input)
        {
            return GetMostEfficientEncoding(input, PreferedEncodings);
        }

        /// <summary>
        /// Gets the best ISO Encoding for usage in a stream
        /// </summary>
        /// <param name="input">text to detect</param>
        /// <returns>the suggested encoding</returns>
        public Encoding GetMostEfficientEncodingForStream(string input)
        {
            return GetMostEfficientEncoding(input, PreferedEncodingsForStream);
        }

        /// <summary>
        /// Gets the best fitting encoding from a list of possible encodings
        /// </summary>
        /// <param name="input">text to detect</param>
        /// <param name="preferedEncodings">an array of codepages</param>
        /// <returns>the suggested encoding</returns>
        public Encoding GetMostEfficientEncoding(string input, int[] preferedEncodings)
        {
            var encoding = DetectOutgoingEncoding(input, preferedEncodings, true);
            // unicode.. hmmm... check for smallest encoding
            if (encoding.CodePage == Encoding.Unicode.CodePage)
            {
                var byteCount = Encoding.UTF7.GetByteCount(input);
                encoding = Encoding.UTF7;
                var bestByteCount = byteCount;

                // utf8 smaller?
                byteCount = Encoding.UTF8.GetByteCount(input);
                if (byteCount < bestByteCount)
                {
                    encoding = Encoding.UTF8;
                    bestByteCount = byteCount;
                }

                // unicode smaller?
                byteCount = Encoding.Unicode.GetByteCount(input);
                if (byteCount < bestByteCount)
                {
                    encoding = Encoding.Unicode;
                }
            }
            return encoding;
        }
        #endregion

        #region DetectOutgoingEncoding
        public Encoding DetectOutgoingEncoding(string input)
        {
            return DetectOutgoingEncoding(input, PreferedEncodings, true);
        }

        public Encoding DetectOutgoingStreamEncoding(string input)
        {
            return DetectOutgoingEncoding(input, PreferedEncodingsForStream, true);
        }

        public Encoding[] DetectOutgoingEncodings(string input)
        {
            return DetectOutgoingEncodings(input, PreferedEncodings, true);
        }

        public Encoding[] DetectOutgoingStreamEncodings(string input)
        {
            return DetectOutgoingEncodings(input, PreferedEncodingsForStream, true);
        }

        private Encoding DetectOutgoingEncoding(string input, int[] preferedEncodings, bool preserveOrder)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // empty strings can always be encoded as ASCII
            if (input.Length == 0)
                return Encoding.ASCII;

            var encoding = Encoding.ASCII;

            // get the IMultiLanguage3 interface
            IMultiLanguage3 multilang3 = new CMultiLanguageClass();
            if (multilang3 == null)
                throw new COMException("Failed to get IMultilang3");
            try
            {
                var resultCodePages =
                    new int[preferedEncodings != null ? preferedEncodings.Length : Encoding.GetEncodings().Length];
                var detectedCodepages = (uint) resultCodePages.Length;
                ushort specialChar = '?';


                // get unmanaged arrays
                var pPrefEncs = preferedEncodings == null
                                       ? IntPtr.Zero
                                       : Marshal.AllocCoTaskMem(sizeof (uint)*preferedEncodings.Length);
                var pDetectedEncs = Marshal.AllocCoTaskMem(sizeof (uint)*resultCodePages.Length);

                try
                {
                    if (preferedEncodings != null)
                        Marshal.Copy(preferedEncodings, 0, pPrefEncs, preferedEncodings.Length);

                    Marshal.Copy(resultCodePages, 0, pDetectedEncs, resultCodePages.Length);

                    var options = MLCPF.MLDETECTF_VALID_NLS;
                    if (preserveOrder)
                        options |= MLCPF.MLDETECTF_PRESERVE_ORDER;

                    if (preferedEncodings != null)
                        options |= MLCPF.MLDETECTF_PREFERRED_ONLY;

                    multilang3.DetectOutboundCodePage(options,
                                                      input, (uint) input.Length,
                                                      pPrefEncs,
                                                      (uint) (preferedEncodings == null ? 0 : preferedEncodings.Length),
                                                      pDetectedEncs, ref detectedCodepages,
                                                      ref specialChar);

                    // get result
                    if (detectedCodepages > 0)
                    {
                        var theResult = new int[detectedCodepages];
                        Marshal.Copy(pDetectedEncs, theResult, 0, theResult.Length);
                        encoding = Encoding.GetEncoding(theResult[0]);
                    }
                }
                finally
                {
                    if (pPrefEncs != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pPrefEncs);
                    Marshal.FreeCoTaskMem(pDetectedEncs);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang3);
            }
            return encoding;
        }

        public static Encoding[] DetectOutgoingEncodings(string input, int[] preferedEncodings, bool preserveOrder)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // empty strings can always be encoded as ASCII
            if (input.Length == 0)
                return new[] {Encoding.ASCII};

            var result = new List<Encoding>();

            // get the IMultiLanguage3 interface
            IMultiLanguage3 multilang3 = new CMultiLanguageClass();
            if (multilang3 == null)
                throw new COMException("Failed to get IMultilang3");
            try
            {
                var resultCodePages = new int[preferedEncodings.Length];
                var detectedCodepages = (uint) resultCodePages.Length;
                ushort specialChar = '?';


                // get unmanaged arrays
                var pPrefEncs = Marshal.AllocCoTaskMem(sizeof (uint)*preferedEncodings.Length);
                var pDetectedEncs = Marshal.AllocCoTaskMem(sizeof (uint)*resultCodePages.Length);

                try
                {
                    Marshal.Copy(preferedEncodings, 0, pPrefEncs, preferedEncodings.Length);

                    Marshal.Copy(resultCodePages, 0, pDetectedEncs, resultCodePages.Length);

                    MLCPF options = MLCPF.MLDETECTF_VALID_NLS | MLCPF.MLDETECTF_PREFERRED_ONLY;
                    if (preserveOrder)
                        options |= MLCPF.MLDETECTF_PRESERVE_ORDER;

                    options |= MLCPF.MLDETECTF_PREFERRED_ONLY;

                    // finally... call to DetectOutboundCodePage
                    multilang3.DetectOutboundCodePage(options,
                                                      input, (uint) input.Length,
                                                      pPrefEncs,
                                                      (uint) (preferedEncodings.Length),
                                                      pDetectedEncs, ref detectedCodepages,
                                                      ref specialChar);

                    // get result
                    if (detectedCodepages > 0)
                    {
                        var theResult = new int[detectedCodepages];
                        Marshal.Copy(pDetectedEncs, theResult, 0, theResult.Length);


                        // get the encodings for the codepages
                        for (int i = 0; i < detectedCodepages; i++)
                            result.Add(Encoding.GetEncoding(theResult[i]));
                    }
                }
                finally
                {
                    if (pPrefEncs != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(pPrefEncs);
                    Marshal.FreeCoTaskMem(pDetectedEncs);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang3);
            }
            // nothing found
            return result.ToArray();
        }
        #endregion

        #region DetectInputCodepage
        /// <summary>
        /// Detect the most probable codepage from an byte array
        /// </summary>
        /// <param name="input">array containing the raw data</param>
        /// <returns>the detected encoding or the default encoding if the detection failed</returns>
        public Encoding DetectInputCodepage(byte[] input)
        {
            try
            {
                var detected = DetectInputCodepages(input, 1);
                return detected.Length > 0 ? detected[0] : Encoding.Default;
            }
            catch (COMException)
            {
                // return default codepage on error
                return Encoding.Default;
            }
        }

        /// <summary>
        /// Returns up to maxEncodings codpages that are assumed to be apropriate
        /// </summary>
        /// <param name="input">array containing the raw data</param>
        /// <param name="maxEncodings">maxiumum number of encodings to detect</param>
        /// <returns>an array of Encoding with assumed encodings</returns>
        public static Encoding[] DetectInputCodepages(byte[] input, int maxEncodings)
        {
            if (maxEncodings < 1)
                throw new ArgumentOutOfRangeException(nameof(input), "maxEncodings");

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // empty strings can always be encoded as ASCII
            if (input.Length == 0)
                return new[] {Encoding.ASCII};

            // expand the string to be at least 256 bytes
            if (input.Length < 256)
            {
                var newInput = new byte[256];
                var steps = 256/input.Length;
                for (var i = 0; i < steps; i++)
                    Array.Copy(input, 0, newInput, input.Length*i, input.Length);

                var rest = 256%input.Length;
                if (rest > 0)
                    Array.Copy(input, 0, newInput, steps*input.Length, rest);
                input = newInput;
            }

            var result = new List<Encoding>();

            // get the IMultiLanguage" interface
            IMultiLanguage2 multilang2 = new CMultiLanguageClass();
            if (multilang2 == null)
                throw new COMException("Failed to get IMultilang2");
            try
            {
                var detectedEncdings = new DetectEncodingInfo[maxEncodings];

                var scores = detectedEncdings.Length;
                var srcLen = input.Length;

                // setup options (none)   
                const MLDETECTCP options = MLDETECTCP.MLDETECTCP_NONE;

                // finally... call to DetectInputCodepage
                multilang2.DetectInputCodepage(options, 0,
                                               ref input[0], ref srcLen, ref detectedEncdings[0], ref scores);

                // get result
                if (scores > 0)
                {
                    for (var i = 0; i < scores; i++)
                    {
                        // add the result
                        result.Add(Encoding.GetEncoding((int) detectedEncdings[i].nCodePage));
                    }
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang2);
            }
            // nothing found
            return result.ToArray();
        }
        #endregion

        #region ReadTextFile
        /// <summary>
        /// Opens a text file and returns the content 
        /// encoded in the most probable encoding
        /// </summary>
        /// <param name="path">path to the source file</param>
        /// <returns>the text content of the file</returns>
        public string ReadTextFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            using (Stream fs = File.Open(path, FileMode.Open))
            {
                var rawData = new byte[fs.Length];
                var encoding = DetectInputCodepage(rawData);
                return encoding.GetString(rawData);
            }
        }
        #endregion

        #region OpenTextFile
        /// <summary>
        /// Returns a stream reader for the given
        /// text file with the best encoding applied
        /// </summary>
        /// <param name="path">path to the file</param>
        /// <returns>a StreamReader for the file</returns>
        public StreamReader OpenTextFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            return OpenTextStream(File.Open(path, FileMode.Open));
        }
        #endregion

        #region OpenTextStream
        /// <summary>
        /// Creates a stream reader from a stream and detects
        /// the encoding form the first bytes in the stream
        /// </summary>
        /// <param name="stream">a stream to wrap</param>
        /// <returns>the newly created StreamReader</returns>
        public StreamReader OpenTextStream(Stream stream)
        {
            // check stream parameter
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanSeek)
                throw new ArgumentException("the stream must support seek operations", "stream");

            // assume default encoding at first place

            // seek to stream start
            stream.Seek(0, SeekOrigin.Begin);

            // buffer for preamble and up to 512b sample text for dection
            var buf = new byte[Math.Min(stream.Length, 512)];

            stream.Read(buf, 0, buf.Length);
            var detectedEncoding = DetectInputCodepage(buf);
            // seek back to stream start
            stream.Seek(0, SeekOrigin.Begin);


            return new StreamReader(stream, detectedEncoding);
        }
        #endregion
    }
}