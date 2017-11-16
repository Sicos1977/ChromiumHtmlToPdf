using System;
using System.Collections.Generic;
using System.Linq;

namespace ChromeHtmlToPdfLib
{
    internal static class Extensions
    {
        #region Contains
        /// <summary>
        ///     Returns <c>true</c> when the list containts the given <paramref name="source" />
        /// </summary>
        /// <param name="source"></param>
        /// <param name="value">The value to check if it exists in the list</param>
        /// <param name="comparison">
        ///     <see cref="StringComparison" />
        /// </param>
        /// <returns></returns>
        public static bool Contains(this List<string> source, string value, StringComparison comparison)
        {
            return
                source != null &&
                !string.IsNullOrEmpty(value) &&
                source.Any(x => string.Compare(x, value, comparison) == 0);
        }
        #endregion

        #region Replace
        /// <summary>
        ///     Replaces the first occurence of the <paramref name="oldValue" /> with the <paramref name="newValue" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="newValue">The new value</param>
        /// <returns></returns>
        public static int Replace<T>(this IList<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var index = source.IndexOf(oldValue);
            if (index != -1)
                source[index] = newValue;
            return index;
        }

        /// <summary>
        ///     Replaces the first occurence of the <paramref name="oldValue" /> with the <paramref name="newValue" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="newValue">The new value</param>
        /// <returns></returns>
        public static IEnumerable<T> Replace<T>(this IEnumerable<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Select(x => EqualityComparer<T>.Default.Equals(x, oldValue) ? newValue : x);
        }

        /// <summary>
        ///     Replaces all the <paramref name="oldValue" /> with the <paramref name="newValue" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="oldValue">The value to replace</param>
        /// <param name="newValue">The new value</param>
        public static void ReplaceAll<T>(this IList<T> source, T oldValue, T newValue)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int index;

            do
            {
                index = source.IndexOf(oldValue);
                if (index != -1)
                    source[index] = newValue;
            } while (index != -1);
        }
        #endregion
    }
}