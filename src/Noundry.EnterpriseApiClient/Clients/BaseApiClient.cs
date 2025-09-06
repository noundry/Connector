using Refit;

namespace Noundry.EnterpriseApiClient.Clients;

public abstract class BaseApiClient<TEntity, TKey> : IApiClient<TEntity, TKey> where TEntity : class
{
    protected readonly IRefitClient<TEntity, TKey> RefitClient;

    protected BaseApiClient(IRefitClient<TEntity, TKey> refitClient)
    {
        RefitClient = refitClient ?? throw new ArgumentNullException(nameof(refitClient));
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await RefitClient.GetAllAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await RefitClient.GetByIdAsync(id, cancellationToken);
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return await RefitClient.CreateAsync(entity, cancellationToken);
    }

    public virtual async Task<TEntity> UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default)
    {
        return await RefitClient.UpdateAsync(id, entity, cancellationToken);
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await RefitClient.DeleteAsync(id, cancellationToken);
    }
}

public interface IRefitClient<TEntity, in TKey> where TEntity : class
{
    [Get("")]
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    [Get("/{id}")]
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    [Post("")]
    Task<TEntity> CreateAsync([Body] TEntity entity, CancellationToken cancellationToken = default);

    [Put("/{id}")]
    Task<TEntity> UpdateAsync(TKey id, [Body] TEntity entity, CancellationToken cancellationToken = default);

    [Delete("/{id}")]
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}