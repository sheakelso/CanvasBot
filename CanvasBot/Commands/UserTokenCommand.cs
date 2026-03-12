using CanvasAPI;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class UserTokenCommand : ICommand
{
    public ApplicationCommandProperties Properties { get; private set; } = BuildCommand();
    public bool IsGlobal => true;
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        SocketSlashCommandDataOption subcommand = ctx.Command.Data.Options.First();
        switch (subcommand.Name)
        {
            case "set":
                SocketSlashCommandDataOption? tokenData = subcommand.Options.FirstOrDefault(o => o.Name == "token");
                if (tokenData != null)
                {
                    await ctx.Command.DeferAsync(true);
                    string token = (string)tokenData.Value;
                    token = token.Trim();

                    GuildUserInfo user = ctx.CurrentGuild.GetOrCreateUserInfo(ctx.Command.User.Id);

                    bool valid = await user.TrySetToken(token);
                    
                    if(valid) await ctx.Command.ModifyOriginalResponseAsync(properties => properties.Content = "Your Canvas token has been set.");
                    else await ctx.Command.ModifyOriginalResponseAsync(properties => properties.Content = "Failed to set Canvas token. The Canvas URL or token may be invalid.");
                }

                await ctx.Command.RespondAsync("You must provide your Canvas token.");
                break;
        }
    }


    private static ApplicationCommandProperties BuildCommand()
    {
        SlashCommandBuilder builder = new SlashCommandBuilder();
        builder.WithName("token");
        builder.WithDescription("Sets your Canvas token.");

        SlashCommandOptionBuilder setBuilder = new SlashCommandOptionBuilder();
        setBuilder.WithName("set");
        setBuilder.WithDescription("Sets your Canvas token.");
        setBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        builder.AddOption(setBuilder);
        
        SlashCommandOptionBuilder tokenBuilder = new SlashCommandOptionBuilder();
        tokenBuilder.WithName("token");
        tokenBuilder.WithDescription("Your canvas token.");
        tokenBuilder.WithType(ApplicationCommandOptionType.String);
        tokenBuilder.WithRequired(true);
        setBuilder.AddOption(tokenBuilder);
        
        return builder.Build();
    }
}