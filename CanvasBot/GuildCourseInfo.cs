using Discord;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildCourseInfo
{
    private static readonly Color[] DefaultColors = new[]
    {
        Color.Blue,
        Color.Red,
        Color.Green,
        Color.Gold,
        Color.Magenta,
        Color.Orange,
        Color.Purple,
        Color.Teal
    };
    
    public string CourseId { get; set; }
    public string? Nickname { get; set; }
    public Color Color { get; set; }
    public string? LastAnnouncementCursor { get; set; }
    [JsonProperty] private List<string> Tokens { get; set; } = new();

    public GuildCourseInfo(string courseId)
    {
        CourseId = courseId;
        Color = DefaultColors[new Random().Next(DefaultColors.Length)];
    }
    
    public string GetToken()
    {
        return Tokens[new Random().Next(Tokens.Count)];
    }

    public void AddToken(string token)
    {
        Tokens.Add(token);
    }
    
    public bool HasToken(string token) => Tokens.Contains(token); 
}