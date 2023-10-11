using System.Collections.Generic;

namespace ChromiumHtmlToPdfLib.Helpers;

/// <summary>
///     <see cref="DocumentHelper.ValidateImagesAsync"/>
/// </summary>
internal class ValidateImagesResult
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
    #endregion

    #region SanitizeHtmlResult
    /// <summary>
    ///    Makes this object and sets it's needed properties
    /// </summary>
    /// <param name="success"><c>true</c> when success otherwise <c>false</c></param>
    /// <param name="outputUri"><see cref="ConvertUri"/> of the output file"/></param>
    internal ValidateImagesResult(bool success, ConvertUri outputUri)
    {
        Success = success;
        OutputUri = outputUri;
    }
    #endregion
}