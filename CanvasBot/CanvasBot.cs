using CanvasAPI;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class CanvasBot
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    private readonly GuildData _data;
    private readonly SlashCommands _slashCommands;

    private CancellationTokenSource? _ctSource;
    private Task? _refreshTask;
    
    public CanvasBot(string token, string dataFileName)
    {
        _token = token.Trim();
        
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.Ready += OnReady;
        _client.SlashCommandExecuted += OnSlashCommandExecuted;
        _client.AutocompleteExecuted += OnAutocompleteExecuted;

        _data = new GuildData(_client, dataFileName);

        _slashCommands = new SlashCommands();
    }

    public async Task Start()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task Stop()
    {
        if(_ctSource != null) await _ctSource.CancelAsync();
        await _client.StopAsync();
        await _client.LogoutAsync();
        _data.Save();
    }

    private Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }
    
    private async Task OnReady()
    {
        try
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                await UpdateGuildCommands(guild);
            }

            _ctSource = new CancellationTokenSource();
            _refreshTask = RefreshLoop(_ctSource.Token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private Task OnSlashCommandExecuted(SocketSlashCommand socketCommand)
    {
        ulong? guildId = socketCommand.GuildId;
        if (guildId == null) return Task.CompletedTask;

        SocketGuild guild = _client.GetGuild(guildId.Value);
        
        CommandExecutionContext context = new CommandExecutionContext(socketCommand, _data, _data.GetOrCreateGuildInfo(guild));
        ICommand? command = _slashCommands.GetCommand(socketCommand.CommandName);
        
        if(command != null) command.Execute(context);
        
        return Task.CompletedTask;
    }

    private Task OnAutocompleteExecuted(SocketAutocompleteInteraction interaction)
    {
        ulong? guildId = interaction.GuildId;
        if (guildId == null) return Task.CompletedTask;

        SocketGuild guild = _client.GetGuild(guildId.Value);

        AutocompleteInteractionContext ctx = 
            new AutocompleteInteractionContext(interaction, _data, _data.GetOrCreateGuildInfo(guild));
        ICommand? command = _slashCommands.GetCommand(interaction.Data.CommandName);

        if (command is IAutocompleteCommand autocompleteCommand)
            autocompleteCommand.ExecuteAutocomplete(ctx);
        
        return Task.CompletedTask;
    }
    
    private async Task UpdateGuildCommands(SocketGuild guild)
    {
        await guild.BulkOverwriteApplicationCommandAsync(_slashCommands.GetCommandProperties);
    }
    
    public async Task SendNewAnnouncements()
    {
        Dictionary<GuildCourseInfo, Discussion[]> newAnnouncements = await _data.GetNewAnnouncements();
        foreach (GuildCourseInfo course in newAnnouncements.Keys)
        {
            Discussion[] courseAnnouncements = newAnnouncements[course];
            foreach (Discussion announcement in courseAnnouncements)
            {
                await MakeAnnouncement(course, announcement);
            }
        }
    }

    

    private async Task MakeAnnouncement(GuildCourseInfo courseInfo, Discussion announcement)
    {
        GuildInfo guildInfo = courseInfo.GuildInfo;
        Course? course = await courseInfo.GetCourse();
        if(course == null) return;
        
        if (guildInfo.Channels.TryGetValue(ChannelType.Announcements, out ulong channelId))
        {
            SocketGuild guild = _client.GetGuild(guildInfo.GuildId);
            SocketGuildChannel channel = guild.GetChannel(channelId);
            if (channel is IMessageChannel messageChannel)
            {
                User? author = await announcement.GetAuthor();
                string? message = await announcement.GetMessage();
                string title = announcement.title;
                if (author == null || message == null) return;
                
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle(course.name);
                embed.WithColor(courseInfo.Color);
                embed.WithAuthor(author.name, author.avatarUrl);
                embed.WithDescription(CreateAnnouncementBody(title, message, announcement.Link));
                embed.WithFooter("Canvas",
                    "https://du11hjcvx0uqb.cloudfront.net/dist/images/canvas_logomark_only@2x-e197434829.png");
                
                if (announcement.postedAt != null) embed.WithTimestamp(announcement.postedAt.Value);
                
                await messageChannel.SendMessageAsync(embed: TruncateEmbed(embed).Build());
            }
        }
        
    }

    private string CreateAnnouncementBody(string title, string htmlMessage, string url)
    {
        string messageText = HtmlUtils.GetHtmlText(htmlMessage);
        return TruncateString($"### [{title}]({url})\n\n{messageText}", EmbedBuilder.MaxDescriptionLength);
    }

    private EmbedBuilder TruncateEmbed(EmbedBuilder embed)
    {
        int totalCharacters = 0;
        
        embed.Title = TruncateString(embed.Title, EmbedBuilder.MaxTitleLength);
        embed.Description = TruncateString(embed.Description, EmbedBuilder.MaxDescriptionLength);
        totalCharacters += embed.Title.Length;
        totalCharacters += embed.Description.Length;
        
        if (embed.Author != null)
        {
            embed.Author.Name = TruncateString(embed.Author.Name, EmbedAuthorBuilder.MaxAuthorNameLength);
            totalCharacters += embed.Author.Name.Length;
        }

        if (embed.Footer != null)
        {
            embed.Footer.Text = TruncateString(embed.Footer.Text, EmbedFooterBuilder.MaxFooterTextLength);
            totalCharacters += embed.Footer.Text.Length;
        }
        
        if(totalCharacters > 6000) embed.Title = TruncateString(embed.Description, 6000 - embed.Title.Length - embed.Author.Name.Length -embed.Footer.Text.Length);
        Console.WriteLine(totalCharacters);
        return embed;
    }

    private string TruncateString(string? str, int maxLength)
    {
        if (str == null) return "";
        if(str.Length > maxLength) return str.Substring(0, maxLength - 3) + "...";
        return str;
    }

    private async Task DeleteAllRoles()
    {
        foreach (SocketGuild guild in _client.Guilds)
        {
            foreach (SocketRole role in guild.Roles)
            {
                await role.DeleteAsync();
            }
        }
    }

    private async Task RefreshLoop(CancellationToken ct)
    {
        while (ct.IsCancellationRequested == false)
        {
            Log("Refreshing data...");
            await _data.Refresh().ContinueWith(HandleTaskException, ct);
            Log("Checking for announcements...");
            await SendNewAnnouncements().ContinueWith(HandleTaskException, ct);
            await Task.Delay(30000, ct);
        }
    }

    private void HandleTaskException(Task task)
    {
        if (task.IsFaulted)
        {
            AggregateException ex = task.Exception.Flatten();
            Console.WriteLine(ex.ToString());
        }
    }

    private void Log(string message, LogSeverity severity = LogSeverity.Info)
    {
        Log(new LogMessage(severity, "CanvasBot", message));
    }
}