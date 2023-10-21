using System;
using Microsoft.Extensions.Logging;

namespace ChromiumHtmlToPdfLib.Loggers;

/// <summary>
///     A helper class to share logging between the different classes
/// </summary>
internal class Logger
{
    #region Fields
    /// <summary>
    ///     Used to make the logging thread safe
    /// </summary>
    private static readonly object LoggerLock = new();

    /// <summary>
    ///     When set then logging is written to this ILogger instance
    /// </summary>
    public ILogger InternalLogger { get; set; }
    
    /// <summary>
    ///     An unique id that can be used to identify the logging of the converter when
    ///     calling the code from multiple threads and writing all the logging to the same file
    /// </summary>
    public string InstanceId { get; set; }
    #endregion

    #region Constructor
    /// <summary>
    ///     Makes this object and sets all its needed properties
    /// </summary>
    /// <param name="logger"></param>
    internal Logger(ILogger logger, string instanceId)
    {
        InternalLogger = logger;
        InstanceId = instanceId;
    }
    #endregion

    #region WriteToLog
    /// <summary>
    ///     Writes a line to the <see cref="InternalLogger" />
    /// </summary>
    /// <param name="message">The message to write</param>
    internal void WriteToLog(string message)
    {
        lock (LoggerLock)
        {
            try
            {
                if (InternalLogger == null) return;
                using (InternalLogger.BeginScope(InstanceId))
                {
                    InternalLogger.LogInformation(message);
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }
    }
    #endregion
}