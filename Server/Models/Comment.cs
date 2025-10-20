namespace Server.Models;

public class Comment
{
    public int Id { get; set; }
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int Score { get; set; } = 0;

    // FK
    public int PostId { get; set; }
}
