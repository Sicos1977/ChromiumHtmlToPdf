using System;
using System.Runtime.Serialization;
using System.Text;

namespace ChromeHtmlToPdfLib
{
    /// <summary>
    /// <inheritdoc cref="Uri"/>
    /// </summary>
    public class ConvertUri : Uri
    {
        /// <summary>
        ///     The encoding of the file that will be converted
        /// </summary>
        public Encoding Encoding { get; }

        public ConvertUri(string uriString) : base(uriString)
        {
        }

        /// <summary>
        ///     Sets the uri and encoding
        /// </summary>
        /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
        /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
        public ConvertUri(string uriString, Encoding encoding) : base(uriString)
        {
            Encoding = encoding;
        }

        /// <summary>
        ///     Sets the uri and encoding
        /// </summary>
        /// <param name="uriString">The uri, e.g. file://c:\test.txt</param>
        /// <param name="encoding">The encoding used for this file, e.g UTF-8</param>
        public ConvertUri(string uriString, string encoding) : base(uriString)
        {
            Encoding = !string.IsNullOrWhiteSpace(encoding)
                ? Encoding.GetEncoding(encoding)
                : Encoding.UTF8;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public ConvertUri(string uriString, bool dontEscape) : base(uriString, dontEscape)
        {
        }

        public ConvertUri(Uri baseUri, string relativeUri, bool dontEscape) : base(baseUri, relativeUri, dontEscape)
        {
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public ConvertUri(string uriString, UriKind uriKind) : base(uriString, uriKind)
        {
        }

        public ConvertUri(Uri baseUri, string relativeUri) : base(baseUri, relativeUri)
        {
        }

        public ConvertUri(Uri baseUri, Uri relativeUri) : base(baseUri, relativeUri)
        {
        }

        protected ConvertUri(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
