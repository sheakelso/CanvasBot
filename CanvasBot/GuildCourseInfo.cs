using CanvasAPI;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildCourseInfo
{
    public static readonly Color[] DefaultColors = new[]
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
    public string Nickname { get; set; }
    public string ColorHex { get; set; }
    public string? LastAnnouncementCursor { get; set; }
    public ulong RoleId { get; set; }
    
    [JsonProperty] private List<string> Tokens { get; set; } = new();
    
    [JsonIgnore] public Color Color => Color.Parse(ColorHex);
    
    [JsonIgnore] private DiscordSocketClient _client;

    [JsonIgnore] public GuildInfo GuildInfo;

    [JsonConstructor]
    public GuildCourseInfo() { }
    
    public async Task<Course?> GetCourse()
    {
        Uri? canvasUri = GuildInfo.CanvasUrl;
        if (canvasUri != null)
        {
            return await new CanvasClient(canvasUri.ToString(), GetRandomToken()).GetNode<Course>(CourseId);
        }
        return null;
    }

    public GuildCourseInfo(DiscordSocketClient client, GuildInfo guildInfo, Course course, Color color, IRole role,  string? lastAnnouncementCursor)
    {
        _client = client;
        GuildInfo = guildInfo;
        CourseId = course.Id;
        Nickname = course.name;
        ColorHex = color.ToString();
        RoleId = role.Id;
        LastAnnouncementCursor = lastAnnouncementCursor;
    }

    public void Initialize(DiscordSocketClient client, GuildInfo guildInfo)
    {
        _client = client;
        GuildInfo = guildInfo;
    }
    
    public string GetRandomToken()
    {
        return Tokens[new Random().Next(Tokens.Count)];
    }

    public void AddToken(string token)
    {
        Tokens.Add(token);
    }
    
    public CanvasClient? CreateCanvasClient()
    {
        if(Tokens.Count == 0) return null;
        Uri? canvasUri = GuildInfo.CanvasUrl;
        if (canvasUri != null) return new CanvasClient(canvasUri.ToString(), GetRandomToken());
        return null;
    }

    public async Task<Discussion[]?> GetNewAnnouncements()
    {
        CanvasClient? client = CreateCanvasClient();
        if(client == null) return null;

        Course? course = await GetCourse();
        if(course == null) return null;

        Dictionary<string, Discussion>? discussions = await course.GetDiscussions(LastAnnouncementCursor);
        if(discussions == null) return null;

        if(discussions.Count > 0) LastAnnouncementCursor = discussions.Last().Key;
        return discussions.Values.ToArray();
    }
    
    public bool HasToken(string token) => Tokens.Contains(token); 
}