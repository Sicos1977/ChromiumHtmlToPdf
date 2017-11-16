using System;
using System.Runtime.Serialization;
using ChromeHtmlToPdfLib.Protocol;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// Raised when an error is returned from Chrome
    /// </summary>
    [Serializable]
    public class ChromeException : Exception
    {
        /// <summary>
        /// Returns the error code that is returned from Chrome
        /// </summary>
        public double Code { get; }

        protected ChromeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ChromeException() { }

        public ChromeException(Error error) : base(error.InnerError.Message)
        {
            Code = error.InnerError.Code;
        }

        public ChromeException(string message) : base(message) { }

        public ChromeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
