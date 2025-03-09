using Microsoft.EntityFrameworkCore;
using MovieSeriesManagement.Data;
using MovieSeriesManagement.Data.Repositories;
using MovieSeriesManagement.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieSeriesManagement.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IViewingHistoryRepository _viewingHistoryRepository;
        private readonly IRecommendationRepository _recommendationRepository;
        private readonly IContentRepository _contentRepository;

        public RecommendationService(
            ApplicationDbContext context,
            IViewingHistoryRepository viewingHistoryRepository,
            IRecommendationRepository recommendationRepository,
            IContentRepository contentRepository)
        {
            _context = context;
            _viewingHistoryRepository = viewingHistoryRepository;
            _recommendationRepository = recommendationRepository;
            _contentRepository = contentRepository;
        }

        public async Task<IEnumerable<Content>> GetRecommendationsForUserAsync(string userId, int count = 10)
        {
            // Get existing recommendations or generate new ones if none exist
            var recommendations = await _recommendationRepository.GetUserRecommendationsAsync(userId, count);

            if (!recommendations.Any())
            {
                await GenerateRecommendationsAsync(userId);
                recommendations = await _recommendationRepository.GetUserRecommendationsAsync(userId, count);
            }

            return recommendations.Select(r => r.Content).ToList();
        }

        public async Task GenerateRecommendationsAsync(string userId)
        {
            // Clear existing recommendations
            await _recommendationRepository.DeleteUserRecommendationsAsync(userId);

            // Get user's viewing history
            var userHistory = await _viewingHistoryRepository.GetUserHistoryAsync(userId);

            // Get user's genre preferences
            var genrePreferences = await _viewingHistoryRepository.GetUserGenrePreferencesAsync(userId);

            // Get all content
            var allContent = await _contentRepository.GetAllAsync();

            // Get content IDs the user has already watched
            var watchedContentIds = userHistory.Select(h => h.ContentId).ToHashSet();

            // Filter out content the user has already watched
            var unwatchedContent = allContent.Where(c => !watchedContentIds.Contains(c.Id)).ToList();

            var recommendations = new List<Recommendation>();

            // Generate genre-based recommendations
            foreach (var content in unwatchedContent)
            {
                double score = 0;

                // Genre-based score
                if (genrePreferences.ContainsKey(content.Genre))
                {
                    score += genrePreferences[content.Genre] * 2.0;
                }

                // Only add recommendations with a positive score
                if (score > 0)
                {
                    recommendations.Add(new Recommendation
                    {
                        UserId = userId,
                        ContentId = content.Id,
                        Type = RecommendationType.GenreBased,
                        Score = score,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Add popularity-based recommendations for diversity
            var popularContent = allContent
                .Where(c => !watchedContentIds.Contains(c.Id))
                .OrderByDescending(c => c.ViewingHistories.Count)
                .Take(10);

            foreach (var content in popularContent)
            {
                // Check if we already added this content
                if (!recommendations.Any(r => r.ContentId == content.Id))
                {
                    recommendations.Add(new Recommendation
                    {
                        UserId = userId,
                        ContentId = content.Id,
                        Type = RecommendationType.PopularityBased,
                        Score = 1.0, // Base score for popular content
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Save recommendations
            await _context.Recommendations.AddRangeAsync(recommendations);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Content>> GetSimilarContentsAsync(int contentId, int count = 5)
        {
            var content = await _contentRepository.GetByIdAsync(contentId);
            if (content == null)
            {
                return Enumerable.Empty<Content>();
            }

            // Find content with the same genre
            var similarContent = await _context.Contents
                .Where(c => c.Id != contentId && c.Genre == content.Genre)
                .OrderByDescending(c => c.ViewingHistories.Count) // Order by popularity
                .Take(count)
                .ToListAsync();

            return similarContent;
        }
    }
}

