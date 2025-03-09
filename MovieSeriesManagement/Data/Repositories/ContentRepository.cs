using Microsoft.EntityFrameworkCore;
using MovieSeriesManagement.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieSeriesManagement.Data.Repositories
{
    public class ContentRepository : Repository<Content>, IContentRepository
    {
        public ContentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Content>> SearchContentAsync(string searchTerm, string genre, string platform, ContentType? type)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Title.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(c => c.Genre == genre);
            }

            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(c => c.Platform == platform);
            }

            if (type.HasValue)
            {
                query = query.Where(c => c.Type == type.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllGenresAsync()
        {
            return await _dbSet.Select(c => c.Genre).Distinct().ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllPlatformsAsync()
        {
            return await _dbSet.Select(c => c.Platform).Distinct().ToListAsync();
        }
    }

    public interface IContentRepository : IRepository<Content>
    {
        Task<IEnumerable<Content>> SearchContentAsync(string searchTerm, string genre, string platform, ContentType? type);
        Task<IEnumerable<string>> GetAllGenresAsync();
        Task<IEnumerable<string>> GetAllPlatformsAsync();
    }
}

