using CanvasAPI;
using Discord;

namespace CanvasBot;

public static class AnnouncementUtils
{
    public static async Task<Embed?> CreateAnnouncementEmbed(GuildCourseInfo courseInfo, Discussion announcement)
    {
        User? author = await announcement.GetAuthor();
        string? message = await announcement.GetMessage();
        string title = announcement.title;
        if (author == null || message == null) return null;
                
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle(courseInfo.Nickname);
        embed.WithColor(courseInfo.Color);
        embed.WithAuthor(author.name, author.avatarUrl);
        embed.WithDescription(CreateAnnouncementBody(title, message, announcement.Link));
        embed.WithFooter("Canvas",
            "https://du11hjcvx0uqb.cloudfront.net/dist/images/canvas_logomark_only@2x-e197434829.png");
                
        if (announcement.postedAt != null) embed.WithTimestamp(announcement.postedAt.Value);

        return TruncateEmbed(embed).Build();
    }

    private static string CreateAnnouncementBody(string title, string htmlMessage, string url)
    {
        string messageText = HtmlUtils.GetHtmlText(htmlMessage);
        return TruncateString($"### [{title}]({url})\n\n{messageText}", EmbedBuilder.MaxDescriptionLength);
    }

    private static EmbedBuilder TruncateEmbed(EmbedBuilder embed)
    {
        int totalCharacters = 0;
        
        embed.Title = TruncateString(embed.Title, EmbedBuilder.MaxTitleLength);
        embed.Description = TruncateString(embed.Description, EmbedBuilder.MaxDescriptionLength);
        totalCharacters += embed.Title.Length;
        totalCharacters += embed.Description.Length;
        
        if (embed.Author != null)
        {
            embed.Author.Name = TruncateString(embed.Author.Name, EmbedAuthorBuilder.MaxAuthorNameLength);
            totalCharacters += embed.Author.Name.Length;
        }

        if (embed.Footer != null)
        {
            embed.Footer.Text = TruncateString(embed.Footer.Text, EmbedFooterBuilder.MaxFooterTextLength);
            totalCharacters += embed.Footer.Text.Length;
        }
        
        if(totalCharacters > 6000) embed.Title = TruncateString(embed.Description, 6000 - embed.Title.Length - embed.Author.Name.Length -embed.Footer.Text.Length);
        Console.WriteLine(totalCharacters);
        return embed;
    }

    private static string TruncateString(string? str, int maxLength)
    {
        if (str == null) return "";
        if(str.Length > maxLength) return str.Substring(0, maxLength - 3) + "...";
        return str;
    }
}