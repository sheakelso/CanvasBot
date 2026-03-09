using Discord;

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
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public GuildCourseInfo(string courseId)
    {
        CourseId = courseId;
        Color = DefaultColors[new Random().Next(DefaultColors.Length)];
    }
}