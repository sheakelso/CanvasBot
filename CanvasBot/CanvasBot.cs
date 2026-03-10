using CanvasAPI;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CanvasBot;

public class CanvasBot
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    
    private Dictionary<ulong, GuildInfo> _serverData;
    private readonly string _dataFileName;
    
    private readonly SlashCommands _slashCommands;

    private Task? _updateTokensTask;
    private Task? _checkForAnnouncementsTask;
    private CancellationTokenSource _loopCancellationTokenSource;
    
    public CanvasBot(string token, string dataFileName)
    {
        _token = token;
        
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.Ready += OnReady;
        _client.JoinedGuild += OnJoinedGuild;
        _client.LeftGuild += OnLeftGuild;
        _client.SlashCommandExecuted += OnSlashCommandExecuted;

        _dataFileName = dataFileName;
        LoadData();

        _slashCommands = new SlashCommands();
        
        _updateTokensTask = new(async () =>
        {
            await UpdateCourseTokens();
            await Task.Delay(600000);
        });
        
        if(_serverData == null) _serverData = new Dictionary<ulong, GuildInfo>();
    }

    public async Task Start()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task Stop()
    {
        await _loopCancellationTokenSource.CancelAsync();
        await _client.StopAsync();
        await _client.LogoutAsync();
        SaveData();
    }

    public Task Log(LogMessage message)
    {
        Console.WriteLine(message.Message);
        return Task.CompletedTask;
    }
    
    private async Task OnReady()
    {
        try
        {
            foreach (SocketGuild guild in _client.Guilds)
            {
                if (!_serverData.ContainsKey(guild.Id))
                {
                    _serverData.Add(guild.Id, new GuildInfo(guild.Id));
                }

                GuildInfo guildInfo = _serverData[guild.Id];
                guildInfo.SetUsersGuild();
                
                await UpdateGuildCommands(guild);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        _loopCancellationTokenSource = new CancellationTokenSource();
        _updateTokensTask = UpdateTokensLoop(_loopCancellationTokenSource.Token);
        _checkForAnnouncementsTask = CheckForAnnouncementsLoop(_loopCancellationTokenSource.Token);
    }

    private Task OnJoinedGuild(SocketGuild guild)
    {
        _serverData.Add(guild.Id, new GuildInfo(guild.Id));
        return Task.CompletedTask;
    }

    private Task OnLeftGuild(SocketGuild guild)
    {
        _serverData.Remove(guild.Id);
        return Task.CompletedTask;
    }

    private Task OnSlashCommandExecuted(SocketSlashCommand socketCommand)
    {
        ulong? guildId = socketCommand.GuildId;
        if (guildId == null) return Task.CompletedTask;
        
        CommandExecutionContext context = new CommandExecutionContext(socketCommand, GetGuildInfo(guildId.Value));
        ICommand? command = _slashCommands.GetCommand(socketCommand.CommandName);
        
        if(command != null) command.Execute(context);
        
        return Task.CompletedTask;
    }

    private void LoadData()
    {
        if (!File.Exists(_dataFileName))
        {
            File.Create(_dataFileName).Close();
            _serverData = new Dictionary<ulong, GuildInfo>();
            return;
        }
        
        string fileContents = File.ReadAllText(_dataFileName);
        Dictionary<ulong, GuildInfo>? data =
            JsonConvert.DeserializeObject<Dictionary<ulong, GuildInfo>>(fileContents);

        if(data != null) _serverData = data;
        else _serverData = new Dictionary<ulong, GuildInfo>();
    }

    private void SaveData()
    {
        string json = JsonConvert.SerializeObject(_serverData);
        File.WriteAllText(_dataFileName, json);
    }

    private async Task UpdateGuildCommands(SocketGuild guild)
    {
        await guild.BulkOverwriteApplicationCommandAsync(_slashCommands.GetCommandProperties);
    }

    public GuildInfo GetGuildInfo(ulong guildId) => _serverData[guildId];

    public async Task CheckForAnnouncements()
    {
        foreach (GuildInfo guildInfo in _serverData.Values)
        {
            foreach (GuildCourseInfo courseInfo in guildInfo.GetCourses())
            {
                CanvasClient? client = guildInfo.CreateCanvasClient(courseInfo.GetToken());
                if(client == null) continue;
                
                Course? course = await client.GetCourse(courseInfo.CourseId);
                if(course == null) continue;

                Dictionary<string, Discussion>? discussions = await course.GetDiscussions(courseInfo.LastAnnouncementCursor);
                if(discussions == null) continue;

                foreach (Discussion discussion in discussions.Values)
                {
                    await MakeAnnouncement(guildInfo, courseInfo, course, discussion);
                }

                courseInfo.LastAnnouncementCursor = discussions.Last().Key;
            }
        }
    }

    public async Task UpdateCourseTokens()
    {
        foreach (GuildInfo guildInfo in _serverData.Values)
        {
            foreach (GuildUserInfo userInfo in guildInfo.GetUsers())
            {
                CanvasClient? client = userInfo.CreateCanvasClient();
                if (client == null || userInfo.Token == null) continue;

                Course[]? courses = await client.GetAllCourses();
                if(courses == null || courses.Length == 0) continue;

                foreach (Course course in courses)
                {
                    GuildCourseInfo courseInfo = guildInfo.GetCourseInfo(course.Id);
                    if(!courseInfo.HasToken(userInfo.Token)) courseInfo.AddToken(userInfo.Token);
                }
            }
        }
    }

    public async Task MakeAnnouncement(GuildInfo guildInfo, GuildCourseInfo courseInfo, Course course, Discussion announcement)
    {
        if (guildInfo.Channels.TryGetValue(ChannelType.Announcements, out ulong channelId))
        {
            SocketGuild guild = _client.GetGuild(guildInfo.GuildId);
            SocketGuildChannel channel = guild.GetChannel(channelId);
            if (channel is IMessageChannel messageChannel)
            {
                User? author = await announcement.GetAuthor();
                string? message = await announcement.GetMessage();
                string? title = await announcement.GetTitle();
                if (author == null || message == null || title == null) return;
                
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle(await course.GetName());
                embed.WithColor(courseInfo.Color);
                embed.WithAuthor(await author.GetName(), await author.GetAvatarUrl());
                embed.WithTimestamp(await announcement.GetPostedAt());
                embed.WithDescription($"## {title}\n\n{message}");
                
                await messageChannel.SendMessageAsync(embed: embed.Build());
            }
        }
        
    }

    private async Task UpdateTokensLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await UpdateCourseTokens();
            await Task.Delay(600000, cancellationToken);
        }
    }
    
    private async Task CheckForAnnouncementsLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CheckForAnnouncements();
            await Task.Delay(60000, cancellationToken);
        }
    }
}