//
// ChromeFinder.cs
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

//This needs the NuGet package Microsoft.Windows.Compatibility!!!
using Microsoft.Win32;

namespace ChromeHtmlToPdfLib.Helpers
{
    /// <summary>
    /// This class searches for the Chrome or Chromium executables cross-platform.
    /// </summary>
    public static class ChromeFinder
    {
        #region GetApplicationDirectories
        private static void GetApplicationDirectories(ICollection<string> directories)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const string subDirectory = "Google\\Chrome\\Application";
                directories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), subDirectory));
                directories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), subDirectory));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                directories.Add("/usr/local/sbin");
                directories.Add("/usr/local/bin");
                directories.Add("/usr/sbin");
                directories.Add("/usr/bin");
                directories.Add("/sbin");
                directories.Add("/bin");
                directories.Add("/opt/google/chrome");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new Exception("Finding Chrome on MacOS is currently not supported, please contact the programmer.");
        }
        #endregion

        #region GetAppPath
        private static string GetAppPath()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;

            if (appPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return appPath;
            return appPath + Path.DirectorySeparatorChar;
        }
        #endregion

        #region Find
        /// <summary>
        /// Tries to find Chrome
        /// </summary>
        /// <returns></returns>
        public static string Find()
        {
            // For Windows we first check the registry. This is the safest
            // method and also considers non-default installation locations.
			// Note that Chrome x64 currently (February 2019) also gets installed
			// in Program Files (x86) and uses the same registry key!
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var key = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Google Chrome",
                    "InstallLocation", string.Empty);

                if (key != null)
                {
                    var path = Path.Combine(key.ToString(), "chrome.exe");
                    if (File.Exists(path))
                        return path;
                }
            }
            
            // Collect the usual executable names
            var exeNames = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                exeNames.Add("chrome.exe");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                exeNames.Add("google-chrome");
                exeNames.Add("chrome");
                exeNames.Add("chromium");
                exeNames.Add("chromium-browser");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                exeNames.Add("Google Chrome.app/Contents/MacOS/Google Chrome");
                exeNames.Add("Chromium.app/Contents/MacOS/Chromium");
            }
            
            // Check the directory of this assembly/application
            var currentPath = GetAppPath();
                    
            foreach(var exeName in exeNames)
            {
                var path = Path.Combine(currentPath, exeName);
                if (File.Exists(path))
                    return path;
            }

            // Search common software installation directories
            // for the various exe names.

            var directories = new List<string>();

            GetApplicationDirectories(directories);

            foreach(var exeName in exeNames)
            {
                foreach(var directory in directories)
                {
                    var path = Path.Combine(directory, exeName);
                    if (File.Exists(path))
                        return path;
                }
            }

            return null;
        }
        #endregion
    }
}
