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
