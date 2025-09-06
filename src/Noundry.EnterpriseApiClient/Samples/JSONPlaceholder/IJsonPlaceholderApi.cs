using Noundry.EnterpriseApiClient.Samples.JSONPlaceholder.Models;
using Refit;

namespace Noundry.EnterpriseApiClient.Samples.JSONPlaceholder;

public interface IJsonPlaceholderApi
{
    // Users
    [Get("/users")]
    Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    [Get("/users/{id}")]
    Task<User> GetUserAsync(int id, CancellationToken cancellationToken = default);

    [Post("/users")]
    Task<User> CreateUserAsync([Body] User user, CancellationToken cancellationToken = default);

    [Put("/users/{id}")]
    Task<User> UpdateUserAsync(int id, [Body] User user, CancellationToken cancellationToken = default);

    [Delete("/users/{id}")]
    Task DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    // Posts
    [Get("/posts")]
    Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default);

    [Get("/posts/{id}")]
    Task<Post> GetPostAsync(int id, CancellationToken cancellationToken = default);

    [Get("/users/{userId}/posts")]
    Task<IEnumerable<Post>> GetPostsByUserAsync(int userId, CancellationToken cancellationToken = default);

    [Post("/posts")]
    Task<Post> CreatePostAsync([Body] Post post, CancellationToken cancellationToken = default);

    [Put("/posts/{id}")]
    Task<Post> UpdatePostAsync(int id, [Body] Post post, CancellationToken cancellationToken = default);

    [Delete("/posts/{id}")]
    Task DeletePostAsync(int id, CancellationToken cancellationToken = default);

    // Comments
    [Get("/comments")]
    Task<IEnumerable<Comment>> GetCommentsAsync(CancellationToken cancellationToken = default);

    [Get("/comments/{id}")]
    Task<Comment> GetCommentAsync(int id, CancellationToken cancellationToken = default);

    [Get("/posts/{postId}/comments")]
    Task<IEnumerable<Comment>> GetCommentsByPostAsync(int postId, CancellationToken cancellationToken = default);

    [Post("/comments")]
    Task<Comment> CreateCommentAsync([Body] Comment comment, CancellationToken cancellationToken = default);

    [Put("/comments/{id}")]
    Task<Comment> UpdateCommentAsync(int id, [Body] Comment comment, CancellationToken cancellationToken = default);

    [Delete("/comments/{id}")]
    Task DeleteCommentAsync(int id, CancellationToken cancellationToken = default);
}