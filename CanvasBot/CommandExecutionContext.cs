using CanvasAPI;
using Discord.WebSocket;

namespace CanvasBot;

public class CommandExecutionContext
{
    public readonly SocketSlashCommand Command;
    public readonly GuildInfo GuildInfo;
    private readonly Dictionary<ulong, CanvasClient> _canvasClients = new();

    public CommandExecutionContext(SocketSlashCommand command, GuildInfo guildInfo)
    {
        Command = command;
        GuildInfo = guildInfo;
        
        if(guildInfo.CanvasUrl == null) return;
        
        foreach (GuildUserInfo user in guildInfo.GetUsers())
        {
            string? token = user.Token;
            if (token != null)
            {
                _canvasClients.Add(user.UserId, new CanvasClient(guildInfo.CanvasUrl.ToString(), token));
            }
        }
    }
    
    public CanvasClient? GetCanvasClient(ulong userId) => _canvasClients.GetValueOrDefault(userId);
    public bool HasCanvasClient(ulong userId) => _canvasClients.ContainsKey(userId);
}