using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public interface ICommand
{
    public ApplicationCommandProperties Properties { get; }
    public bool IsGlobal { get; }
    public Task Execute(CommandExecutionContext ctx);
}