using System;
using System.Diagnostics;
using System.Reflection;

namespace ChromeHtmlToPdfLib
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

            var s = assembly.FullName.Split(',');

            var reflectedType = stackframe.GetMethod().ReflectedType;
            if (reflectedType != null)
                return s[0] + "::" +
                       reflectedType.Name
                       + "."
                       + stackframe.GetMethod().Name + "()";

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