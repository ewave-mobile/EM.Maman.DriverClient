using EM.Maman.Models.Interfaces.Repositories;
using EM.Maman.Models.LocalDbModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query; // Needed for IIncludableQueryable

namespace EM.Maman.DAL.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly LocalMamanDBContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public Repository(LocalMamanDBContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity> GetByIdAsync(long id)
        {
            return await DbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        // Implementation for GetAllAsync overload with includes/ordering
        public async Task<IEnumerable<TEntity>> GetAllAsync(
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false)
        {
            IQueryable<TEntity> query = DbSet;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        // Original FindAsync
        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.Where(predicate).ToListAsync();
        }

        // Implementation for FindAsync overload with includes/ordering
        public async Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false)
        {
            IQueryable<TEntity> query = DbSet;

            if (disableTracking)
            {
                query = query.AsNoTracking();
            }

            if (include != null)
            {
                query = include(query);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }


        public async System.Threading.Tasks.Task AddAsync(TEntity entity)
        {
            await DbSet.AddAsync(entity);
        }

        public async System.Threading.Tasks.Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await DbSet.AddRangeAsync(entities);
        }

        public void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            DbSet.Remove(entity);
        }

        // In your Repository<TEntity> class, modify RemoveRange to explicitly attach and mark as deleted
        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            // Get logger instance (assuming ILogger<Repository<TEntity>> is injected or available)
            // Note: Direct logger injection into a generic base class can be tricky.
            // For now, we'll use a placeholder comment.
            // _logger.LogInformation($"Attempting to remove range of {typeof(TEntity).Name} entities.");

            foreach (var entity in entities)
            {
                // First check the state of the entity
                var entry = Context.Entry(entity);
                // _logger.LogInformation($"Entity state before processing: {entry.State} for entity type {typeof(TEntity).Name}");

                if (entry.State == EntityState.Detached)
                {
                    // If detached, attach it first
                    DbSet.Attach(entity);
                    // _logger.LogInformation($"Entity state after Attach: {entry.State} for entity type {typeof(TEntity).Name}");
                }

                // Now mark it for deletion
                DbSet.Remove(entity);
                // _logger.LogInformation($"Entity state after Remove: {entry.State} for entity type {typeof(TEntity).Name}");
            }

            // Alternative approach (only use one of these methods at a time):
            // foreach (var entity in entities)
            // {
            //     Context.Entry(entity).State = EntityState.Deleted;
            // }
            // _logger.LogInformation($"Finished processing RemoveRange for {typeof(TEntity).Name} entities.");
        }

        public async Task<bool> AnyAsync()
        {
            //implementation for anyasync
            return await DbSet.AnyAsync();
        }
    }
}
