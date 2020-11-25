//
// ExceptionHelper.cs
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

using System;
using System.Diagnostics;
using System.Reflection;

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    ///     Exception helper methods
    /// </summary>
    public static class ExceptionHelpers
    {
        #region GetCurrentMethodName
        /// <summary>
        ///     Returns the last called method
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentMethodName(Assembly assembly)
        {
            if (assembly == null) return string.Empty;

            var stackframe = new StackFrame(2, true);

            if (assembly.FullName != null)
            {
                var s = assembly.FullName.Split(',');

                var reflectedType = stackframe.GetMethod()?.ReflectedType;
                if (reflectedType != null)
                    return s[0] + "::" +
                           reflectedType.Name
                           + "."
                           + stackframe.GetMethod()?.Name + "()";
            }

            return string.Empty;
        }
        #endregion

        #region GetInnerException
        /// <summary>
        ///     Returns the full exception with it's inner exceptions as a string
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <returns></returns>
        public static string GetInnerException(Exception exception)
        {
            var result = string.Empty;

            if (exception == null) return result;
            result = exception.Message + Environment.NewLine;
            if (exception.InnerException != null)
                result += GetInnerException(exception.InnerException);
            return result;
        }
        #endregion
    }
}