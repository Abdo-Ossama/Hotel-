using System.Linq.Expressions;

namespace Hotel_Booking.Repositories.IRepositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>?[]? includes = null, 
            bool tracked = true,
              Func<IQueryable<T>, IQueryable<T>>? includeThen = null,
            CancellationToken cancellationToken = default);

        IQueryable<T> GetQueryable(
                Expression<Func<T, bool>>? expression = null,
                List<Expression<Func<T, object>>>? includes = null,
                 Func<IQueryable<T>, IQueryable<T>>? thenInclude = null,
                bool tracked = false,
                CancellationToken cancellationToken = default);


        Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            Func<IQueryable<T>, IQueryable<T>>? includeThen = null,
            CancellationToken cancellationToken = default);

        Task CreateAysnc(T entity, CancellationToken cancellationToken = default);

        void Update(T entity);

        void Delete(T entity);

        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        Task<bool> AnyAsync(
            Expression<Func<T, bool>> expression,
            CancellationToken cancellationToken = default);
    }
}