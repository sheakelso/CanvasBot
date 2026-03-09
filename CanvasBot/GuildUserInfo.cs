namespace CanvasBot;

public class GuildUserInfo
{
    public ulong UserId { get; private set; }
    public string? Token { get; set; }
    
    public GuildUserInfo(ulong userId)
    {
        UserId = userId;
    }
}