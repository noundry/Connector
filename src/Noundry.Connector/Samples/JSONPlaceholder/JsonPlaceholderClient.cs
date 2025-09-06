using Noundry.Connector.Samples.JSONPlaceholder.Models;

namespace Noundry.Connector.Samples.JSONPlaceholder;

public class JsonPlaceholderClient
{
    private readonly IJsonPlaceholderApi _api;

    public JsonPlaceholderClient(IJsonPlaceholderApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    // User operations
    public async Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _api.GetUsersAsync(cancellationToken);
    }

    public async Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _api.GetUserAsync(id, cancellationToken);
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        return await _api.CreateUserAsync(user, cancellationToken);
    }

    public async Task<User> UpdateUserAsync(int id, User user, CancellationToken cancellationToken = default)
    {
        return await _api.UpdateUserAsync(id, user, cancellationToken);
    }

    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        await _api.DeleteUserAsync(id, cancellationToken);
    }

    // Post operations
    public async Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default)
    {
        return await _api.GetPostsAsync(cancellationToken);
    }

    public async Task<Post> GetPostAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _api.GetPostAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Post>> GetPostsByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _api.GetPostsByUserAsync(userId, cancellationToken);
    }

    public async Task<Post> CreatePostAsync(Post post, CancellationToken cancellationToken = default)
    {
        return await _api.CreatePostAsync(post, cancellationToken);
    }

    public async Task<Post> UpdatePostAsync(int id, Post post, CancellationToken cancellationToken = default)
    {
        return await _api.UpdatePostAsync(id, post, cancellationToken);
    }

    public async Task DeletePostAsync(int id, CancellationToken cancellationToken = default)
    {
        await _api.DeletePostAsync(id, cancellationToken);
    }

    // Comment operations
    public async Task<IEnumerable<Comment>> GetCommentsAsync(CancellationToken cancellationToken = default)
    {
        return await _api.GetCommentsAsync(cancellationToken);
    }

    public async Task<Comment> GetCommentAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _api.GetCommentAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Comment>> GetCommentsByPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        return await _api.GetCommentsByPostAsync(postId, cancellationToken);
    }

    public async Task<Comment> CreateCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        return await _api.CreateCommentAsync(comment, cancellationToken);
    }

    public async Task<Comment> UpdateCommentAsync(int id, Comment comment, CancellationToken cancellationToken = default)
    {
        return await _api.UpdateCommentAsync(id, comment, cancellationToken);
    }

    public async Task DeleteCommentAsync(int id, CancellationToken cancellationToken = default)
    {
        await _api.DeleteCommentAsync(id, cancellationToken);
    }
}