using CanvasAPI;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class CoursesCommand : ICommand
{
    public ApplicationCommandProperties Properties { get; private set; } = BuildCommand();
    public bool IsGlobal => false;
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        SocketSlashCommandDataOption? userOption = ctx.Command.Data.Options.FirstOrDefault(o => o.Name == "user");
        if (userOption != null)
        {
            if (userOption.Value is SocketGuildUser user)
            {
                CanvasClient? client = ctx.GetCanvasClient(user.Id);
                if (client != null)
                {
                    Course[]? courses = await client.GetAllCourses();
                    await RespondWithCourses(ctx, courses, user.Id);
                }
            }
        }
    }

    public async Task RespondWithCourses(CommandExecutionContext ctx, Course[] courses, ulong userId)
    {
        Embed[] embeds = new Embed[courses.Length];
        for (int i = 0; i < courses.Length; i++)
        {
            GuildCourseInfo courseInfo = ctx.GuildInfo.GetCourseInfo(courses[i].Id);
            embeds[i] = await CreateCourseEmbed(courses[i], courseInfo.Color);
            Console.WriteLine(await courses[i].GetImageUrl());
        }
        
        await ctx.Command.RespondAsync($"Here the courses for {MentionUtils.MentionUser(userId)}: ", embeds);
    }

    public async Task<Embed> CreateCourseEmbed(Course course, Color color)
    {
        EmbedBuilder builder = new EmbedBuilder();
        builder.WithTitle(await course.GetName());
        builder.WithColor(color);
        string? imageUrl = await course.GetImageUrl();
        if (imageUrl is { Length: > 0 }) builder.WithImageUrl(imageUrl);

        return builder.Build();
    }

    private static ApplicationCommandProperties BuildCommand()
    {
        SlashCommandBuilder builder = new SlashCommandBuilder();
        builder.WithName("courses");
        builder.WithDescription("View Canvas courses.");

        SlashCommandOptionBuilder userBuilder = new SlashCommandOptionBuilder();
        userBuilder.WithName("user");
        userBuilder.WithDescription("User who's courses should be shown. Shows all if empty.");
        userBuilder.WithType(ApplicationCommandOptionType.User);
        builder.AddOption(userBuilder);
        
        return builder.Build();
    }
}