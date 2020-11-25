//
// CountdownTimer.cs
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

using System.Diagnostics;

namespace ChromeHtmlToPdfLib.Helpers
{
    internal class CountdownTimer
    {
        #region Fields
        private readonly Stopwatch _stopwatch;
        private readonly int _timeoutMilliseconds;
        #endregion

        #region Properties
        /// <summary>
        ///     Returns the milliseconds that are left before the countdown reaches zero
        /// </summary>
        public int MillisecondsLeft
        {
            get
            {
                if (!_stopwatch.IsRunning)
                    return 0;

                var value = _timeoutMilliseconds - (int) _stopwatch.ElapsedMilliseconds;
                return value <= 0 ? 0 : value;
            }
        }

        /// <summary>
        ///     Returns <c>true</c> when the countdown timer is running
        /// </summary>
        public bool IsRunning => _stopwatch.IsRunning;
        #endregion

        #region Constructor
        /// <summary>
        ///     Makes this object and sets the timeout in milliseconds
        /// </summary>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds</param>
        internal CountdownTimer(int timeoutMilliseconds)
        {
            _stopwatch = new Stopwatch();
            _timeoutMilliseconds = timeoutMilliseconds;
        }
        #endregion

        #region Reset
        /// <summary>
        ///     Stops the countdown and reset 
        /// </summary>
        public void Reset()
        {
            _stopwatch.Reset();
        }
        #endregion

        #region Restart
        /// <summary>
        ///     Stops time interval measurement, resets the elapsed time to zero, and starts measuring elapsed time.
        /// </summary>
        public void Restart()
        {
            _stopwatch.Restart();
        }
        #endregion

        #region Start
        /// <summary>
        ///     Starts, or resumes, measuring elapsed time for an interval.
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
        }
        #endregion

        #region Stop
        /// <summary>
        ///     Stops measuring elapsed time for an interval.
        /// </summary>
        public void Stop()
        {
            _stopwatch.Stop();
        }
        #endregion
    }
}