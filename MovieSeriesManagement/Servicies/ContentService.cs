using MovieSeriesManagement.Data.Repositories;
using MovieSeriesManagement.Models.Entities;
using MovieSeriesManagement.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieSeriesManagement.Services
{
    public class ContentService : IContentService
    {
        private readonly IContentRepository _contentRepository;

        public ContentService(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }

        public async Task<IEnumerable<Content>> GetAllContentsAsync()
        {
            return await _contentRepository.GetAllAsync();
        }

        public async Task<Content> GetContentByIdAsync(int id)
        {
            return await _contentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Content>> SearchContentsAsync(ContentSearchViewModel searchModel)
        {
            return await _contentRepository.SearchContentAsync(
                searchModel.SearchTerm,
                searchModel.Genre,
                searchModel.Platform,
                searchModel.ContentType
            );
        }

        public async Task<IEnumerable<string>> GetAllGenresAsync()
        {
            return await _contentRepository.GetAllGenresAsync();
        }

        public async Task<IEnumerable<string>> GetAllPlatformsAsync()
        {
            return await _contentRepository.GetAllPlatformsAsync();
        }

        public async Task AddContentAsync(Content content)
        {
            await _contentRepository.AddAsync(content);
            await _contentRepository.SaveChangesAsync();
        }

        public async Task UpdateContentAsync(Content content)
        {
            await _contentRepository.UpdateAsync(content);
            await _contentRepository.SaveChangesAsync();
        }

        public async Task DeleteContentAsync(int id)
        {
            var content = await _contentRepository.GetByIdAsync(id);
            if (content != null)
            {
                await _contentRepository.DeleteAsync(content);
                await _contentRepository.SaveChangesAsync();
            }
        }
    }
}

