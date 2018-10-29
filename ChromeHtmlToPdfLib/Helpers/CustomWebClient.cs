using System;
using System.Net;

namespace ChromeHtmlToPdfLib.Helpers
{
    internal class CustomWebClient : WebClient
    {   
        #region Properties
        /// <summary>
        /// Gets or sets the lengt of time, in milliseconds, before the request timesout
        /// </summary>
        public int Timeout { get; set; }
        #endregion

        #region GetWebRequest
        protected override WebRequest GetWebRequest(Uri uri)
        {
            var webRequest = base.GetWebRequest(uri);
            // ReSharper disable once PossibleNullReferenceException
            webRequest.Timeout = Timeout;
            ((HttpWebRequest)webRequest).ReadWriteTimeout = Timeout;
            return webRequest;
        }
        #endregion
    }
}
