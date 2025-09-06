using Noundry.Connector.Samples.GitHub.Models;

namespace Noundry.Connector.Samples.GitHub;

public class GitHubClient
{
    private readonly IGitHubApi _api;

    public GitHubClient(IGitHubApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<GitHubUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return await _api.GetCurrentUserAsync(cancellationToken);
    }

    public async Task<GitHubUser> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _api.GetUserAsync(username, cancellationToken);
    }

    public async Task<IEnumerable<GitHubUser>> GetUsersAsync(string? since = null, CancellationToken cancellationToken = default)
    {
        return await _api.GetUsersAsync(since, cancellationToken);
    }

    public async Task<IEnumerable<GitHubRepository>> GetCurrentUserRepositoriesAsync(string? type = null, CancellationToken cancellationToken = default)
    {
        return await _api.GetCurrentUserRepositoriesAsync(type, cancellationToken);
    }

    public async Task<IEnumerable<GitHubRepository>> GetUserRepositoriesAsync(string username, string? type = null, CancellationToken cancellationToken = default)
    {
        return await _api.GetUserRepositoriesAsync(username, type, cancellationToken);
    }

    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        return await _api.GetRepositoryAsync(owner, repo, cancellationToken);
    }

    public async Task<GitHubRepository> CreateRepositoryAsync(CreateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        return await _api.CreateRepositoryAsync(request, cancellationToken);
    }

    public async Task<GitHubRepository> UpdateRepositoryAsync(string owner, string repo, UpdateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        return await _api.UpdateRepositoryAsync(owner, repo, request, cancellationToken);
    }

    public async Task DeleteRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        await _api.DeleteRepositoryAsync(owner, repo, cancellationToken);
    }
}