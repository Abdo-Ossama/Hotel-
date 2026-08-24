using Hotel_Booking.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Hotel_Booking.Repositories
{
    public class Repository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger<Repository<T>> _logger;

        public Repository(
            ApplicationDbContext context,
            ILogger<Repository<T>> logger)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        // Get All
        public async Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Getting {EntityType} entities. Tracked: {Tracked}",
                typeof(T).Name,
                tracked);

            var values = _dbSet.AsQueryable();

            if (expression is not null)
            {
                values = values.Where(expression);
            }

            if (includes is not null)
            {
                foreach (var item in includes)
                {
                    if (item is not null)
                    {
                        values = values.Include(item);
                    }
                }
            }

            if (!tracked)
            {
                values = values.AsNoTracking();
            }

            var result = await values.ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Retrieved {Count} {EntityType} entities.",
                result.Count,
                typeof(T).Name);

            return result;
        }


        // Get One
        public async Task<T?> GetOneAsync(
            Expression<Func<T, bool>>? expression = null,
            Expression<Func<T, object>>?[]? includes = null,
            bool tracked = true,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Getting single {EntityType}. Tracked: {Tracked}",
                typeof(T).Name,
                tracked);

            var values = _dbSet.AsQueryable();

            if (expression is not null)
            {
                values = values.Where(expression);
            }

            if (includes is not null)
            {
                foreach (var item in includes)
                {
                    if (item is not null)
                    {
                        values = values.Include(item);
                    }
                }
            }

            if (!tracked)
            {
                values = values.AsNoTracking();
            }

            var result = await values.FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                _logger.LogDebug(
                    "No {EntityType} entity was found.",
                    typeof(T).Name);
            }
            else
            {
                _logger.LogDebug(
                    "{EntityType} entity was found.",
                    typeof(T).Name);
            }

            return result;
        }


        // Create
        public async Task Create(
            T entity,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Adding new {EntityType} entity.",
                typeof(T).Name);

            await _dbSet.AddAsync(entity, cancellationToken);

            _logger.LogDebug(
                "{EntityType} entity added to DbContext.",
                typeof(T).Name);
        }


        // Update
        public void Update(T entity)
        {
            _logger.LogInformation(
                "Updating {EntityType} entity.",
                typeof(T).Name);

            _dbSet.Update(entity);

            _logger.LogDebug(
                "{EntityType} entity marked as modified.",
                typeof(T).Name);
        }


        // Delete
        public void Delete(T entity)
        {
            _logger.LogInformation(
                "Deleting {EntityType} entity.",
                typeof(T).Name);

            _dbSet.Remove(entity);

            _logger.LogDebug(
                "{EntityType} entity marked for deletion.",
                typeof(T).Name);
        }


        // Commit
        public async Task<int> CommitAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Saving changes to database.");

            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Database changes saved successfully. Affected rows: {AffectedRows}",
                result);

            return result;
        }
    }
}