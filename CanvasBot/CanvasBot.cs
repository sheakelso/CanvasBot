using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CanvasBot;

public class CanvasBot
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;
    
    private Dictionary<ulong, GuildInfo> _serverData;
    private readonly string _dataFileName;
    
    private readonly SlashCommands _slashCommands;
    
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
        
        if(_serverData == null) _serverData = new Dictionary<ulong, GuildInfo>(); 
    }

    public async Task Start()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task Stop()
    {
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

                await UpdateGuildCommands(guild);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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
}