using System.Linq.Expressions;

namespace Hotel_Booking.Repositories.IRepositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? expression = null,
        Expression<Func<T, object>>?[]? includes = null,
        bool tracked = true,
        CancellationToken cancellationToken = default);



         Task<IEnumerable<T>> GetOneAsync(  
        Expression<Func<T, bool>>? expression = null,
        Expression<Func<T, object>>?[]? includes = null,
        bool tracked = true,
        CancellationToken cancellationToken = default);



        Task<T> Create(T entity, CancellationToken cancellationToken);
        Task<T> Update(T entity);
        Task<T> Delete(T entity);
    }
}
