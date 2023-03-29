using System;

namespace ChromeHtmlToPdfLib.Event
{
    /// <summary>
    ///     <see cref="EventArgs"/>
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; }

        /// <summary>
        ///     Raised when a massage is received
        /// </summary>
        /// <param name="message"></param>
        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
}
