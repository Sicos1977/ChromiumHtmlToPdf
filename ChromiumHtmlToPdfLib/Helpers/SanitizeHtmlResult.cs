//
// SanitizeHtmlResult.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2025 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

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
    ///     <see cref="ConvertUri"/> of the output file
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
    /// <param name="outputUri"><see cref="ConvertUri"/> of the output file</param>
    /// <param name="safeUrls">A list of safe URL's</param>
    internal SanitizeHtmlResult(bool success, ConvertUri outputUri, List<string> safeUrls)
    {
        Success = success;
        OutputUri = outputUri;
        SafeUrls = safeUrls;
    }
    #endregion
}