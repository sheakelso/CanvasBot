using CanvasAPI;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public enum ChannelType
{
    Announcements
}

public class ChannelCommand : ICommand
{
    public ApplicationCommandProperties Properties { get; private set; } = BuildCommand();
    public bool IsGlobal => false;
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        SocketSlashCommandDataOption? setOption = ctx.Command.Data.Options.FirstOrDefault(o => o.Name == "set");
        if (setOption != null)
        {
            SocketSlashCommandDataOption? channelTypeOption = setOption.Options.FirstOrDefault(o => o.Name == "channel-type");
            if (channelTypeOption != null)
            {
                int intType = Convert.ToInt32(channelTypeOption.Value);
                ChannelType type = (ChannelType)intType;
                SocketSlashCommandDataOption? channelOption = setOption.Options.FirstOrDefault(o => o.Name == "channel");
                if (channelOption != null)
                {
                    if (channelOption.Value is SocketGuildChannel channel)
                    {
                        ctx.CurrentGuild.Channels[type] = channel.Id;
                        await ctx.Command.RespondAsync($"Canvas channel '{Enum.GetName(type)}' is set to {MentionUtils.MentionChannel(channel.Id)}.");
                        return;
                    }
                }
            }
        }
        
        SocketSlashCommandDataOption? getOption = ctx.Command.Data.Options.FirstOrDefault(o => o.Name == "get");
        if (getOption != null)
        {
            SocketSlashCommandDataOption? channelTypeOption = getOption.Options.FirstOrDefault(o => o.Name == "channel-type");
            if (channelTypeOption != null)
            {
                int intType = Convert.ToInt32(channelTypeOption.Value);
                ChannelType type = (ChannelType)intType;
                if (ctx.CurrentGuild.Channels.TryGetValue(type, out var channel))
                {
                    await ctx.Command.RespondAsync($"Canvas channel '{Enum.GetName(type)}' is set to {MentionUtils.MentionChannel(channel)}.");
                    return;
                }
                await ctx.Command.RespondAsync($"Canvas channel '{Enum.GetName(type)}' is not set.");
            }
        }
    }
    
    private static ApplicationCommandProperties BuildCommand()
    {
        SlashCommandBuilder builder = new SlashCommandBuilder();
        builder.WithName("channel");
        builder.WithDescription("Manage Canvas channels.");

        SlashCommandOptionBuilder setBuilder = new SlashCommandOptionBuilder();
        setBuilder.WithName("set");
        setBuilder.WithDescription("Set Canvas channels.");
        setBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        builder.AddOption(setBuilder);
        
        SlashCommandOptionBuilder getBuilder = new SlashCommandOptionBuilder();
        getBuilder.WithName("get");
        getBuilder.WithDescription("Get Canvas channels.");
        getBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        builder.AddOption(getBuilder);
        
        SlashCommandOptionBuilder channelTypeBuilder = new SlashCommandOptionBuilder();
        channelTypeBuilder.WithName("channel-type");
        channelTypeBuilder.WithDescription("Canvas channel type.");
        channelTypeBuilder.WithType(ApplicationCommandOptionType.Integer);
        channelTypeBuilder.WithRequired(true);
        foreach (ChannelType channelType in Enum.GetValues(typeof(ChannelType)))
        {
            Console.WriteLine(Enum.GetName(channelType));
            channelTypeBuilder.AddChoice(Enum.GetName(channelType), (int)channelType);
        }
        setBuilder.AddOption(channelTypeBuilder);
        getBuilder.AddOption(channelTypeBuilder);
        
        SlashCommandOptionBuilder channelBuilder = new SlashCommandOptionBuilder();
        channelBuilder.WithName("channel");
        channelBuilder.WithDescription("Channel to receive Canvas notifications.");
        channelBuilder.WithType(ApplicationCommandOptionType.Channel);
        channelBuilder.WithRequired(true);
        setBuilder.AddOption(channelBuilder);
        
        return builder.Build();
    }
}