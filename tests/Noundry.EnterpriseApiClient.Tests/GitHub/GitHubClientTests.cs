using Microsoft.Extensions.DependencyInjection;
using Noundry.EnterpriseApiClient.Authentication;
using Noundry.EnterpriseApiClient.Extensions;
using Noundry.EnterpriseApiClient.Samples.GitHub;

namespace Noundry.EnterpriseApiClient.Tests.GitHub;

[TestFixture]
public class GitHubClientTests
{
    private ServiceProvider? _serviceProvider;
    private GitHubClient? _gitHubClient;
    private const string TestToken = "github_pat_test_token";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        
        services.AddEnterpriseApiClient<IGitHubApi, object, long>(options =>
        {
            options.BaseUrl = "https://api.github.com";
            options.DefaultHeaders["User-Agent"] = "Noundry.EnterpriseApiClient/1.0.0";
            options.DefaultHeaders["Accept"] = "application/vnd.github.v3+json";
        }, new TokenAuthenticationProvider(TestToken));

        services.AddTransient<GitHubClient>();

        _serviceProvider = services.BuildServiceProvider();
        _gitHubClient = _serviceProvider.GetRequiredService<GitHubClient>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task GetUsersAsync_ReturnsUsersCollection()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var users = await _gitHubClient.GetUsersAsync();
        var usersList = users.ToList();

        Assert.That(usersList, Is.Not.Empty);
        Assert.That(usersList.Count, Is.GreaterThan(0));

        var firstUser = usersList.First();
        Assert.That(firstUser.Login, Is.Not.Empty);
        Assert.That(firstUser.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetUserAsync_WithValidUsername_ReturnsCorrectUser()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var user = await _gitHubClient.GetUserAsync("octocat");

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Login, Is.EqualTo("octocat"));
        Assert.That(user.Id, Is.EqualTo(583231));
        Assert.That(user.HtmlUrl, Is.EqualTo("https://github.com/octocat"));
    }

    [Test]
    public async Task GetUserRepositoriesAsync_WithValidUsername_ReturnsRepositories()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var repositories = await _gitHubClient.GetUserRepositoriesAsync("octocat");
        var reposList = repositories.ToList();

        Assert.That(reposList, Is.Not.Empty);
        
        var firstRepo = reposList.First();
        Assert.That(firstRepo.Name, Is.Not.Empty);
        Assert.That(firstRepo.Owner, Is.Not.Null);
        Assert.That(firstRepo.Owner!.Login, Is.EqualTo("octocat"));
    }

    [Test]
    public async Task GetRepositoryAsync_WithValidOwnerAndRepo_ReturnsRepository()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var repository = await _gitHubClient.GetRepositoryAsync("octocat", "Hello-World");

        Assert.That(repository, Is.Not.Null);
        Assert.That(repository.Name, Is.EqualTo("Hello-World"));
        Assert.That(repository.FullName, Is.EqualTo("octocat/Hello-World"));
        Assert.That(repository.Owner, Is.Not.Null);
        Assert.That(repository.Owner!.Login, Is.EqualTo("octocat"));
    }

    [Test]
    public async Task GetUsersAsync_CanBeFilteredWithLinq()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var users = await _gitHubClient.GetUsersAsync();
        
        var filteredUsers = users
            .Where(u => u.Id < 100)
            .Take(5)
            .ToList();

        Assert.That(filteredUsers, Is.Not.Empty);
        Assert.That(filteredUsers.Count, Is.LessThanOrEqualTo(5));
        Assert.That(filteredUsers.All(u => u.Id < 100), Is.True);
    }

    [Test]
    public async Task GetUserRepositoriesAsync_CanBeFilteredWithLinq()
    {
        if (_gitHubClient == null) Assert.Fail("GitHub client not initialized");

        var repositories = await _gitHubClient.GetUserRepositoriesAsync("octocat");
        
        var publicRepos = repositories
            .Where(r => !r.Private)
            .OrderByDescending(r => r.StargazersCount)
            .Take(3)
            .ToList();

        Assert.That(publicRepos, Is.Not.Empty);
        Assert.That(publicRepos.All(r => !r.Private), Is.True);
        
        if (publicRepos.Count > 1)
        {
            Assert.That(publicRepos[0].StargazersCount, Is.GreaterThanOrEqualTo(publicRepos[1].StargazersCount));
        }
    }
}