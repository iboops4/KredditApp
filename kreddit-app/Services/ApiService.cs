using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace kreddit_app.Data
{
    // ---------- DTOs (tilpasset UI’et) ----------

    public class PostListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int Score { get; set; }
        public string? Text { get; set; }
        public string? Url { get; set; }

        // UI-state
        public int UserVote { get; set; } = 0;

        // Hjælpefelter (må gerne blive)
        public string Content => !string.IsNullOrWhiteSpace(Text) ? Text! : (Url ?? "");
        public int Votes => Score;              // legacy alias
        public DateTime? Created => CreatedAt;  // legacy alias
    }

    public class PostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int Score { get; set; }
        public string? Text { get; set; }
        public string? Url { get; set; }
        public List<CommentDetailDto> Comments { get; set; } = new();

        public string Content => !string.IsNullOrWhiteSpace(Text) ? Text! : (Url ?? "");
        public int Votes => Score;
        public DateTime? Created => CreatedAt;
    }

    public class CommentDetailDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Votes => Score;
    }

    public class CreateCommentRequest
    {
        public int PostId { get; set; }
        public string Author { get; set; } = "";
        public string Content { get; set; } = "";
    }

    // ---------- Service ----------

    public class ApiService
    {
        private readonly HttpClient http;
        private readonly string baseAPI = "";

        public ApiService(HttpClient http, IConfiguration configuration)
        {
            this.http = http;
            this.baseAPI = configuration["base_api"]!;
        }

        // --- LISTE: understøt ?search= ---
        public async Task<PostListDto[]> GetPosts(string? search = null)
        {
            var url = $"{baseAPI}posts";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"?search={Uri.EscapeDataString(search)}";

            try
            {
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return Array.Empty<PostListDto>();
                var data = await res.Content.ReadFromJsonAsync<PostListDto[]>();
                return data ?? Array.Empty<PostListDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetPosts failed: {ex.Message}");
                return Array.Empty<PostListDto>();
            }
        }

        // Detalje
        public async Task<PostDetailDto?> GetPost(int id)
        {
            var url = $"{baseAPI}posts/{id}";
            try
            {
                var res = await http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return null;
                return await res.Content.ReadFromJsonAsync<PostDetailDto?>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetPost({id}) failed: {ex.Message}");
                return null;
            }
        }

        // Kommentar – { author, text }
        public async Task<bool> CreateComment(int postId, string author, string text)
        {
            var url = $"{baseAPI}posts/{postId}/comments";
            try
            {
                var resp = await http.PostAsJsonAsync(url, new { author, text });
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CreateComment failed: {ex.Message}");
                return false;
            }
        }

        // Post-votes
        public async Task UpvotePost(int id)
        {
            var url = $"{baseAPI}posts/{id}/upvote";
            try { await http.PutAsync(url, null); }
            catch (Exception ex) { Console.WriteLine($"❌ UpvotePost failed: {ex.Message}"); }
        }
        public async Task DownvotePost(int id)
        {
            var url = $"{baseAPI}posts/{id}/downvote";
            try { await http.PutAsync(url, null); }
            catch (Exception ex) { Console.WriteLine($"❌ DownvotePost failed: {ex.Message}"); }
        }

        // Comment-votes
        public async Task UpvoteComment(int commentId)
        {
            var url = $"{baseAPI}comments/{commentId}/upvote";
            try { await http.PutAsync(url, null); }
            catch (Exception ex) { Console.WriteLine($"❌ UpvoteComment failed: {ex.Message}"); }
        }
        public async Task DownvoteComment(int commentId)
        {
            var url = $"{baseAPI}comments/{commentId}/downvote";
            try { await http.PutAsync(url, null); }
            catch (Exception ex) { Console.WriteLine($"❌ DownvoteComment failed: {ex.Message}"); }
        }

        // Ny post
        public async Task<bool> CreatePost(string title, string author, string? text, string? url)
        {
            var urlEndpoint = $"{baseAPI}posts";
            try
            {
                var dto = new { Title = title, Author = author, Text = text, Url = url };
                var resp = await http.PostAsJsonAsync(urlEndpoint, dto);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CreatePost failed: {ex.Message}");
                return false;
            }
        }

        // ----- Convenience for UI -----
        public async Task<List<PostListDto>> GetPostsAsync(string? search = null)
            => (await GetPosts(search)).ToList();

        public Task<PostDetailDto?> GetPostAsync(int id)
            => GetPost(id);

        public async Task<bool> CreateCommentAsync(CreateCommentRequest req)
            => await CreateComment(req.PostId, req.Author, req.Content);

        public async Task<bool> VotePostAsync(int postId, int delta)
        {
            if (delta >= 0) await UpvotePost(postId);
            else await DownvotePost(postId);
            return true;
        }

        public async Task<bool> VoteCommentAsync(int commentId, int delta)
        {
            if (delta >= 0) await UpvoteComment(commentId);
            else await DownvoteComment(commentId);
            return true;
        }
    }
}
