using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChromeHtmlToPdf
{
    /// <summary>
    /// A wrapper class around Google Chrome to convert HTML to PDF
    /// </summary>
    public class Converter
    {
        #region Properties
        /// <summary>
        /// Run Google Chrome without a Windows
        /// </summary>
        public bool HeadLess { get; set; } = true;

        /// <summary>
        /// Disable the GPU when converting HTML to PDF
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool DisableGpu { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool Incognito { get; set; } = true;

        /// <summary>
        /// Enable the viewport (Default <c>true</c>)
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool EnableViewPort { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public bool DisableTranslate { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool DisableExtensions { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool DisableBackGroundNetworking { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool SafeBrowsingDisableAutoUpdate { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool DisableSync { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        public bool MetricsRecordingOnly { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool DisableDefaultApps { get; set; } = true;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Default set to <c>true</c>
        /// </remarks>
        public bool NoFirstRun { get; set; } = true;

        #endregion
    }
}
/*
    --headless \
    --disable-gpu \
    --disable-translate \
    --disable-extensions \
    --disable-background-networking \
    --safebrowsing-disable-auto-update \
    --disable-sync \
    --metrics-recording-only \
    --disable-default-apps \
    --no-first-run \
    --remote-debugging-port=<port goes here>
    --print-to-pdf=
 */
