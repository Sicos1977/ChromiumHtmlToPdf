using System;
using ChromeHtmlToPdfLib;

namespace ChromeHtmlToPdfConsole
{
    #region Enum ConversionItemStatus
    internal enum ConversionItemStatus
    {
        /// <summary>
        /// The item is not yet converted
        /// </summary>
        None,

        /// <summary>
        /// The conversion was successful
        /// </summary>
        Success,

        /// <summary>
        /// The conversion failed
        /// </summary>
        Failed
    }
    #endregion

    /// <summary>
    /// Used as a place holder when converting multiple items 
    /// </summary>
    internal class ConversionItem
    {
        #region Properties
        /// <summary>
        /// The uri to convert (this can be a file or url)
        /// </summary>
        public ConvertUri InputUri { get; internal set; }

        /// <summary>
        /// The output file
        /// </summary>
        public string OutputFile { get; }

        /// <summary>
        /// The status
        /// </summary>
        public ConversionItemStatus Status { get; private set; }

        /// <summary>
        /// The exception when <see cref="Status"/> is <see cref="ConversionItemStatus.Failed"/>
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Returns this object as a comma separated string
        /// </summary>
        public string OutputLine => (InputUri.IsFile
                                        ? $"\"{Status}\";\"{InputUri.AbsolutePath}\";\"{OutputFile}\""
                                        : $"\"{Status}\";\"{InputUri}\";\"{OutputFile}\"") +
                                    (Exception != null ? $";\"{Exception.Message}\"" : string.Empty) + Environment.NewLine;
        #endregion

        #region Constructor
        internal ConversionItem(ConvertUri inputUri, string outputFile)
        {
            InputUri = inputUri;
            OutputFile = outputFile;
            Status = ConversionItemStatus.None;
        }
        #endregion

        #region SetStatus
        /// <summary>
        /// Sets the status of the <see cref="ConversionItem"/>
        /// </summary>
        /// <param name="status"><see cref="ConversionItemStatus"/></param>
        /// <param name="exception"></param>
        public void SetStatus(ConversionItemStatus status, Exception exception = null)
        {
            Status = status;
            Exception = exception;
        }
        #endregion
    }
}
