using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- EF Core + SQLite ----------
builder.Services.AddDbContext<AppDb>(opt =>
    opt.UseSqlite("Data Source=redditlite.db"));

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7228") // Blazor-port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Middleware ----------
app.UseCors("AllowBlazor");
app.UseSwagger();
app.UseSwaggerUI();

// ---------- Seed ----------
// ---------- Seed ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();

    // 1) Sørg for at tabellerne findes (opret/migrer schema)
    db.Database.Migrate();

    // 2) Seed KUN hvis der ingen posts er
    if (!await db.Posts.AnyAsync())
    {
        Console.WriteLine("Seeding 50 posts...");

        var random = new Random();
        var now = DateTime.UtcNow;

        string[] authors = { "Maja", "Noah", "Lucas", "Sofie", "Liva", "Oliver", "Freja", "William", "Clara", "Emil", "Laura", "Elias", "Anna", "Lukas", "Ida", "Oscar", "Alma", "Magnus", "Karla", "Victor", "Mille" };
        string[] titles = {
            "Hvad synes I om Blazor?",
            "Jeg elsker Entity Framework!",
            "C# er undervurderet",
            "Dark mode eller light mode?",
            "Nogen der kan forklare LINQ?",
            "Tips til async/await?",
            "SQL vs NoSQL debat",
            "Min første API virker!",
            "Er .NET 9 hurtigere?",
            "Hvordan deployer man til Azure?"
        };
        string[] comments = {
            "Fedt opslag!","Det giver mening!","Haha enig 😂","Tak for info!","Det skal jeg prøve",
            "Respekt 🙌","Interessant synspunkt","God pointe","Mega sejt!","Elsker det her community"
        };

        var posts = new List<Post>();
        for (int i = 0; i < 50; i++)
        {
            var author = authors[random.Next(authors.Length)];
            var title = titles[random.Next(titles.Length)];

            // Post: indenfor seneste 7 dage
            var postCreated = now.AddMinutes(-random.Next(7 * 24 * 60));

            var post = new Post
            {
                Title = title,
                Author = author,
                Text = $"Dette er et testopslag #{i + 1} skrevet af {author}.",
                CreatedAt = postCreated,
                Score = random.Next(-5, 40)
            };

            // Kommentarer: altid EFTER posten og FØR nu
            int commentCount = random.Next(1, 6);
            int maxDeltaSincePost = Math.Max(2, (int)(now - postCreated).TotalMinutes - 1);

            for (int j = 0; j < commentCount; j++)
            {
                var commenter = authors[random.Next(authors.Length)];
                int delta = random.Next(1, maxDeltaSincePost);
                var commentCreated = postCreated.AddMinutes(delta);

                post.Comments.Add(new Comment
                {
                    Author = commenter,
                    Text = comments[random.Next(comments.Length)],
                    CreatedAt = commentCreated,
                    Score = random.Next(-2, 6)
                });
            }

            posts.Add(post);
        }

        db.Posts.AddRange(posts);
        await db.SaveChangesAsync();
        Console.WriteLine("Seeding done.");
    }
    else
    {
        Console.WriteLine("DB already has data, no seeding.");
    }
}




// ---------- ROUTES / ENDPOINTS ----------

// GET: 50 nyeste posts
app.MapGet("/api/posts", (AppDb db) =>
{
    var posts = db.Posts
        .OrderByDescending(p => p.CreatedAt)
        .Take(50)
        .Select(p => new
        {
            p.Id,
            p.Title,
            p.Author,
            p.CreatedAt,
            p.Score,
            p.Url,
            Text = p.Text
        })
        .ToList();

    return Results.Ok(posts);
});

// GET: enkelt post inkl. comments
app.MapGet("/api/posts/{id:int}", (int id, AppDb db) =>
{
    var post = db.Posts
        .Include(p => p.Comments)
        .FirstOrDefault(p => p.Id == id);

    if (post is null)
        return Results.NotFound();

    post.Comments = post.Comments.OrderBy(c => c.CreatedAt).ToList();
    return Results.Ok(post);
});

// POST: opret ny post
app.MapPost("/api/posts", async (PostDto dto, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Author))
        return Results.BadRequest("Title og Author er påkrævet.");

    var both = !string.IsNullOrWhiteSpace(dto.Text) && !string.IsNullOrWhiteSpace(dto.Url);
    var none = string.IsNullOrWhiteSpace(dto.Text) && string.IsNullOrWhiteSpace(dto.Url);
    if (both || none)
        return Results.BadRequest("Angiv enten Text eller Url.");

    var post = new Post
    {
        Title = dto.Title,
        Author = dto.Author,
        Text = dto.Text,
        Url = dto.Url
    };

    db.Posts.Add(post);
    await db.SaveChangesAsync();
    return Results.Created($"/api/posts/{post.Id}", post);
});

// POST: tilføj kommentar
app.MapPost("/api/posts/{id:int}/comments", async (int id, CommentDto dto, AppDb db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null)
        return Results.NotFound();

    var c = new Comment { PostId = id, Author = dto.Author, Text = dto.Text };
    db.Comments.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/api/posts/{id}/comments/{c.Id}", c);
});

// PUT: upvote post
app.MapPut("/api/posts/{id:int}/upvote", async (int id, AppDb db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) return Results.NotFound();

    post.Score++;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// PUT: downvote post
app.MapPut("/api/posts/{id:int}/downvote", async (int id, AppDb db) =>
{
    var post = await db.Posts.FindAsync(id);
    if (post is null) return Results.NotFound();

    post.Score--;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// PUT: upvote comment
app.MapPut("/api/comments/{id:int}/upvote", async (int id, AppDb db) =>
{
    var c = await db.Comments.FindAsync(id);
    if (c is null) return Results.NotFound();

    c.Score++;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// PUT: downvote comment
app.MapPut("/api/comments/{id:int}/downvote", async (int id, AppDb db) =>
{
    var c = await db.Comments.FindAsync(id);
    if (c is null) return Results.NotFound();

    c.Score--;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// ---------- DTOs ----------
record PostDto(string Title, string Author, string? Text, string? Url);
record CommentDto(string Author, string Text);
