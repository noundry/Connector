using Microsoft.Extensions.DependencyInjection;
using Noundry.Connector.Authentication;
using Noundry.Connector.Extensions;
using Samples.JSONPlaceholder;
using Samples.JSONPlaceholder.Models;

namespace Noundry.Connector.Tests.JSONPlaceholder;

[TestFixture]
public class JsonPlaceholderLinqTests
{
    private ServiceProvider? _serviceProvider;
    private JsonPlaceholderClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        
        services.AddConnector<IJsonPlaceholderApi, User, int>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
            options.DefaultHeaders["User-Agent"] = "Noundry.Connector.Tests/1.0.0";
        }, new TokenAuthenticationProvider("no-auth-required"));

        services.AddTransient<JsonPlaceholderClient>();

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredService<JsonPlaceholderClient>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    #region User LINQ Tests

    [Test]
    public async Task Users_CanBeFilteredByEmailDomain()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var users = await _client.GetUsersAsync();
        
        var bizUsers = users
            .Where(u => u.Email.EndsWith(".biz"))
            .ToList();

        var orgUsers = users
            .Where(u => u.Email.EndsWith(".org"))
            .ToList();

        Assert.That(bizUsers, Is.Not.Empty);
        Assert.That(orgUsers, Is.Not.Empty);
        Assert.That(bizUsers.All(u => u.Email.EndsWith(".biz")), Is.True);
        Assert.That(orgUsers.All(u => u.Email.EndsWith(".org")), Is.True);
    }

    [Test]
    public async Task Users_CanBeGroupedByWebsiteDomain()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var users = await _client.GetUsersAsync();
        
        var usersByDomain = users
            .Where(u => !string.IsNullOrEmpty(u.Website))
            .GroupBy(u => u.Website.Split('.').LastOrDefault() ?? "unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.That(usersByDomain, Is.Not.Empty);
        Assert.That(usersByDomain.ContainsKey("org"), Is.True);
        Assert.That(usersByDomain.ContainsKey("biz"), Is.True);
    }

    [Test]
    public async Task Users_CanBeSortedByUsernameLength()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var users = await _client.GetUsersAsync();
        
        var sortedUsers = users
            .OrderBy(u => u.Username.Length)
            .ThenBy(u => u.Username)
            .ToList();

        Assert.That(sortedUsers, Is.Not.Empty);
        Assert.That(sortedUsers.Count, Is.EqualTo(10));
        
        // Verify sorting
        for (int i = 1; i < sortedUsers.Count; i++)
        {
            var current = sortedUsers[i];
            var previous = sortedUsers[i - 1];
            
            Assert.That(current.Username.Length >= previous.Username.Length, 
                $"User at index {i} has shorter username than previous user");
        }
    }

    [Test]
    public async Task Users_CanBeFilteredByCompanyName()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var users = await _client.GetUsersAsync();
        
        var usersWithCompanyName = users
            .Where(u => u.Company.Name.Contains("Group") || u.Company.Name.Contains("Inc"))
            .Select(u => new { u.Name, u.Username, CompanyName = u.Company.Name })
            .ToList();

        Assert.That(usersWithCompanyName, Is.Not.Empty);
        Assert.That(usersWithCompanyName.All(u => 
            u.CompanyName.Contains("Group") || u.CompanyName.Contains("Inc")), Is.True);
    }

    #endregion

    #region Post LINQ Tests

    [Test]
    public async Task Posts_CanBeFilteredByTitleLength()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var posts = await _client.GetPostsAsync();
        
        var shortTitlePosts = posts
            .Where(p => p.Title.Length <= 30)
            .OrderBy(p => p.Title.Length)
            .Take(10)
            .ToList();

        var longTitlePosts = posts
            .Where(p => p.Title.Length > 50)
            .OrderByDescending(p => p.Title.Length)
            .Take(5)
            .ToList();

        Assert.That(shortTitlePosts, Is.Not.Empty);
        Assert.That(longTitlePosts, Is.Not.Empty);
        Assert.That(shortTitlePosts.All(p => p.Title.Length <= 30), Is.True);
        Assert.That(longTitlePosts.All(p => p.Title.Length > 50), Is.True);
    }

    [Test]
    public async Task Posts_CanBeGroupedByUserId()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var posts = await _client.GetPostsAsync();
        
        var postsByUser = posts
            .GroupBy(p => p.UserId)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.That(postsByUser, Is.Not.Empty);
        Assert.That(postsByUser.Count, Is.EqualTo(10)); // 10 users
        Assert.That(postsByUser.All(kvp => kvp.Value == 10), Is.True); // Each user has 10 posts
    }

    [Test]
    public async Task Posts_CanBeFilteredByKeywords()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var posts = await _client.GetPostsAsync();
        
        var postsWithKeywords = posts
            .Where(p => p.Title.Contains("et") || p.Body.Contains("ut"))
            .Select(p => new { 
                p.Id, 
                p.Title, 
                BodyPreview = p.Body.Length > 50 ? p.Body.Substring(0, 50) + "..." : p.Body,
                p.UserId,
                FullBody = p.Body
            })
            .OrderBy(p => p.UserId)
            .ThenBy(p => p.Id)
            .ToList();

        Assert.That(postsWithKeywords, Is.Not.Empty);
        Assert.That(postsWithKeywords.All(p => 
            p.Title.Contains("et") || p.FullBody.Contains("ut")), Is.True);
    }

    [Test]
    public async Task Posts_CanCalculateStatistics()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var posts = await _client.GetPostsAsync();
        
        var stats = new
        {
            TotalPosts = posts.Count(),
            AverageTitleLength = posts.Average(p => p.Title.Length),
            AverageBodyLength = posts.Average(p => p.Body.Length),
            MaxTitleLength = posts.Max(p => p.Title.Length),
            MinTitleLength = posts.Min(p => p.Title.Length),
            UsersWithPosts = posts.Select(p => p.UserId).Distinct().Count()
        };

        Assert.That(stats.TotalPosts, Is.EqualTo(100));
        Assert.That(stats.AverageTitleLength, Is.GreaterThan(0));
        Assert.That(stats.AverageBodyLength, Is.GreaterThan(0));
        Assert.That(stats.UsersWithPosts, Is.EqualTo(10));
        Assert.That(stats.MaxTitleLength, Is.GreaterThan(stats.MinTitleLength));
    }

    #endregion

    #region Comment LINQ Tests

    [Test]
    public async Task Comments_CanBeFilteredByEmailProvider()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var comments = await _client.GetCommentsAsync();
        
        var gmailComments = comments
            .Where(c => c.Email.Contains("gmail.com"))
            .Take(10)
            .ToList();

        var otherEmailComments = comments
            .Where(c => !c.Email.Contains("gmail.com") && c.Email.Contains("@"))
            .GroupBy(c => c.Email.Split('@').LastOrDefault())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.That(gmailComments.Count, Is.LessThanOrEqualTo(10));
        Assert.That(otherEmailComments, Is.Not.Empty);
    }

    [Test]
    public async Task Comments_CanBeAnalyzedByPost()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var comments = await _client.GetCommentsAsync();
        
        var commentAnalysis = comments
            .GroupBy(c => c.PostId)
            .Select(g => new {
                PostId = g.Key,
                CommentCount = g.Count(),
                AverageCommentLength = g.Average(c => c.Body.Length),
                LongestComment = g.OrderByDescending(c => c.Body.Length).First().Body.Length,
                ShortestComment = g.OrderBy(c => c.Body.Length).First().Body.Length
            })
            .OrderByDescending(a => a.CommentCount)
            .Take(10)
            .ToList();

        Assert.That(commentAnalysis, Is.Not.Empty);
        Assert.That(commentAnalysis.All(a => a.CommentCount > 0), Is.True);
        Assert.That(commentAnalysis.All(a => a.LongestComment >= a.ShortestComment), Is.True);
    }

    [Test]
    public async Task Comments_CanBeCombinedWithPosts()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var comments = await _client.GetCommentsAsync();
        var posts = await _client.GetPostsAsync();
        
        var postCommentSummary = posts
            .Join(comments.GroupBy(c => c.PostId),
                  post => post.Id,
                  commentGroup => commentGroup.Key,
                  (post, commentGroup) => new {
                      PostTitle = post.Title,
                      PostId = post.Id,
                      UserId = post.UserId,
                      CommentCount = commentGroup.Count(),
                      CommenterEmails = commentGroup.Select(c => c.Email).ToList()
                  })
            .Where(pcs => pcs.CommentCount >= 5)
            .OrderByDescending(pcs => pcs.CommentCount)
            .Take(5)
            .ToList();

        Assert.That(postCommentSummary, Is.Not.Empty);
        Assert.That(postCommentSummary.All(pcs => pcs.CommentCount >= 5), Is.True);
        Assert.That(postCommentSummary.All(pcs => pcs.CommenterEmails.Count == pcs.CommentCount), Is.True);
    }

    #endregion

    #region Cross-Entity LINQ Tests

    [Test]
    public async Task CrossEntity_CanAnalyzeUserActivityByPostsAndComments()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var users = await _client.GetUsersAsync();
        var posts = await _client.GetPostsAsync();
        var comments = await _client.GetCommentsAsync();
        
        var userActivity = users
            .Select(u => new {
                User = u,
                PostCount = posts.Count(p => p.UserId == u.Id),
                CommentCount = comments.Count(c => c.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase))
            })
            .Select(ua => new {
                ua.User.Name,
                ua.User.Username,
                ua.User.Email,
                ua.PostCount,
                ua.CommentCount,
                TotalActivity = ua.PostCount + ua.CommentCount,
                ActivityRatio = ua.CommentCount > 0 ? (double)ua.PostCount / ua.CommentCount : ua.PostCount
            })
            .OrderByDescending(ua => ua.TotalActivity)
            .ToList();

        Assert.That(userActivity, Is.Not.Empty);
        Assert.That(userActivity.Count, Is.EqualTo(10));
        Assert.That(userActivity.All(ua => ua.PostCount > 0), Is.True);
    }

    [Test]
    public async Task CrossEntity_CanFindMostActivePostsByCommentCount()
    {
        if (_client == null) Assert.Fail("Client not initialized");

        var posts = await _client.GetPostsAsync();
        var comments = await _client.GetCommentsAsync();
        var users = await _client.GetUsersAsync();
        
        var mostActivePosts = posts
            .Select(p => new {
                Post = p,
                CommentCount = comments.Count(c => c.PostId == p.Id),
                Author = users.FirstOrDefault(u => u.Id == p.UserId)
            })
            .Where(p => p.Author != null)
            .OrderByDescending(p => p.CommentCount)
            .Take(10)
            .Select(p => new {
                p.Post.Id,
                p.Post.Title,
                AuthorName = p.Author!.Name,
                AuthorUsername = p.Author.Username,
                p.CommentCount,
                TitleLength = p.Post.Title.Length,
                BodyLength = p.Post.Body.Length
            })
            .ToList();

        Assert.That(mostActivePosts, Is.Not.Empty);
        Assert.That(mostActivePosts.Count, Is.LessThanOrEqualTo(10));
        Assert.That(mostActivePosts.All(p => !string.IsNullOrEmpty(p.AuthorName)), Is.True);
    }

    #endregion
}