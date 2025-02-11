﻿//
// PageSettings.cs
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

using System;
using ChromiumHtmlToPdfLib.Enums;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace ChromiumHtmlToPdfLib.Settings;

/// <summary>
///     The page settings to use when converting to PDF
/// </summary>
public class PageSettings : ICloneable
{
    #region Properties
    /// <summary>
    ///     Paper orientation. Defaults to false.
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    ///     Display header and footer. Defaults to false.
    /// </summary>
    public bool DisplayHeaderFooter { get; set; }

    /// <summary>
    ///     HTML template for the print header.
    ///     Should be valid HTML markup with following classes used to inject printing values into them:
    ///     - date - formatted print date - title - document title - url - document location - pageNumber - current page number
    ///     - totalPages - total pages in the document For example, would generate span containing the title.
    /// </summary>
    public string? HeaderTemplate { get; set; }

    /// <summary>
    ///     HTML template for the print footer. Should use the same format as the headerTemplate.
    /// </summary>
    public string? FooterTemplate { get; set; }

    /// <summary>
    ///     Print background graphics. Defaults to false.
    /// </summary>
    public bool PrintBackground { get; set; }

    /// <summary>
    ///     Scale of the webpage rendering. Defaults to 1.
    /// </summary>
    public double Scale { get; set; }

    /// <summary>
    ///     Paper width in inches. Defaults to 8.5 inches.
    /// </summary>
    public double PaperWidth { get; set; }

    /// <summary>
    ///     Paper height in inches. Defaults to 11 inches.
    /// </summary>
    public double PaperHeight { get; set; }

    /// <summary>
    ///     Top margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    public double MarginTop { get; set; }

    /// <summary>
    ///     Bottom margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    public double MarginBottom { get; set; }

    /// <summary>
    ///     Left margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    public double MarginLeft { get; set; }

    /// <summary>
    ///     Right margin in inches. Defaults to 1cm (~0.4 inches).
    /// </summary>
    public double MarginRight { get; set; }

    /// <summary>
    ///     Paper ranges to print, e.g., '1-5, 8, 11-13'. Defaults to the empty string, which means print all pages.
    /// </summary>
    public string? PageRanges { get; set; }

    /// <summary>
    ///     Whether to prefer page size as defined by css. Defaults to false, in which case the content will be scaled
    ///     to fit the paper size.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public bool PreferCSSPageSize { get; set; }

    /// <summary>
    ///     The paper format
    /// </summary>
    public PaperFormat PaperFormat { get; private set; }

    /// <summary>
    ///     Generate tagged PDF. Defaults to true.
    /// </summary>
    public bool TaggedPdf { get; set; } = true;

    /// <summary>
    ///     Generate outline bookmarks from header tags (H1-H6). Defaults to false.
    ///     Requires both <see cref="Converter.UseOldHeadlessMode"/> and <see cref="TaggedPdf"/> set to <c>true</c>.
    /// </summary>
    public bool GenerateOutline { get; set; }
    #endregion

    #region PageSettings
    /// <summary>
    ///     Makes this object and sets all the settings to its default values
    /// </summary>
    /// <remarks>
    ///     Default paper settings are set to <see cref="Enums.PaperFormat.A4" />
    /// </remarks>
    public PageSettings()
    {
        ResetToDefaultSettings();
    }

    /// <summary>
    ///     Makes this object and sets all the settings to its default values
    /// </summary>
    /// <remarks>
    ///     Default paper settings are set to <see cref="Enums.PaperFormat.A4" />
    /// </remarks>
    /// <param name="paperFormat"></param>
    public PageSettings(PaperFormat paperFormat)
    {
        ResetToDefaultSettings();
        PaperFormat = paperFormat;
        SetPaperFormat(paperFormat);
    }
    #endregion

    #region ResetToDefaultSettings
    /// <summary>
    ///     Resets the <see cref="PageSettings" /> to it's default settings
    /// </summary>
    public void ResetToDefaultSettings()
    {
        Landscape = false;
        DisplayHeaderFooter = false;
        PrintBackground = false;
        Scale = 1.0;
        SetPaperFormat(PaperFormat.A4);
        MarginTop = 0.4;
        MarginBottom = 0.4;
        MarginLeft = 0.4;
        MarginRight = 0.4;
        PageRanges = string.Empty;
        TaggedPdf = true;
        GenerateOutline = false;
    }
    #endregion

    #region SetPaperFormat
    /// <summary>
    ///     Set the given <paramref name="paperFormat" />
    /// </summary>
    /// <param name="paperFormat">
    ///     <see cref="PaperFormat" />
    /// </param>
    public void SetPaperFormat(PaperFormat paperFormat)
    {
        PaperFormat = paperFormat;

        double mmToInch(double mm) => Math.Round(mm / 25.4, 6);

        switch (paperFormat)
        {
            case PaperFormat.Letter:
                PaperWidth = 8.5;
                PaperHeight = 11;
                break;

            case PaperFormat.Legal:
                PaperWidth = 8.5;
                PaperHeight = 14;
                break;

            case PaperFormat.Tabloid:
                PaperWidth = 11;
                PaperHeight = 17;
                break;

            case PaperFormat.Ledger:
                PaperWidth = 17;
                PaperHeight = 11;
                break;

            case PaperFormat.A0:
                PaperWidth = mmToInch(841.0);
                PaperHeight = mmToInch(1189.0);
                break;

            case PaperFormat.A1:
                PaperWidth = mmToInch(594.0);
                PaperHeight = mmToInch(841.0);
                break;

            case PaperFormat.A2:
                PaperWidth = mmToInch(420.0);
                PaperHeight = mmToInch(594.0);
                break;

            case PaperFormat.A3:
                PaperWidth = mmToInch(297.0);
                PaperHeight = mmToInch(420.0);
                break;

            case PaperFormat.A4:
                PaperWidth = mmToInch(210.0);
                PaperHeight = mmToInch(297.0);
                break;

            case PaperFormat.A5:
                PaperWidth = mmToInch(148.0);
                PaperHeight = mmToInch(210.0);
                break;

            case PaperFormat.A6:
                PaperWidth = mmToInch(105.0);
                PaperHeight = mmToInch(148.0);
                break;

            case PaperFormat.FitPageToContent:
                PreferCSSPageSize = true;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(paperFormat), paperFormat, null);
        }
    }
    #endregion

    #region Clone
    /// <summary>
    ///     Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>A new object that is a copy of this instance.</returns>
    public object Clone()
    {
        return new PageSettings(PaperFormat)
        {
            Landscape = Landscape,
            DisplayHeaderFooter = DisplayHeaderFooter,
            HeaderTemplate = HeaderTemplate,
            FooterTemplate = FooterTemplate,
            PrintBackground = PrintBackground,
            Scale = Scale,
            PaperWidth = PaperWidth,
            PaperHeight = PaperHeight,
            MarginTop = MarginTop,
            MarginBottom = MarginBottom,
            MarginLeft = MarginLeft,
            MarginRight = MarginRight,
            PageRanges = PageRanges,
            PreferCSSPageSize = PreferCSSPageSize,
            TaggedPdf = TaggedPdf,
            GenerateOutline = GenerateOutline
        };
    }
    #endregion
}
