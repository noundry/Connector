using Microsoft.Extensions.DependencyInjection;
using Noundry.Connector.Authentication;
using Noundry.Connector.Extensions;
using Noundry.Connector.Samples.GitHub;
using Noundry.Connector.Samples.GitHub.Models;
using System.Text;
using System.Text.Json;

namespace Noundry.Connector.Tests.GitHub;

[TestFixture]
public class GitHubApiTests
{
    private ServiceProvider? _serviceProvider;
    private IGitHubApi? _gitHubApi;
    private const string TestToken = "github_pat_test_token"; // Use a test token for integration tests

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        
        services.AddConnector<IGitHubApi, GitHubRepository, long>(options =>
        {
            options.BaseUrl = "https://api.github.com";
            options.DefaultHeaders["User-Agent"] = "Noundry.Connector/1.0.0";
            options.DefaultHeaders["Accept"] = "application/vnd.github.v3+json";
        }, new TokenAuthenticationProvider(TestToken));

        _serviceProvider = services.BuildServiceProvider();
        _gitHubApi = _serviceProvider.GetRequiredService<IGitHubApi>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public async Task GetUsersAsync_ReturnsUsers()
    {
        if (_gitHubApi == null) Assert.Fail("GitHub API not initialized");

        var users = await _gitHubApi.GetUsersAsync();
        var userList = users.ToList();

        Assert.That(userList, Is.Not.Empty);
        Assert.That(userList.First().Login, Is.Not.Empty);
    }

    [Test]
    public async Task GetUserAsync_ValidUsername_ReturnsUser()
    {
        if (_gitHubApi == null) Assert.Fail("GitHub API not initialized");

        var user = await _gitHubApi.GetUserAsync("octocat");

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Login, Is.EqualTo("octocat"));
        Assert.That(user.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetUserRepositoriesAsync_ValidUsername_ReturnsRepositories()
    {
        if (_gitHubApi == null) Assert.Fail("GitHub API not initialized");

        var repositories = await _gitHubApi.GetUserRepositoriesAsync("octocat");
        var repoList = repositories.ToList();

        Assert.That(repoList, Is.Not.Empty);
        Assert.That(repoList.First().Name, Is.Not.Empty);
        Assert.That(repoList.First().Owner, Is.Not.Null);
        Assert.That(repoList.First().Owner!.Login, Is.EqualTo("octocat"));
    }

    [Test]
    public async Task GetRepositoryAsync_ValidOwnerAndRepo_ReturnsRepository()
    {
        if (_gitHubApi == null) Assert.Fail("GitHub API not initialized");

        var repository = await _gitHubApi.GetRepositoryAsync("octocat", "Hello-World");

        Assert.That(repository, Is.Not.Null);
        Assert.That(repository.Name, Is.EqualTo("Hello-World"));
        Assert.That(repository.Owner, Is.Not.Null);
        Assert.That(repository.Owner!.Login, Is.EqualTo("octocat"));
    }

    [Test]
    public void GitHubUser_JsonSerialization_WorksCorrectly()
    {
        var user = new GitHubUser
        {
            Id = 1,
            Login = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            PublicRepos = 5,
            Followers = 10,
            Following = 8
        };

        var json = JsonSerializer.Serialize(user);
        var deserializedUser = JsonSerializer.Deserialize<GitHubUser>(json);

        Assert.That(deserializedUser, Is.Not.Null);
        Assert.That(deserializedUser.Login, Is.EqualTo("testuser"));
        Assert.That(deserializedUser.Name, Is.EqualTo("Test User"));
        Assert.That(deserializedUser.PublicRepos, Is.EqualTo(5));
    }

    [Test]
    public void GitHubRepository_JsonSerialization_WorksCorrectly()
    {
        var repository = new GitHubRepository
        {
            Id = 1,
            Name = "test-repo",
            FullName = "testuser/test-repo",
            Description = "A test repository",
            Private = false,
            StargazersCount = 100,
            ForksCount = 25
        };

        var json = JsonSerializer.Serialize(repository);
        var deserializedRepo = JsonSerializer.Deserialize<GitHubRepository>(json);

        Assert.That(deserializedRepo, Is.Not.Null);
        Assert.That(deserializedRepo.Name, Is.EqualTo("test-repo"));
        Assert.That(deserializedRepo.FullName, Is.EqualTo("testuser/test-repo"));
        Assert.That(deserializedRepo.StargazersCount, Is.EqualTo(100));
    }

    [Test]
    public void CreateRepositoryRequest_JsonSerialization_WorksCorrectly()
    {
        var request = new CreateRepositoryRequest
        {
            Name = "new-repo",
            Description = "A new repository",
            Private = true,
            HasIssues = false,
            HasWiki = true
        };

        var json = JsonSerializer.Serialize(request);
        
        Assert.That(json, Contains.Substring("\"name\":\"new-repo\""));
        Assert.That(json, Contains.Substring("\"private\":true"));
        Assert.That(json, Contains.Substring("\"has_issues\":false"));
    }
}