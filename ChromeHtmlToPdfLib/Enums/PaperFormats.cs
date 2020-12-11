//
// PaperFormats.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2014-2020 Magic-Sessions. (www.magic-sessions.com)
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

namespace ChromeHtmlToPdfLib.Enums
{
    /// <summary>
    ///     The paper formats to use when converting to PDF
    /// </summary>
    public enum PaperFormat
    {
        /// <summary>
        ///     Letter format
        /// </summary>
        Letter,

        /// <summary>
        ///     Legal format
        /// </summary>
        Legal,

        /// <summary>
        ///     Tabloid format
        /// </summary>
        Tabloid,

        /// <summary>
        ///     Ledger format
        /// </summary>
        Ledger,

        /// <summary>
        ///     A0 format
        /// </summary>
        A0,

        /// <summary>
        ///     A1 format
        /// </summary>
        A1,

        /// <summary>
        ///     A2 format
        /// </summary>
        A2,

        /// <summary>
        ///     A3 format
        /// </summary>
        A3,

        /// <summary>
        ///     A4 format
        /// </summary>
        A4,

        /// <summary>
        ///     A5 format
        /// </summary>
        A5,

        /// <summary>
        ///     A6 format
        /// </summary>
        A6,

        /// <summary>
        ///     Fit the page to the width and height of the content it is displaying
        /// </summary>
        FitPageToContent
    }
}