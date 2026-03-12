using HtmlAgilityPack;

namespace CanvasBot;

public static class HtmlUtils
{
    public static string GetHtmlText(string html)
    {
        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(html);
        return GetHtmlText(document.DocumentNode);
    }
    
    public static string GetHtmlText(HtmlDocument document) => GetHtmlText(document.DocumentNode);
    
    public static string GetHtmlText(HtmlNode node)
    {
        string htmlText = "";
        
        if (!node.HasChildNodes) return htmlText + node.InnerText;

        foreach (HtmlNode childNode in node.ChildNodes)
        {
            htmlText += GetHtmlText(childNode);
        }

        return ProcessHtmlText(node, htmlText);
    }

    private static string ProcessHtmlText(HtmlNode node, string htmlText)
    {
        switch (node.Name)
        {
            case "strong":
                htmlText = "**" +  htmlText + "**";
                break;
            case "li":
                if (node.ParentNode.Name == "ul")
                {
                    if(htmlText.StartsWith("\n")) htmlText = htmlText.Substring(1);
                    htmlText = "* " + htmlText;
                }
                break;
        }
        return htmlText.Replace("&nbsp;", " ");
    }
}