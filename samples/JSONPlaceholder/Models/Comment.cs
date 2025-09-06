using System.Text.Json.Serialization;

namespace Samples.JSONPlaceholder.Models;

public class Comment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("postId")]
    public int PostId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}