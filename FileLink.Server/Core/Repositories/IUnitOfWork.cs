using FileLink.Server.Authentication;
using FileLink.Server.Disk.DirectoryManagement;
using FileLink.Server.Disk.FileManagement;

namespace FileLink.Server.Core.Repositories
{
    // Coordinates operations across multiple repositories and manages transactions.
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IFileRepository Files { get; }
        IDirectoryRepository Directories { get; }
        
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveChangesAsync();
    }
}