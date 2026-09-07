using Microsoft.EntityFrameworkCore.Storage;
using System.Data;


namespace Hotel_Booking.Repositories.IRepositories
{
    
    public interface IUnitOfWork 
    {
        Task<IDbContextTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
