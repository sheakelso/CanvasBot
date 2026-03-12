
using CanvasAPI;

static class CanvasAPITester
{
    public static async Task Main(string[] args)
    {
        CanvasClient client = new CanvasClient("https://canvas.qub.ac.uk/",
            "12025~nKkzLNwtVwcPC6X4QxBakzEMAckZ6DUcYG8zX8YBYQkZBPnBEfyeQTk3FuVrMnzk");

        Course[]? courses = await client.GetAllCourses();
        foreach (Course course in courses)
        {
            Console.WriteLine(course.name);
            Dictionary<string, Discussion>? discussions = await course.GetDiscussions();

            if (discussions != null)
            {
                foreach (Discussion discussion in discussions.Values)
                {
                    Console.WriteLine(discussion.title);
                    User? author = await discussion.GetAuthor();
                    if(author != null) Console.WriteLine(author.name);
                }
            }
        }
    }
}