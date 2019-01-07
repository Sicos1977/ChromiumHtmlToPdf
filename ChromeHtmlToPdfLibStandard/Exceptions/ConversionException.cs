using System;
using System.Runtime.Serialization;

namespace ChromeHtmlToPdfLib.Exceptions
{
    /// <summary>
    /// Raised when the PDF conversion fails
    /// </summary>
    [Serializable]
    internal class ConversionException : Exception
    {
        protected ConversionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal ConversionException() { }

        internal ConversionException(string message) : base(message) { }

        internal ConversionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
