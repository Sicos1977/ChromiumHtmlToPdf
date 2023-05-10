using System;

namespace ChromiumHtmlToPdfLib.Event
{
    /// <summary>
    ///     Raised when an error is thrown from Chromium
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        ///     Returns the raised exception
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Raised when an exception occurs
        /// </summary>
        /// <param name="exception"></param>
        public ErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
