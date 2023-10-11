using System.Collections.Generic;

namespace ChromiumHtmlToPdfLib.Helpers;

/// <summary>
///     <see cref="DocumentHelper.SanitizeHtmlAsync"/>
/// </summary>
internal class SanitizeHtmlResult
{
    #region Properties
    /// <summary>
    ///     <c>true</c> when success otherwise <c>false</c>
    /// </summary>
    internal bool Success { get;  }

    /// <summary>
    ///     <see cref="ConvertUri"/> of the output file"/>
    /// </summary>
    internal ConvertUri OutputUri { get;  }

    /// <summary>
    ///     A list of safe URL's
    /// </summary>
    internal List<string> SafeUrls { get; }
    #endregion

    #region SanitizeHtmlResult
    /// <summary>
    ///    Makes this object and sets it's needed properties
    /// </summary>
    /// <param name="success"><c>true</c> when success otherwise <c>false</c></param>
    /// <param name="outputUri"><see cref="ConvertUri"/> of the output file"/></param>
    /// <param name="safeUrls">A list of safe URL's</param>
    internal SanitizeHtmlResult(bool success, ConvertUri outputUri, List<string> safeUrls)
    {
        Success = success;
        OutputUri = outputUri;
        SafeUrls = safeUrls;
    }
    #endregion
}