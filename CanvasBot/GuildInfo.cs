using CanvasAPI;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildInfo
{
    [JsonProperty] public ulong GuildId { get; private set; }
    [JsonProperty] public Uri? CanvasUrl { get; private set; }
    [JsonProperty] public Dictionary<ChannelType, ulong> Channels { get; private set; } = new();
    [JsonProperty] private Dictionary<ulong, GuildUserInfo> Users { get; set; } = new();
    [JsonProperty] private Dictionary<string, GuildCourseInfo> Courses { get; set; } = new();

    public GuildInfo(ulong guildId)
    {
        GuildId = guildId;
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

    public GuildUserInfo GetUserInfo(ulong userId)
    {
        if (Users.TryGetValue(userId, out var user)) return user;
        return AddUser(userId);
    }

    private GuildUserInfo AddUser(ulong userId)
    {
        GuildUserInfo user = new GuildUserInfo(userId, this);
        Users.Add(userId, user);
        return user;
    }

    private GuildCourseInfo AddCourse(string courseId)
    {
        GuildCourseInfo course = new GuildCourseInfo(courseId);
        Courses.Add(courseId, course);
        return course;
    }

    public GuildCourseInfo GetCourseInfo(string courseId)
    {
        if(Courses.TryGetValue(courseId, out var course)) return course;
        return AddCourse(courseId);
    }
    
    public GuildCourseInfo[] GetCourses() => Courses.Values.ToArray();

    public CanvasClient? CreateCanvasClient(string token)
    {
        Console.WriteLine($"Creating canvas client: {token}");
        if (CanvasUrl == null) return null;
        return new CanvasClient(CanvasUrl.ToString(), token);
    }

    public void SetUsersGuild()
    {
        foreach (var user in Users.Values)
        {
            user.Guild = this;
        }
    }
}