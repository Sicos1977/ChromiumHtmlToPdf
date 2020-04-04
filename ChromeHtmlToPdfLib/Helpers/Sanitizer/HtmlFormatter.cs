using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;

namespace ChromeHtmlToPdfLib.Helpers.Sanitizer
{
    /// <summary>
    ///     HTML5 markup formatter. Identical to <see cref="HtmlMarkupFormatter" /> except for &lt; and &gt; which are
    ///     encoded in attribute values.
    /// </summary>
    internal class HtmlFormatter : IMarkupFormatter
    {
        /// <summary>
        ///     An instance of <see cref="HtmlFormatter" />.
        /// </summary>
        public static readonly HtmlFormatter Instance = new HtmlFormatter();

        // disable XML comments warnings
#pragma warning disable 1591

        public virtual string Attribute(IAttr attribute)
        {
            var namespaceUri = attribute.NamespaceUri;
            var localName = attribute.LocalName;
            var value = attribute.Value;
            var temp = new StringBuilder();

            if (string.IsNullOrEmpty(namespaceUri))
                temp.Append(localName);
            else if (namespaceUri == NamespaceNames.XmlUri)
                temp.Append(NamespaceNames.XmlPrefix).Append(':').Append(localName);
            else if (namespaceUri == NamespaceNames.XLinkUri)
                temp.Append(NamespaceNames.XLinkPrefix).Append(':').Append(localName);
            else if (namespaceUri == NamespaceNames.XmlNsUri)
                temp.Append(XmlNamespaceLocalName(localName));
            else
                temp.Append(attribute.Name);

            temp.Append('=').Append('"');

            foreach (var t in value)
                switch (t)
                {
                    case '&':
                        temp.Append("&amp;");
                        break;
                    case '\u00a0':
                        temp.Append("&nbsp;");
                        break;
                    case '"':
                        temp.Append("&quot;");
                        break;
                    case '<':
                        temp.Append("&lt;");
                        break;
                    case '>':
                        temp.Append("&gt;");
                        break;
                    default:
                        temp.Append(t);
                        break;
                }

            return temp.Append('"').ToString();
        }

        private static string XmlNamespaceLocalName(string name)
        {
            return name != NamespaceNames.XmlNsPrefix ? NamespaceNames.XmlNsPrefix + ":" : name;
        }

        public virtual string CloseTag(IElement element, bool selfClosing)
        {
            return HtmlMarkupFormatter.Instance.CloseTag(element, selfClosing);
        }

        /// <summary>Formats the given text.</summary>
        /// <param name="text">The text to sanatize.</param>
        /// <returns>The formatted text.</returns>
        public string Text(ICharacterData text)
        {
            return HtmlMarkupFormatter.Instance.Text(text);
        }

        public virtual string Comment(IComment comment)
        {
            return HtmlMarkupFormatter.Instance.Comment(comment);
        }

        public virtual string Doctype(IDocumentType doctype)
        {
            return HtmlMarkupFormatter.Instance.Doctype(doctype);
        }

        public virtual string OpenTag(IElement element, bool selfClosing)
        {
            var temp = new StringBuilder();

            temp.Append('<');

            if (!string.IsNullOrEmpty(element.Prefix)) temp.Append(element.Prefix).Append(':');

            temp.Append(element.LocalName);

            foreach (var attribute in element.Attributes) temp.Append(' ').Append(Attribute(attribute));

            temp.Append('>');

            return temp.ToString();
        }

        public virtual string Processing(IProcessingInstruction processing)
        {
            return HtmlMarkupFormatter.Instance.Processing(processing);
        }

        public string LiteralText(ICharacterData text)
        {
            throw new System.NotImplementedException();
        }
#pragma warning restore 1591
    }
}