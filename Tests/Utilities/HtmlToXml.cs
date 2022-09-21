using HtmlAgilityPack;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace Tests.Utilities;

public static class HtmlToXml
{
    public static void CleanUpAndWriteTo(this HtmlNode node, XmlWriter writer)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Document:
                if (node.HasChildNodes)
                    foreach (var childNode in node.ChildNodes)
                        childNode.CleanUpAndWriteTo(writer);
                break;
            case HtmlNodeType.Element:
                writer.WriteStartElement(node.OriginalName);
                node.WriteAttributes(writer);
                if (node.HasChildNodes)
                    foreach (var childNode in node.ChildNodes)
                        childNode.CleanUpAndWriteTo(writer);
                writer.WriteEndElement();
                break;
            case HtmlNodeType.Comment:
                writer.WriteComment(((HtmlCommentNode)node).GetXmlComment());
                break;
            case HtmlNodeType.Text:
                writer.WriteString(((HtmlTextNode)node).Text.CleanUp());
                break;
            default:
                throw new ArgumentOutOfRangeException(node.NodeType.ToString());
        }
    }

    private static void WriteAttributes(this HtmlNode node, XmlWriter writer)
    {
        if (!node.HasAttributes) return;
        foreach (var htmlAttribute in node.Attributes)
            writer.WriteAttributeString(
                GetXmlName(htmlAttribute.Name, true, true),
                WebUtility.HtmlDecode(htmlAttribute.Value));
    }

    private static string GetXmlName(string name, bool isAttribute, bool preserveXmlNamespaces)
    {
        string empty = string.Empty;
        bool flag = true;
        for (int index = 0; index < name.Length; ++index)
        {
            if (name[index] >= 'a' && name[index] <= 'z' ||
                name[index] >= 'A' && name[index] <= 'Z' ||
                name[index] >= '0' && name[index] <= '9' ||
                isAttribute | preserveXmlNamespaces && name[index] == ':' ||
                name[index] == '_' ||
                name[index] == '-' ||
                name[index] == '.')
            {
                empty += name[index].ToString();
            }
            else
            {
                flag = false;
                var utF8 = Encoding.UTF8;
                char[] chars = { name[index] };
                foreach (var num in utF8.GetBytes(chars))
                    empty += num.ToString("x2");
                empty += "_";
            }
        }
        return flag ? empty : "_" + empty;
    }

    private static string GetXmlComment(this HtmlCommentNode comment)
    {
        string comment1 = comment.Comment;
        return comment1.Substring(4, comment1.Length - 7).Replace("--", " - -");
    }

    private static void AddAttributesTo(this HtmlNode node, XElement target)
    {
        if (!node.HasAttributes) return;
        foreach (var htmlAttribute in node.Attributes)
            target.Add(new XAttribute(
                GetXmlName(htmlAttribute.Name, true, true),
                WebUtility.HtmlDecode(htmlAttribute.Value)));
    }

    private static void CleanUpTo(this HtmlNode node, XElement target)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Document:
                if (node.HasChildNodes)
                    foreach (var childNode in node.ChildNodes)
                        childNode.CleanUpTo(target);
                break;
            case HtmlNodeType.Element:
                var xElement = new XElement(node.OriginalName);
                node.AddAttributesTo(xElement);
                if (node.HasChildNodes)
                    foreach (var childNode in node.ChildNodes)
                        childNode.CleanUpTo(xElement);
                target.Add(xElement);
                break;
            case HtmlNodeType.Comment:
                target.Add(new XComment(GetXmlComment(((HtmlCommentNode)node))));
                break;
            case HtmlNodeType.Text:
                target.Add(new XText(((HtmlTextNode)node).Text.CleanUp()));
                break;
            default:
                throw new ArgumentOutOfRangeException(node.NodeType.ToString());
        }
    }

    public static XNode CleanUpToXml(this HtmlNode node)
    {
        var raw = ConvertRecursive(node);
        var wrapper = raw as XElement;
        return wrapper?.Nodes().Count() == 1 && wrapper.Name == "x"
            ? wrapper.FirstNode ?? raw
            : raw;

        static XNode ConvertRecursive(HtmlNode node)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Document:
                    return new XElement("x", node.ChildNodes.Select(ConvertRecursive));
                case HtmlNodeType.Element:
                    var xElement = new XElement(node.OriginalName);
                    node.AddAttributesTo(xElement);
                    if (node.HasChildNodes)
                        foreach (var childNode in node.ChildNodes)
                            childNode.CleanUpTo(xElement);
                    return xElement;
                case HtmlNodeType.Comment:
                    return new XComment(GetXmlComment(((HtmlCommentNode)node)));
                case HtmlNodeType.Text:
                    return new XText(((HtmlTextNode)node).Text.CleanUp());
                default:
                    throw new ArgumentOutOfRangeException(node.NodeType.ToString());
            }
        }
    }
}