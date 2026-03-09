using Newtonsoft.Json;

namespace CanvasBot;

public class GuildInfo
{
    [JsonProperty] public ulong GuildId { get; private set; }
    [JsonProperty] public Uri? CanvasUrl { get; private set; }
    [JsonProperty] private Dictionary<ulong, GuildUserInfo> Users { get; set; } = new();

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
        GuildUserInfo user = new GuildUserInfo(userId);
        Users.Add(userId, user);
        return user;
    }
}