using CanvasAPI;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildUserInfo
{
    [JsonIgnore] public GuildInfo Guild { get; set; }
    public ulong UserId { get; private set; }
    public string? Token { get; set; }
    
    public GuildUserInfo(ulong userId, GuildInfo guild)
    {
        UserId = userId;
        Guild = guild;
    }

    public CanvasClient? CreateCanvasClient()
    {
        if(Guild.CanvasUrl == null || Token == null) return null;
        return new CanvasClient(Guild.CanvasUrl.ToString(), Token);
    }
}