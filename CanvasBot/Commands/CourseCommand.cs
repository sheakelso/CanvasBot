using CanvasAPI;
using Discord;
using Discord.WebSocket;

namespace CanvasBot;

public class CourseCommand : IAutocompleteCommand
{
    public ApplicationCommandProperties Properties { get; private set; } = BuildCommand();
    public bool IsGlobal => false;
    
    public async Task Execute(CommandExecutionContext ctx)
    {
        SocketSlashCommandDataOption? nicknameOption = ctx.Command.Data.Options.FirstOrDefault(o => o.Name == "nickname");
        if (nicknameOption != null)
        {
            SocketSlashCommandDataOption? courseIdOption = nicknameOption.Options.FirstOrDefault(o => o.Name == "course-id");
            SocketSlashCommandDataOption? valueOption = nicknameOption.Options.FirstOrDefault(o => o.Name == "value");

            if (courseIdOption != null && valueOption != null)
            {
                string courseId = (string)courseIdOption.Value;
                string newValue = (string)valueOption.Value;

                GuildCourseInfo? courseInfo = ctx.CurrentGuild.GetCourseById(courseId);
                if (courseInfo == null)
                {
                    await ctx.Command.RespondAsync("Course not found.");
                    return;
                }
                
                courseInfo.Nickname = newValue;

                await ctx.Command.RespondAsync($"Set nickname of course '{courseId}' to '{newValue}'.");
            }
        }
        
        SocketSlashCommandDataOption? colorOption = ctx.Command.Data.Options.FirstOrDefault(o => o.Name == "color");
        if (colorOption != null)
        {
            SocketSlashCommandDataOption? courseIdOption = colorOption.Options.FirstOrDefault(o => o.Name == "course-id");
            SocketSlashCommandDataOption? valueOption = colorOption.Options.FirstOrDefault(o => o.Name == "value");

            if (courseIdOption != null && valueOption != null)
            {
                string courseId = (string)courseIdOption.Value;
                string hex = (string)valueOption.Value;

                if (Color.TryParse(hex, out Color color))
                {
                    GuildCourseInfo? courseInfo = ctx.CurrentGuild.GetCourseById(courseId);
                    if (courseInfo == null)
                    {
                        await ctx.Command.RespondAsync("Course not found.");
                        return;
                    }
                    
                    courseInfo.ColorHex = hex;
                    await ctx.Command.RespondAsync($"Set nickname of course '{courseId}' to '{hex}'.");
                }

                await ctx.Command.RespondAsync($"Provided hex code '{hex}' was formatted incorrectly.");
            }
        }
    }

    public async Task ExecuteAutocomplete(AutocompleteInteractionContext ctx)
    {
        AutocompleteOption option = ctx.Interaction.Data.Current;
        if (option.Name == "course-id")
        {
            await ctx.Interaction.RespondAsync(GetCourseResults(ctx.CurrentGuild.GetCourses(), (string)option.Value));
        }
    }

    public List<AutocompleteResult> GetCourseResults(GuildCourseInfo[] courses, string value)
    {
        List<AutocompleteResult> results = new List<AutocompleteResult>();
        foreach (GuildCourseInfo course in courses)
        {
            if (value.Trim().Length == 0)
            {
                results.Add(new AutocompleteResult(course.Nickname ?? course.CourseId, course.CourseId));
                continue;
            }

            if (course.Nickname != null)
            {
                if (course.Nickname.ToLower().Contains(value.ToLower()))
                {
                    results.Insert(0, new AutocompleteResult(course.Nickname, course.CourseId));
                    continue;
                }
            }

            if (course.CourseId.ToLower().Contains(value.ToLower()))
            {
                results.Add(new AutocompleteResult(course.Nickname ?? course.CourseId, course.CourseId));
            }
        }
        
        return results;
    }

    private static ApplicationCommandProperties BuildCommand()
    {
        SlashCommandOptionBuilder courseBuilder = new SlashCommandOptionBuilder();
        courseBuilder.WithName("course-id");
        courseBuilder.WithDescription("ID of the course you want to edit.");
        courseBuilder.WithType(ApplicationCommandOptionType.String);
        courseBuilder.WithAutocomplete(true);
        courseBuilder.WithRequired(true);
        
        SlashCommandOptionBuilder nicknameValueBuilder = new SlashCommandOptionBuilder();
        nicknameValueBuilder.WithName("value");
        nicknameValueBuilder.WithDescription("New nickname.");
        nicknameValueBuilder.WithType(ApplicationCommandOptionType.String);
        nicknameValueBuilder.WithRequired(true);
        
        SlashCommandOptionBuilder nicknameBuilder = new SlashCommandOptionBuilder();
        nicknameBuilder.WithName("nickname");
        nicknameBuilder.WithDescription("Sets a courses nickname.");
        nicknameBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        nicknameBuilder.AddOption(courseBuilder);
        nicknameBuilder.AddOption(nicknameValueBuilder);
        
        SlashCommandOptionBuilder colorValueBuilder = new SlashCommandOptionBuilder();
        colorValueBuilder.WithName("value");
        colorValueBuilder.WithDescription("Hex code for new color.");
        colorValueBuilder.WithType(ApplicationCommandOptionType.String);
        colorValueBuilder.WithRequired(true);
        
        SlashCommandOptionBuilder colorBuilder = new SlashCommandOptionBuilder();
        colorBuilder.WithName("color");
        colorBuilder.WithDescription("Sets a courses color.");
        colorBuilder.WithType(ApplicationCommandOptionType.SubCommand);
        colorBuilder.AddOption(courseBuilder);
        colorBuilder.AddOption(colorValueBuilder);
        
        SlashCommandBuilder builder = new SlashCommandBuilder();
        builder.WithName("course");
        builder.WithDescription("Edit course settings.");
        builder.AddOption(nicknameBuilder);
        builder.AddOption(colorBuilder);
        
        return builder.Build();
    }
}