using CanvasAPI;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildUserInfo
{
    [JsonIgnore] public GuildInfo GuildInfo { get; private set; }
    [JsonProperty] public ulong UserId { get; private set; }

    [JsonProperty] public string? Token { get; private set; }

    [JsonIgnore] private DiscordSocketClient _client;
    
    [JsonIgnore] public SocketGuildUser? GuildUser => GuildInfo.Guild.GetUser(UserId);

    [JsonConstructor]
    public GuildUserInfo() { }

    public async Task<GuildCourseInfo[]?> GetCourses()
    {
        CanvasClient? canvasClient = CreateCanvasClient();
        if(canvasClient == null) return null;

        Course[]? canvasCourses = await canvasClient.GetAllCourses();
        if(canvasCourses == null) return null;
        
        List<GuildCourseInfo> courses = new List<GuildCourseInfo>();
        foreach (Course course in canvasCourses)
        {
            courses.Add(await GuildInfo.GetOrCreateCourseInfo(course));
        }
        
        return courses.ToArray();
    }

    public async Task<GuildCourseInfo?> GetCourseById(string id)
    {
        CanvasClient? canvasClient = CreateCanvasClient();
        if(canvasClient == null) return null;

        Course? course = await canvasClient.GetNode<Course>(id);
        if (course != null) return await GuildInfo.GetOrCreateCourseInfo(course);
        return null;
    }
    
    public GuildUserInfo(DiscordSocketClient client, ulong userId, GuildInfo guildInfo)
    {
        _client = client;
        UserId = userId;
        GuildInfo = guildInfo;
    }

    public void Initialize(DiscordSocketClient client, GuildInfo guild)
    {
        _client = client;
        GuildInfo = guild;
    }

    public async Task Refresh()
    {
        await RefreshCourses();
    }

    private async Task<bool> RefreshCourses()
    {
        GuildCourseInfo[]? courses = await GetCourses();
        SocketGuildUser? guildUser = GuildUser;
        if (courses == null || guildUser == null) return false;

        foreach (GuildCourseInfo course in courses)
        {
            if (guildUser.Roles.Count(role => role.Id == course.RoleId) == 0)
            {
                await guildUser.AddRoleAsync(course.RoleId);
            }

            if (Token != null)
            {
                if (!course.HasToken(Token))
                {
                    course.AddToken(Token);
                }
            }
        }

        return true;
    }

    public CanvasClient? CreateCanvasClient()
    {
        if(GuildInfo.CanvasUrl == null || Token == null) return null;
        return new CanvasClient(GuildInfo.CanvasUrl.ToString(), Token);
    }

    public async Task<bool> TrySetToken(string token)
    {
        Token = token;
        
        GuildCourseInfo[]? courses = await GetCourses();
        if (courses == null)
        {
            Token = null;
            return false;
        }

        await RefreshCourses();
        return true;
    }
}