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

        /// <summary>
        ///     Stops the countdown and reset 
        /// </summary>
        public void Reset()
        {
            _stopwatch.Reset();
        }

        /// <summary>
        ///     Stops time interval measurement, resets the elapsed time to zero, and starts measuring elapsed time.
        /// </summary>
        public void Restart()
        {
            _stopwatch.Restart();
        }

        /// <summary>
        ///     Starts, or resumes, measuring elapsed time for an interval.
        /// </summary>
        public void Start()
        {
            _stopwatch.Start();
        }

        /// <summary>
        ///     Stops measuring elapsed time for an interval.
        /// </summary>
        public void Stop()
        {
            _stopwatch.Stop();
        }
    }
}