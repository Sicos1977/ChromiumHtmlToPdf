using System.Collections.Generic;

namespace ChromeHtmlToPdfLib.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Implement's VB's Like operator logic.
        /// </summary>
        public static bool IsLike(this string s, string pattern, bool caseInsensitive)
        {
            if (caseInsensitive)
            {
                s = s.ToLowerInvariant();
                pattern = pattern.ToLowerInvariant();
            }

            // Characters matched so far
            var matched = 0;

            // Loop through pattern string
            for (var i = 0; i < pattern.Length;)
            {
                // Check for end of string
                if (matched > s.Length)
                    return false;

                // Get next pattern character
                var c = pattern[i++];

                if (c == '[') // Character list
                {
                    // Test for exclude character
                    var exclude = (i < pattern.Length && pattern[i] == '!');
                    if (exclude)
                        i++;
                    
                    // Build character list
                    var j = pattern.IndexOf(']', i);
                    if (j < 0)
                        j = s.Length;
                    
                    var charList = CharListToSet(pattern.Substring(i, j - i));
                    i = j + 1;

                    if (charList.Contains(s[matched]) == exclude)
                        return false;
                    matched++;
                }
                else if (c == '?') // Any single character
                {
                    matched++;
                }
                else if (c == '#') // Any single digit
                {
                    if (!char.IsDigit(s[matched]))
                        return false;
                    matched++;
                }
                else if (c == '*') // Zero or more characters
                {
                    if (i < pattern.Length)
                    {
                        // Matches all characters until
                        // next character in pattern
                        var next = pattern[i];
                        var j = s.IndexOf(next, matched);
                        if (j < 0)
                            return false;
                        matched = j;
                    }
                    else
                    {
                        // Matches all remaining characters
                        matched = s.Length;
                        break;
                    }
                }
                else // Exact character
                {
                    if (matched >= s.Length || c != s[matched])
                        return false;
                    matched++;
                }
            }

            // Return true if all characters matched
            return (matched == s.Length);
        }

        #region CharListToSet
        /// <summary>
        /// Converts a string of characters to a HashSet of characters. If the string
        /// contains character ranges, such as A-Z, all characters in the range are
        /// also added to the returned set of characters.
        /// </summary>
        /// <param name="charList">Character list string</param>
        private static HashSet<char> CharListToSet(string charList)
        {
            var set = new HashSet<char>();

            for (var i = 0; i < charList.Length; i++)
            {
                if ((i + 1) < charList.Length && charList[i + 1] == '-')
                {
                    // Character range
                    var startChar = charList[i++];
                    i++; // Hyphen
                    var endChar = (char) 0;
                    if (i < charList.Length)
                        endChar = charList[i++];
                    for (int j = startChar; j <= endChar; j++)
                        set.Add((char) j);
                }
                else set.Add(charList[i]);
            }

            return set;
        }
        #endregion
    }
}
