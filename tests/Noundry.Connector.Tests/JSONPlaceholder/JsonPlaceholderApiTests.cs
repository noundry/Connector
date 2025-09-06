using Microsoft.Extensions.DependencyInjection;
using Noundry.Connector.Authentication;
using Noundry.Connector.Extensions;
using Noundry.Connector.Samples.JSONPlaceholder;
using Noundry.Connector.Samples.JSONPlaceholder.Models;
using System.Text.Json;

namespace Noundry.Connector.Tests.JSONPlaceholder;

[TestFixture]
public class JsonPlaceholderApiTests
{
    private ServiceProvider? _serviceProvider;
    private IJsonPlaceholderApi? _api;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        
        services.AddConnector<IJsonPlaceholderApi, User, int>(options =>
        {
            options.BaseUrl = "https://jsonplaceholder.typicode.com";
            options.DefaultHeaders["User-Agent"] = "Noundry.Connector.Tests/1.0.0";
        }, new TokenAuthenticationProvider("no-auth-required"));

        _serviceProvider = services.BuildServiceProvider();
        _api = _serviceProvider.GetRequiredService<IJsonPlaceholderApi>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    #region User Tests

    [Test]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var users = await _api.GetUsersAsync();
        var userList = users.ToList();

        Assert.That(userList, Is.Not.Empty);
        Assert.That(userList.Count, Is.EqualTo(10));
        
