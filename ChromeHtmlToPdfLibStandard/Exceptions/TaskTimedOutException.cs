using System;
using System.Runtime.Serialization;

namespace ChromeHtmlToPdfLib.Exceptions
{
    /// <summary>
    /// Raised when a task times out
    /// </summary>
    [Serializable]
    internal class TaskTimedOutException : Exception
    {
        protected TaskTimedOutException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal TaskTimedOutException() { }

        internal TaskTimedOutException(string message) : base(message) { }

        internal TaskTimedOutException(string message, Exception innerException) : base(message, innerException) { }
    }
}
