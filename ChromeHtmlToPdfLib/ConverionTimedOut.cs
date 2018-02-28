using System;
using System.Runtime.Serialization;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// Raised when the PDF conversion times out
    /// </summary>
    [Serializable]
    internal class ConversionTimedOutException : Exception
    {
        protected ConversionTimedOutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal ConversionTimedOutException() { }

        internal ConversionTimedOutException(string message) : base(message) { }

        internal ConversionTimedOutException(string message, Exception innerException) : base(message, innerException) { }
    }
}
