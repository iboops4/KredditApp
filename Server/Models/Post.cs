namespace Server.Models;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";

    // ENTEN Text ELLER Url
    public string? Text { get; set; }
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Score { get; set; } = 0;

    public List<Comment> Comments { get; set; } = new();
}
