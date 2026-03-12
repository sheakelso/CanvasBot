using System.Reflection;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class SlashCommands
{
    private readonly List<ICommand> _commands = new();

    public SlashCommands()
    {
        _commands.Add(new CanvasURLCommand());
        _commands.Add(new UserTokenCommand());
        _commands.Add(new CoursesCommand());
        _commands.Add(new CourseCommand());
        _commands.Add(new ChannelCommand());
    }
    
    public ICommand[] Commands => _commands.ToArray();
    public ApplicationCommandProperties[] GetCommandProperties => Array.ConvertAll(Commands, c => c.Properties);

    public ICommand? GetCommand(string name) => _commands.FirstOrDefault(c => c.Properties.Name.Value == name);
}