        var firstUser = userList.First();
        Assert.That(firstUser.Id, Is.GreaterThan(0));
        Assert.That(firstUser.Name, Is.Not.Empty);
        Assert.That(firstUser.Email, Is.Not.Empty);
    }

    [Test]
    public async Task GetUserAsync_ValidId_ReturnsUser()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var user = await _api.GetUserAsync(1);

        Assert.That(user, Is.Not.Null);
        Assert.That(user.Id, Is.EqualTo(1));
        Assert.That(user.Name, Is.EqualTo("Leanne Graham"));
        Assert.That(user.Username, Is.EqualTo("Bret"));
        Assert.That(user.Email, Is.EqualTo("Sincere@april.biz"));
        Assert.That(user.Address, Is.Not.Null);
        Assert.That(user.Company, Is.Not.Null);
    }

    [Test]
    public async Task CreateUserAsync_ValidUser_ReturnsCreatedUser()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var newUser = new User
        {
            Name = "Test User",
            Username = "testuser",
            Email = "test@example.com",
            Phone = "123-456-7890",
            Website = "test.com",
            Address = new Address
            {
                Street = "123 Test St",
                Suite = "Apt 1",
                City = "Test City",
                Zipcode = "12345",
                Geo = new Geo { Lat = "0.0", Lng = "0.0" }
            },
            Company = new Company
            {
                Name = "Test Company",
                CatchPhrase = "Test Phrase",
                Bs = "test business"
            }
        };

        var createdUser = await _api.CreateUserAsync(newUser);

        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser.Id, Is.EqualTo(11)); // JSONPlaceholder returns id 11 for new posts
        Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
        Assert.That(createdUser.Email, Is.EqualTo(newUser.Email));
    }

    [Test]
    public async Task UpdateUserAsync_ValidUser_ReturnsUpdatedUser()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var updatedUser = new User
        {
            Id = 1,
            Name = "Updated User",
            Username = "updateduser",
            Email = "updated@example.com",
            Address = new Address(),
            Company = new Company()
        };

        var result = await _api.UpdateUserAsync(1, updatedUser);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo(updatedUser.Name));
        Assert.That(result.Email, Is.EqualTo(updatedUser.Email));
    }

    [Test]
    public async Task DeleteUserAsync_ValidId_DoesNotThrow()
    {
        if (_api == null) Assert.Fail("API not initialized");

        Assert.DoesNotThrowAsync(async () => await _api.DeleteUserAsync(1));
    }

    #endregion

    #region Post Tests

    [Test]
    public async Task GetPostsAsync_ReturnsAllPosts()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var posts = await _api.GetPostsAsync();
        var postList = posts.ToList();

        Assert.That(postList, Is.Not.Empty);
        Assert.That(postList.Count, Is.EqualTo(100));
        
        var firstPost = postList.First();
        Assert.That(firstPost.Id, Is.GreaterThan(0));
        Assert.That(firstPost.Title, Is.Not.Empty);
        Assert.That(firstPost.Body, Is.Not.Empty);
        Assert.That(firstPost.UserId, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetPostAsync_ValidId_ReturnsPost()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var post = await _api.GetPostAsync(1);

        Assert.That(post, Is.Not.Null);
        Assert.That(post.Id, Is.EqualTo(1));
        Assert.That(post.UserId, Is.EqualTo(1));
        Assert.That(post.Title, Is.Not.Empty);
        Assert.That(post.Body, Is.Not.Empty);
    }

    [Test]
    public async Task GetPostsByUserAsync_ValidUserId_ReturnsUserPosts()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var posts = await _api.GetPostsByUserAsync(1);
        var postList = posts.ToList();

        Assert.That(postList, Is.Not.Empty);
        Assert.That(postList.Count, Is.EqualTo(10));
        Assert.That(postList.All(p => p.UserId == 1), Is.True);
    }

    [Test]
    public async Task CreatePostAsync_ValidPost_ReturnsCreatedPost()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var newPost = new Post
        {
            UserId = 1,
            Title = "Test Post",
            Body = "This is a test post body"
        };

        var createdPost = await _api.CreatePostAsync(newPost);

        Assert.That(createdPost, Is.Not.Null);
        Assert.That(createdPost.Id, Is.EqualTo(101)); // JSONPlaceholder returns id 101 for new posts
        Assert.That(createdPost.Title, Is.EqualTo(newPost.Title));
        Assert.That(createdPost.Body, Is.EqualTo(newPost.Body));
        Assert.That(createdPost.UserId, Is.EqualTo(newPost.UserId));
    }

    [Test]
    public async Task UpdatePostAsync_ValidPost_ReturnsUpdatedPost()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var updatedPost = new Post
        {
            Id = 1,
            UserId = 1,
            Title = "Updated Test Post",
            Body = "This is an updated test post body"
        };

        var result = await _api.UpdatePostAsync(1, updatedPost);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Title, Is.EqualTo(updatedPost.Title));
        Assert.That(result.Body, Is.EqualTo(updatedPost.Body));
    }

    [Test]
    public async Task DeletePostAsync_ValidId_DoesNotThrow()
    {
        if (_api == null) Assert.Fail("API not initialized");

        Assert.DoesNotThrowAsync(async () => await _api.DeletePostAsync(1));
    }

    #endregion

    #region Comment Tests

    [Test]
    public async Task GetCommentsAsync_ReturnsAllComments()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var comments = await _api.GetCommentsAsync();
        var commentList = comments.ToList();

        Assert.That(commentList, Is.Not.Empty);
        Assert.That(commentList.Count, Is.EqualTo(500));
        
        var firstComment = commentList.First();
        Assert.That(firstComment.Id, Is.GreaterThan(0));
        Assert.That(firstComment.PostId, Is.GreaterThan(0));
        Assert.That(firstComment.Name, Is.Not.Empty);
        Assert.That(firstComment.Email, Is.Not.Empty);
        Assert.That(firstComment.Body, Is.Not.Empty);
    }

    [Test]
    public async Task GetCommentAsync_ValidId_ReturnsComment()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var comment = await _api.GetCommentAsync(1);

        Assert.That(comment, Is.Not.Null);
        Assert.That(comment.Id, Is.EqualTo(1));
        Assert.That(comment.PostId, Is.EqualTo(1));
        Assert.That(comment.Name, Is.Not.Empty);
        Assert.That(comment.Email, Is.Not.Empty);
        Assert.That(comment.Body, Is.Not.Empty);
    }

    [Test]
    public async Task GetCommentsByPostAsync_ValidPostId_ReturnsPostComments()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var comments = await _api.GetCommentsByPostAsync(1);
        var commentList = comments.ToList();

        Assert.That(commentList, Is.Not.Empty);
        Assert.That(commentList.Count, Is.EqualTo(5));
        Assert.That(commentList.All(c => c.PostId == 1), Is.True);
    }

    [Test]
    public async Task CreateCommentAsync_ValidComment_ReturnsCreatedComment()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var newComment = new Comment
        {
            PostId = 1,
            Name = "Test Comment",
            Email = "test@example.com",
            Body = "This is a test comment body"
        };

        var createdComment = await _api.CreateCommentAsync(newComment);

        Assert.That(createdComment, Is.Not.Null);
        Assert.That(createdComment.Id, Is.EqualTo(501)); // JSONPlaceholder returns id 501 for new comments
        Assert.That(createdComment.Name, Is.EqualTo(newComment.Name));
        Assert.That(createdComment.Email, Is.EqualTo(newComment.Email));
        Assert.That(createdComment.Body, Is.EqualTo(newComment.Body));
        Assert.That(createdComment.PostId, Is.EqualTo(newComment.PostId));
    }

    [Test]
    public async Task UpdateCommentAsync_ValidComment_ReturnsUpdatedComment()
    {
        if (_api == null) Assert.Fail("API not initialized");

        var updatedComment = new Comment
        {
            Id = 1,
            PostId = 1,
            Name = "Updated Test Comment",
            Email = "updated@example.com",
            Body = "This is an updated test comment body"
        };

        var result = await _api.UpdateCommentAsync(1, updatedComment);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo(updatedComment.Name));
        Assert.That(result.Email, Is.EqualTo(updatedComment.Email));
        Assert.That(result.Body, Is.EqualTo(updatedComment.Body));
    }

    [Test]
    public async Task DeleteCommentAsync_ValidId_DoesNotThrow()
    {
        if (_api == null) Assert.Fail("API not initialized");

        Assert.DoesNotThrowAsync(async () => await _api.DeleteCommentAsync(1));
    }

    #endregion

    #region Serialization Tests

    [Test]
    public void User_JsonSerialization_WorksCorrectly()
    {
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Username = "testuser",
            Email = "test@example.com",
            Phone = "123-456-7890",
            Website = "test.com",
            Address = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                Zipcode = "12345"
            },
            Company = new Company
            {
                Name = "Test Company",
                CatchPhrase = "Test Phrase"
            }
        };

        var json = JsonSerializer.Serialize(user);
        var deserializedUser = JsonSerializer.Deserialize<User>(json);

        Assert.That(deserializedUser, Is.Not.Null);
        Assert.That(deserializedUser.Name, Is.EqualTo("Test User"));
        Assert.That(deserializedUser.Email, Is.EqualTo("test@example.com"));
        Assert.That(deserializedUser.Address.Street, Is.EqualTo("123 Test St"));
        Assert.That(deserializedUser.Company.Name, Is.EqualTo("Test Company"));
    }

    [Test]
    public void Post_JsonSerialization_WorksCorrectly()
    {
        var post = new Post
        {
            Id = 1,
            UserId = 1,
            Title = "Test Post",
            Body = "Test body"
        };

        var json = JsonSerializer.Serialize(post);
        var deserializedPost = JsonSerializer.Deserialize<Post>(json);

        Assert.That(deserializedPost, Is.Not.Null);
        Assert.That(deserializedPost.Title, Is.EqualTo("Test Post"));
        Assert.That(deserializedPost.UserId, Is.EqualTo(1));
    }

    [Test]
    public void Comment_JsonSerialization_WorksCorrectly()
    {
        var comment = new Comment
        {
            Id = 1,
            PostId = 1,
            Name = "Test Comment",
            Email = "test@example.com",
            Body = "Test comment body"
        };

        var json = JsonSerializer.Serialize(comment);
        var deserializedComment = JsonSerializer.Deserialize<Comment>(json);

        Assert.That(deserializedComment, Is.Not.Null);
        Assert.That(deserializedComment.Name, Is.EqualTo("Test Comment"));
        Assert.That(deserializedComment.PostId, Is.EqualTo(1));
    }

    #endregion
}