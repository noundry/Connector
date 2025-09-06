using Microsoft.Extensions.DependencyInjection;
using Noundry.Connector.Authentication;
using Noundry.Connector.Extensions;
using Samples.GitHub;
using Samples.GitHub.Models;

const string githubToken = "github_pat_your_token_here"; // Replace with your GitHub token

var services = new ServiceCollection();

services.AddConnector<IGitHubApi, GitHubRepository, long>(options =>
{
    options.BaseUrl = "https://api.github.com";
    options.DefaultHeaders["User-Agent"] = "Noundry.Connector.Sample/1.0.0";
    options.DefaultHeaders["Accept"] = "application/vnd.github.v3+json";
}, new TokenAuthenticationProvider(githubToken));

services.AddTransient<GitHubClient>();

var serviceProvider = services.BuildServiceProvider();
var gitHubClient = serviceProvider.GetRequiredService<GitHubClient>();

try
{
    Console.WriteLine("=== Noundry Connector - GitHub Sample ===\n");

    // Get and display users
    Console.WriteLine("Fetching users...");
    var users = await gitHubClient.GetUsersAsync();
    var userList = users.Take(5).ToList();
    
    Console.WriteLine($"Found {userList.Count} users:");
    foreach (var user in userList)
    {
        Console.WriteLine($"  - {user.Login} (ID: {user.Id})");
    }

    // Get specific user details
    Console.WriteLine("\nFetching details for 'octocat'...");
    var octocatUser = await gitHubClient.GetUserAsync("octocat");
    Console.WriteLine($"User: {octocatUser.Login}");
    Console.WriteLine($"Name: {octocatUser.Name ?? "N/A"}");
    Console.WriteLine($"Company: {octocatUser.Company ?? "N/A"}");
    Console.WriteLine($"Public Repos: {octocatUser.PublicRepos}");
    Console.WriteLine($"Followers: {octocatUser.Followers}");

    // Get repositories and use LINQ
    Console.WriteLine("\nFetching repositories for 'octocat'...");
    var repositories = await gitHubClient.GetUserRepositoriesAsync("octocat");
    
    var topRepos = repositories
        .Where(r => !r.Private)
        .OrderByDescending(r => r.StargazersCount)
        .Take(3)
        .ToList();

    Console.WriteLine($"Top {topRepos.Count} repositories by stars:");
    foreach (var repo in topRepos)
    {
        Console.WriteLine($"  - {repo.Name} ({repo.StargazersCount} ⭐, {repo.Language ?? "Unknown"})");
        Console.WriteLine($"    {repo.Description ?? "No description"}");
    }

    // Get specific repository
    Console.WriteLine("\nFetching specific repository 'Hello-World'...");
    var helloWorldRepo = await gitHubClient.GetRepositoryAsync("octocat", "Hello-World");
    Console.WriteLine($"Repository: {helloWorldRepo.FullName}");
    Console.WriteLine($"Description: {helloWorldRepo.Description ?? "No description"}");
    Console.WriteLine($"Language: {helloWorldRepo.Language ?? "Unknown"}");
    Console.WriteLine($"Stars: {helloWorldRepo.StargazersCount}");
    Console.WriteLine($"Forks: {helloWorldRepo.ForksCount}");
    Console.WriteLine($"Created: {helloWorldRepo.CreatedAt:yyyy-MM-dd}");

    // Demonstrate LINQ queries on collections
    Console.WriteLine("\n=== LINQ Query Examples ===");
    
    var allRepos = await gitHubClient.GetUserRepositoriesAsync("octocat");
    
    Console.WriteLine($"\nTotal repositories: {allRepos.Count()}");
    
    var csharpRepos = allRepos.Where(r => r.Language == "C#").ToList();
    Console.WriteLine($"C# repositories: {csharpRepos.Count}");
    
    var recentRepos = allRepos
        .Where(r => r.UpdatedAt > DateTime.Now.AddYears(-1))
        .OrderByDescending(r => r.UpdatedAt)
        .Take(5)
        .ToList();
    Console.WriteLine($"Recently updated repositories (last year): {recentRepos.Count}");
    
    foreach (var repo in recentRepos)
    {
        Console.WriteLine($"  - {repo.Name} (updated: {repo.UpdatedAt:yyyy-MM-dd})");
    }

    Console.WriteLine("\n✅ Sample completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    
    if (githubToken == "github_pat_your_token_here")
    {
        Console.WriteLine("\n💡 Tip: Make sure to replace 'github_pat_your_token_here' with a valid GitHub personal access token.");
        Console.WriteLine("You can create one at: https://github.com/settings/tokens");
    }
}
finally
{
    serviceProvider.Dispose();
}