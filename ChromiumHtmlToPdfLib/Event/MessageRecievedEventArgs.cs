using System;

namespace ChromiumHtmlToPdfLib.Event;

/// <summary>
///     <see cref="EventArgs" />
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     The received message
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Raised when a message is received
    /// </summary>
    /// <param name="message"></param>
    public MessageReceivedEventArgs(string message)
    {
        Message = message;
    }
}
