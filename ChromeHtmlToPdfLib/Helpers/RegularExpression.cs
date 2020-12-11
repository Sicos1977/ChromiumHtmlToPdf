//
// RegularExpression.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017-2019 Magic-Sessions. (www.magic-sessions.com)
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
using System.Text.RegularExpressions;

namespace ChromeHtmlToPdfLib.Helpers
{
    internal static class RegularExpression
    {
        #region IsRegExMatch
        /// <summary>
        /// Returns <c>true</c> when a match in <paramref name="patterns"/> has been
        /// found for <paramref name="value"/>
        /// </summary>
        /// <param name="patterns">A list with regular expression</param>
        /// <param name="value">The string where to find the match</param>
        /// <param name="matchedPattern"></param>
        /// <returns></returns>
        public static bool IsRegExMatch(IEnumerable<string> patterns, string value, out string matchedPattern)
        {
            matchedPattern = string.Empty;

            if (patterns == null)
                return false;

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase))
                {
                    matchedPattern = Regex.Unescape(pattern);
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
