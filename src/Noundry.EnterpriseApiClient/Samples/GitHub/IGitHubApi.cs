using Noundry.EnterpriseApiClient.Samples.GitHub.Models;
using Refit;
using System.Text.Json.Serialization;

namespace Noundry.EnterpriseApiClient.Samples.GitHub;

public interface IGitHubApi
{
    [Get("/user")]
    Task<GitHubUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    [Get("/users/{username}")]
    Task<GitHubUser> GetUserAsync(string username, CancellationToken cancellationToken = default);

    [Get("/users")]
    Task<IEnumerable<GitHubUser>> GetUsersAsync([Query] string? since = null, CancellationToken cancellationToken = default);

    [Get("/user/repos")]
    Task<IEnumerable<GitHubRepository>> GetCurrentUserRepositoriesAsync([Query] string? type = null, CancellationToken cancellationToken = default);

    [Get("/users/{username}/repos")]
    Task<IEnumerable<GitHubRepository>> GetUserRepositoriesAsync(string username, [Query] string? type = null, CancellationToken cancellationToken = default);

    [Get("/repos/{owner}/{repo}")]
    Task<GitHubRepository> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);

    [Post("/user/repos")]
    Task<GitHubRepository> CreateRepositoryAsync([Body] CreateRepositoryRequest request, CancellationToken cancellationToken = default);

    [Patch("/repos/{owner}/{repo}")]
    Task<GitHubRepository> UpdateRepositoryAsync(string owner, string repo, [Body] UpdateRepositoryRequest request, CancellationToken cancellationToken = default);

    [Delete("/repos/{owner}/{repo}")]
    Task DeleteRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);
}

public class CreateRepositoryRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("has_issues")]
    public bool HasIssues { get; set; } = true;

    [JsonPropertyName("has_projects")]
    public bool HasProjects { get; set; } = true;

    [JsonPropertyName("has_wiki")]
    public bool HasWiki { get; set; } = true;
}

public class UpdateRepositoryRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("private")]
    public bool? Private { get; set; }

    [JsonPropertyName("has_issues")]
    public bool? HasIssues { get; set; }

    [JsonPropertyName("has_projects")]
    public bool? HasProjects { get; set; }

    [JsonPropertyName("has_wiki")]
    public bool? HasWiki { get; set; }
}