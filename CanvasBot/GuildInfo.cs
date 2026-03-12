using CanvasAPI;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildInfo
{
    [JsonProperty] public ulong GuildId { get; private set; }
    [JsonProperty] public Uri? CanvasUrl { get; private set; }
    [JsonProperty] public Dictionary<ChannelType, ulong> Channels { get; private set; } = new();
    [JsonProperty] private Dictionary<ulong, GuildUserInfo> Users { get; set; } = new();
    [JsonProperty] private Dictionary<string, GuildCourseInfo> Courses { get; set; } = new();
    
    [JsonIgnore] private DiscordSocketClient _client;
    [JsonIgnore] public SocketGuild Guild => _client.GetGuild(GuildId);

    [JsonConstructor]
    private GuildInfo() { }
    
    public GuildInfo(DiscordSocketClient client, SocketGuild guild)
    {
        GuildId = guild.Id;
        _client = client;
    }

    public void Initialize(DiscordSocketClient client)
    {
        _client = client;
        foreach (GuildUserInfo user in Users.Values)
        {
            user.Initialize(_client, this);
        }
        foreach (GuildCourseInfo course in Courses.Values)
        {
            course.Initialize(_client, this);
        }
    }

    public async Task Refresh()
    {
        foreach (GuildUserInfo user in Users.Values)
        {
            await user.Refresh();
        }
    }

    public bool SetCanvasUrl(string canvasUrl)
    {
        if (Uri.TryCreate(canvasUrl, UriKind.Absolute, out Uri? uri))
        {
            if (uri.HostNameType != UriHostNameType.Dns) return false;
            CanvasUrl = uri;
            return true;
        }
        return false;
    }
    
    public GuildUserInfo[] GetUsers() => Users.Values.ToArray();

    public GuildUserInfo GetOrCreateUserInfo(ulong userId)
    {
        if (Users.TryGetValue(userId, out var user)) return user;
        return AddUser(userId);
    }

    private GuildUserInfo AddUser(ulong userId)
    {
        GuildUserInfo user = new GuildUserInfo(_client, userId, this);
        Users.Add(userId, user);
        return user;
    }

    public GuildCourseInfo? GetCourseById(string courseId) => Courses.GetValueOrDefault(courseId);

    public async Task<GuildCourseInfo> GetOrCreateCourseInfo(Course course)
    {
        if(Courses.TryGetValue(course.Id, out var courseInfo)) return courseInfo;
        return await CreateCourseInfo(course);
    }

    private async Task<GuildCourseInfo> CreateCourseInfo(Course course)
    {
        Color color = GuildCourseInfo.DefaultColors[new Random().Next(GuildCourseInfo.DefaultColors.Length)];
        RestRole role = await Guild.CreateRoleAsync(course.courseCode, color: color);
        Dictionary<string, Discussion>? announcements = await course.GetDiscussions(last: 1);
        string? lastCursor = announcements?.Keys.FirstOrDefault();
        
        GuildCourseInfo courseInfo = new GuildCourseInfo(_client, this, course, color, role, lastCursor);
        Courses.Add(course.Id, courseInfo);

        return courseInfo;
    }

    public async Task<Dictionary<GuildCourseInfo, Discussion[]>> GetNewAnnouncements()
    {
        Dictionary<GuildCourseInfo, Discussion[]> newAnnouncements = new();
        foreach (GuildCourseInfo courseInfo in Courses.Values)
        {
            Discussion[]? courseAnnouncements = await courseInfo.GetNewAnnouncements();
            if(courseAnnouncements == null) continue;
            newAnnouncements.Add(courseInfo, courseAnnouncements);
        }
        return newAnnouncements;
    }
    
    public GuildCourseInfo[] GetCourses() => Courses.Values.ToArray();

    public CanvasClient? CreateCanvasClient(string token)
    {
        Console.WriteLine($"Creating canvas client: {token}");
        if (CanvasUrl == null) return null;
        return new CanvasClient(CanvasUrl.ToString(), token);
    }
}