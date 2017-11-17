//
// RemoteSession.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2017 Magic-Sessions. (www.magic-sessions.com)
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChromeHtmlToPdfLib.Protocol
{
    /// <summary>
    /// The JSON object that is returned from Chrome when calling the <see cref="Communicator.GetAvailableSessions"/> method
    /// </summary>
    public class RemoteSessionsResponse
    {
        #region Properties
        /// <summary>
        /// The message id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        
        /// <summary>
        /// A description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// The url to use when we want to debug the Chrome session from Chrome itself
        /// </summary>
        [JsonProperty("devtoolsFrontendUrl")]
        public string DevtoolsFrontendUrl { get; set; }

        /// <summary>
        /// The title from the Chrome screen
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// The type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The url that is currently viewed
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// The websocket url to use when sending commands to Chrome
        /// </summary>
        [JsonProperty("webSocketDebuggerUrl")]
        public string WebSocketDebuggerUrl { get; set; }
        #endregion

        #region FromJson
        /// <summary>
        /// Returns this object deserialized from the given <paramref name="json"/> string
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<RemoteSessionsResponse> FromJson(string json)
        {
            return JsonConvert.DeserializeObject<List<RemoteSessionsResponse>>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            });
        }
        #endregion
    }
}
