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

                    if (valid)
                    {
                        await ctx.Command.ModifyOriginalResponseAsync(properties => properties.Content = "Your Canvas token has been set.");
                        ctx.Data.Save();
                    }
                    else await ctx.Command.ModifyOriginalResponseAsync(properties => properties.Content = "Failed to set Canvas token. The Canvas URL or token may be invalid.");
                }

                await ctx.Command.RespondAsync("You must provide your Canvas token.");
                break;
            
            case "help":
                await RespondWithHelpMessage(ctx);
                break;
        }
    }

    private async Task RespondWithHelpMessage(CommandExecutionContext ctx)
    {
        Uri? canvasUrl = ctx.CurrentGuild.CanvasUrl;
        if (canvasUrl == null)
        {
            await ctx.Command.RespondAsync("A Canvas URL must be set on the server before setting access tokens.");
            return;
        }
        
        string message =
            "In order for me to access your Canvas information, you need to generate an 'access token'. Here are the steps to get set up:";

        string embedDescription =
            $"**1. Generate an access token**Head to {canvasUrl.ToString()}profile/settings and click 'New access token' under the 'Approved integrations' section. Set the purpose to 'Canvas Discord Bot' and set the expiration date to whatever you wish. After this expiration date you _will_ have to generate a new token before using this bot again. Copy the generated token.\n\n**2. Submit your token**Once you have your access token, return to this server and do the following command:\n**/token set <your token>**\nIf successful, the bot can now access your Canvas information!";

        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("Access Token Submission");
        embed.WithDescription(embedDescription);
        embed.WithColor(Color.Parse("ee6060"));
        
        await ctx.Command.RespondAsync(message, embed: embed.Build());
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
        
        SlashCommandOptionBuilder helpBuilder = new SlashCommandOptionBuilder();
        helpBuilder.WithName("help");
        helpBuilder.WithDescription("Display a guide on how to set your access token.");
        helpBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        builder.AddOption(helpBuilder);
        
        SlashCommandOptionBuilder tokenBuilder = new SlashCommandOptionBuilder();
        tokenBuilder.WithName("token");
        tokenBuilder.WithDescription("Your canvas token.");
        tokenBuilder.WithType(ApplicationCommandOptionType.String);
        tokenBuilder.WithRequired(true);
        setBuilder.AddOption(tokenBuilder);
        
        return builder.Build();
    }
}