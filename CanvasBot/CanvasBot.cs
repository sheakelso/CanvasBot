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
        _client.LeftGuild += OnLeftGuild;
        _client.ButtonExecuted += OnButtonExecuted;

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

    private Task OnLeftGuild(SocketGuild guild)
    {
        _data.RemoveGuild(guild);
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
        if(newAnnouncements.Count > 0) _data.Save();
    }

    private async Task MakeAnnouncement(GuildCourseInfo courseInfo, Discussion announcement)
    {
        GuildInfo guildInfo = courseInfo.GuildInfo;
        
        if (guildInfo.Channels.TryGetValue(ChannelType.Announcements, out ulong channelId))
        {
            SocketGuild guild = _client.GetGuild(guildInfo.GuildId);
            SocketGuildChannel channel = guild.GetChannel(channelId);
            if (channel is IMessageChannel messageChannel)
            {
                Embed? embed = await AnnouncementUtils.CreateAnnouncementEmbed(courseInfo, announcement);
                if (embed == null) return;
                
                ComponentBuilder componentBuilder = new ComponentBuilder();
                componentBuilder.WithButton("Mark as Read", "read" + announcement.Id);
                
                await messageChannel.SendMessageAsync(MentionUtils.MentionRole(courseInfo.RoleId), embed: embed, components: componentBuilder.Build());
            }
        }
    }

    private async Task OnButtonExecuted(SocketMessageComponent component)
    {
        if (component.Data.CustomId.StartsWith("read"))
        {
            ulong? guildId = component.GuildId;
            if (guildId == null)
            {
                await component.RespondAsync("This command must be used within a server.", ephemeral: true);
                return;
            }
            
            GuildInfo? guildInfo = _data.GetGuildInfoById(guildId.Value);
            if (guildInfo == null)
            {
                await component.RespondAsync("No user token set.", ephemeral: true);
                return;
            }

            GuildUserInfo guildUserInfo = guildInfo.GetOrCreateUserInfo(component.User.Id);
            CanvasClient? canvasClient = guildUserInfo.CreateCanvasClient();
            if (canvasClient == null)
            {
                await component.RespondAsync("No user token set.", ephemeral: true);
                return;
            }
            
            string discussionId = component.Data.CustomId.Replace("read", "");
            Discussion? discussion = await canvasClient.GetNode<Discussion>(discussionId);
            if (discussion == null)
            {
                await component.RespondAsync("You do not have access to this discussion.", ephemeral: true);
                return;
            }

            bool success = await discussion.SetReadState(true);
            if(!success) await component.RespondAsync("An error occured.", ephemeral: true);
            else await component.RespondAsync("Announcement marked as read.", ephemeral: true);
        }
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