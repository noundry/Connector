using Microsoft.Extensions.DependencyInjection;
using Noundry.Connector.Authentication;
using Noundry.Connector.Extensions;
using Samples.JSONPlaceholder;

namespace Noundry.Connector.Tests.Integration;

[TestFixture]
public class ReadmeExampleTests
{
    [Test]
    public void QuickStartExample_CompileAndConfigureSuccessfully()
    {
        var services = new ServiceCollection();

        // This is the exact example from README Quick Start section
        services.AddTokenAuthentication("test-token");
        services.AddConnector<IJsonPlaceholderApi>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
            options.DefaultHeaders["User-Agent"] = "MyApp/1.0.0";
        });

        var serviceProvider = services.BuildServiceProvider();
        var apiClient = serviceProvider.GetRequiredService<IJsonPlaceholderApi>();

        Assert.That(apiClient, Is.Not.Null);
        Assert.That(serviceProvider.GetService<IAuthenticationProvider>(), Is.Not.Null);
        Assert.That(serviceProvider.GetService<IAuthenticationProvider>(), Is.InstanceOf<TokenAuthenticationProvider>());
    }

    [Test]
    public void OAuthExample_CompileAndConfigureSuccessfully()
    {
        var services = new ServiceCollection();

        // This is the exact OAuth example from README
        services.AddOAuthAuthentication(config =>
        {
            config.ClientId = "test-client-id";
            config.ClientSecret = "test-client-secret";
            config.TokenEndpoint = "https://api.example.com/oauth/token";
            config.Scope = "read write";
        });

        services.AddConnector<IJsonPlaceholderApi>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
        });

        var serviceProvider = services.BuildServiceProvider();
        var authProvider = serviceProvider.GetService<IAuthenticationProvider>();

        Assert.That(authProvider, Is.Not.Null);
        Assert.That(authProvider, Is.InstanceOf<OAuthAuthenticationProvider>());
    }

    [Test]
    public async Task TokenAuthentication_WorksAsDocumented()
    {
        const string testToken = "test-token-12345";
        var tokenProvider = new TokenAuthenticationProvider(testToken);

        var token = await tokenProvider.GetAccessTokenAsync();
        
        Assert.That(token, Is.EqualTo(testToken));
        
        // Should not throw
        Assert.DoesNotThrowAsync(async () => await tokenProvider.RefreshTokenAsync());
    }

    [Test]
    public async Task JsonPlaceholderIntegration_WorksAsDocumentedInReadme()
    {
        var services = new ServiceCollection();
        
        // Exact setup from README
        services.AddTokenAuthentication("no-auth-required");
        services.AddConnector<IJsonPlaceholderApi>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
            options.DefaultHeaders["User-Agent"] = "Noundry.Connector.Tests/1.0.0";
        });

        services.AddTransient<JsonPlaceholderClient>();

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<JsonPlaceholderClient>();

        // Test the LINQ example from README
        var users = await client.GetUsersAsync();
        
        var bizUsers = users.Where(u => u.Email.EndsWith(".biz")).ToList();
        var orgUsers = users.Where(u => u.Email.EndsWith(".org")).ToList();

        // Verify LINQ operations work as documented
        Assert.That(users, Is.Not.Empty);
        Assert.That(bizUsers.Count + orgUsers.Count, Is.GreaterThan(0));
        
        // Verify strongly-typed access works
        var firstUser = users.First();
        Assert.That(firstUser.Email, Is.Not.Empty);
        Assert.That(firstUser.Address.City, Is.Not.Empty);
    }

    [Test] 
    public async Task LinqQueryExample_WorksAsDocumented()
    {
        var services = new ServiceCollection();
        
        services.AddTokenAuthentication("no-auth-required");
        services.AddConnector<IJsonPlaceholderApi>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
        });

        services.AddTransient<JsonPlaceholderClient>();

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<JsonPlaceholderClient>();

        // This is a simplified version of the complex LINQ example from README
        var posts = await client.GetPostsAsync();
        var users = await client.GetUsersAsync();

        // Cross-entity LINQ query as shown in README
        var userPostCounts = users
            .Select(u => new {
                User = u,
                PostCount = posts.Count(p => p.UserId == u.Id)
            })
            .Where(up => up.PostCount > 0)
            .OrderByDescending(up => up.PostCount)
            .Take(5)
            .ToList();

        Assert.That(userPostCounts, Is.Not.Empty);
        Assert.That(userPostCounts.All(up => up.PostCount > 0), Is.True);
        Assert.That(userPostCounts.Count, Is.LessThanOrEqualTo(5));
    }
}