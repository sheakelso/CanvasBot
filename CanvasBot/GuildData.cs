using CanvasAPI;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CanvasBot;

public class GuildData
{
    private readonly DiscordSocketClient _client;
    
    private Dictionary<ulong, GuildInfo> _guildInfos = new();
    private readonly string _dataFileName;

    public GuildData(DiscordSocketClient client, string dataFileName)
    {
        _client = client;
        _dataFileName = dataFileName;
        LoadData();
    }

    public void RemoveGuild(SocketGuild guild)
    {
        _guildInfos.Remove(guild.Id);
    }
    
    public void RemoveGuild(ulong id)
    {
        _guildInfos.Remove(id);
    }

    private void LoadData()
    {
        if(!File.Exists(_dataFileName)) File.Create(_dataFileName).Close();
        string json = File.ReadAllText(_dataFileName);
        Dictionary<ulong, GuildInfo>? data = JsonConvert.DeserializeObject<Dictionary<ulong, GuildInfo>>(json);

        if (data != null)
        {
            foreach (GuildInfo info in data.Values)
            {
                info.Initialize(_client);
            }
            _guildInfos = data;
        }
    }
    
    public GuildInfo? GetGuildInfoById(ulong id) => _guildInfos.GetValueOrDefault(id);
    
    public GuildInfo CreateGuildInfo(SocketGuild guild)
    {
        GuildInfo guildInfo = new GuildInfo(_client, guild);
        _guildInfos.Add(guildInfo.GuildId, guildInfo);
        return guildInfo;
    }

    public GuildInfo GetOrCreateGuildInfo(SocketGuild guild)
    {
        GuildInfo? guildInfo = GetGuildInfoById(guild.Id);
        if(guildInfo != null) return guildInfo;
        return CreateGuildInfo(guild);
    }
    
    public async Task Refresh()
    {
        foreach (GuildInfo guildInfo in _guildInfos.Values)
        {
            if(_client.Guilds.Count(guild => guild.Id == guildInfo.GuildId) == 0) RemoveGuild(guildInfo.GuildId);
            else await guildInfo.Refresh();
        }
    }

    public async Task<Dictionary<GuildCourseInfo, Discussion[]>> GetNewAnnouncements()
    {
        Dictionary<GuildCourseInfo, Discussion[]> newAnnouncements = new();
        foreach (GuildInfo guildInfo in _guildInfos.Values)
        {
            Dictionary<GuildCourseInfo, Discussion[]> guildAnnouncements = await guildInfo.GetNewAnnouncements();
            foreach (KeyValuePair<GuildCourseInfo, Discussion[]> guildAnnouncement in guildAnnouncements)
            {
                newAnnouncements.Add(guildAnnouncement.Key, guildAnnouncement.Value);
            }
        }
        return newAnnouncements;
    }
    
    public void Save()
    {
        string json = JsonConvert.SerializeObject(_guildInfos);
        File.WriteAllText(_dataFileName, json);
    }
}