using CanvasAPI;
using Discord.WebSocket;

namespace CanvasBot;

public class AutocompleteInteractionContext
{
    public readonly SocketAutocompleteInteraction Interaction;
    public readonly GuildData Data;
    public readonly GuildInfo CurrentGuild;
    private readonly Dictionary<ulong, CanvasClient> _canvasClients = new();

    public AutocompleteInteractionContext(SocketAutocompleteInteraction interaction, GuildData data, GuildInfo currentGuild)
    {
        Interaction = interaction;
        Data = data;
        CurrentGuild = currentGuild;
        
        if(currentGuild.CanvasUrl == null) return;
        
        foreach (GuildUserInfo user in currentGuild.GetUsers())
        {
            string? token = user.Token;
            if (token != null)
            {
                _canvasClients.Add(user.UserId, new CanvasClient(currentGuild.CanvasUrl.ToString(), token));
            }
        }
    }
    
    public CanvasClient? GetCanvasClient(ulong userId) => _canvasClients.GetValueOrDefault(userId);
    public bool HasCanvasClient(ulong userId) => _canvasClients.ContainsKey(userId);
}