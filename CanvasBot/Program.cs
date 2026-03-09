using CanvasAPI;
using Discord;
using Discord.WebSocket;

public static class Program
{
    public static async Task Main(string[] args)
    {
        string token = await File.ReadAllTextAsync("token");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Token is missing");
            return;
        }
        
        CanvasBot.CanvasBot bot = new CanvasBot.CanvasBot(token, "data.json");
        await bot.Start();
        
        Console.ReadLine();

        await bot.Stop();
    }
}