using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class CanvasURLCommand : ICommand
{
    public ApplicationCommandProperties Properties { get; private set; } = BuildCommand();
    public bool IsGlobal => false;

    private static ApplicationCommandProperties BuildCommand()
    {
        SlashCommandBuilder builder = new SlashCommandBuilder();
        builder.WithName("canvas-url").WithDescription("Get or set the server's Canvas url.");
        
        SlashCommandOptionBuilder setBuilder = new SlashCommandOptionBuilder();
        setBuilder.WithName("set").WithDescription("Set the server's Canvas url.");
        setBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        setBuilder.AddOption("url", ApplicationCommandOptionType.String, "Canvas url", true);
        
        SlashCommandOptionBuilder getBuilder = new SlashCommandOptionBuilder();
        getBuilder.WithName("get").WithDescription("Display the server's Canvas url.");
        getBuilder.WithType(ApplicationCommandOptionType.SubCommand);
            
        builder.AddOption(getBuilder).AddOption(setBuilder);
            
        return builder.Build();
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        SocketSlashCommand command = ctx.Command;
        
        SocketSlashCommandDataOption subCommand = command.Data.Options.First();
        switch (subCommand.Name)
        {
            case "get":
                Uri? canvasUrl = ctx.CurrentGuild.CanvasUrl;
                if(canvasUrl != null) await command.RespondAsync($"This server's Canvas URL is '{canvasUrl.ToString()}'.");
                else  await command.RespondAsync("No Canvas URL has been set.");
                break;
            
            case "set":
                SocketSlashCommandDataOption? urlData = 
                    subCommand.Options.FirstOrDefault(o => o.Name == "url");
                if (urlData != null)
                {
                    bool success = ctx.CurrentGuild.SetCanvasUrl((string)urlData.Value);
                    if (success) await command.RespondAsync($"Canvas URL set to '{(string)urlData.Value}'.");
                    else await command.RespondAsync($"Provided url '{(string)urlData.Value}' is invalid.");
                    return;
                }

                await command.RespondAsync("You must provide a URL.");
                break;
        }
    }
}