namespace FileLink.Server.Core.Repositories
{
    // Defines a generic repository interface for entity operations.
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        // Gets an entity by its unique identifier.
        Task<TEntity> GetByIdAsync(TKey id);
        
        // Gets all entities.
        Task<IEnumerable<TEntity>> GetAllAsync();
        
        // Adds a new entity.
        Task<bool> AddAsync(TEntity entity);
        
        // Updates an existing entity.
        Task<bool> UpdateAsync(TEntity entity);
        
        // Deletes an entity by its unique identifier.
        Task<bool> DeleteAsync(TKey id);
    }
}