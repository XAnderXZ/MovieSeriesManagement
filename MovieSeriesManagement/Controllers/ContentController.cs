using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MovieSeriesManagement.Models.Entities;
using MovieSeriesManagement.Models.ViewModels;
using MovieSeriesManagement.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MovieSeriesManagement.Controllers
{
    public class ContentController : Controller
    {
        private readonly IContentService _contentService;
        private readonly IRecommendationService _recommendationService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContentController(
            IContentService contentService,
            IRecommendationService recommendationService,
            IWebHostEnvironment webHostEnvironment)
        {
            _contentService = contentService;
            _recommendationService = recommendationService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Content
        public async Task<IActionResult> Index(ContentSearchViewModel searchModel)
        {
            // Initialize search model if it's null
            if (searchModel == null)
            {
                searchModel = new ContentSearchViewModel();
            }

            // Get filter options for dropdowns
            searchModel.Genres = await _contentService.GetAllGenresAsync();
            searchModel.Platforms = await _contentService.GetAllPlatformsAsync();

            // Get filtered content
            var contents = await _contentService.SearchContentsAsync(searchModel);

            // Pass both the search model and results to the view
            return View(new Tuple<ContentSearchViewModel, IEnumerable<Content>>(searchModel, contents));
        }

        // GET: Content/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var content = await _contentService.GetContentByIdAsync(id);
            if (content == null)
            {
                return NotFound();
            }

            // Get similar content
            var similarContent = await _recommendationService.GetSimilarContentsAsync(id, 3);
            ViewBag.SimilarContent = similarContent;

            return View(content);
        }

        // GET: Content/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ContentViewModel
            {
                ReleaseDate = DateTime.Now
            };
            return View(viewModel);
        }

        // POST: Content/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ContentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var content = new Content
                {
                    Title = viewModel.Title,
                    Genre = viewModel.Genre,
                    Platform = viewModel.Platform,
                    Type = viewModel.Type,
                    Description = viewModel.Description,
                    ReleaseDate = viewModel.ReleaseDate
                };

                // Handle image upload
                if (viewModel.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "content");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(uploadsFolder);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(fileStream);
                    }

                    content.ImageUrl = "/images/content/" + uniqueFileName;
                }

                await _contentService.AddContentAsync(content);
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Content/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var content = await _contentService.GetContentByIdAsync(id);
            if (content == null)
            {
                return NotFound();
            }

            var viewModel = new ContentViewModel
            {
                Id = content.Id,
                Title = content.Title,
                Genre = content.Genre,
                Platform = content.Platform,
                Type = content.Type,
                Description = content.Description,
                ReleaseDate = content.ReleaseDate,
                ImageUrl = content.ImageUrl
            };

            return View(viewModel);
        }

        // POST: Content/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, ContentViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var content = await _contentService.GetContentByIdAsync(id);
                if (content == null)
                {
                    return NotFound();
                }

                content.Title = viewModel.Title;
                content.Genre = viewModel.Genre;
                content.Platform = viewModel.Platform;
                content.Type = viewModel.Type;
                content.Description = viewModel.Description;
                content.ReleaseDate = viewModel.ReleaseDate;

                // Handle image upload
                if (viewModel.ImageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(content.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, content.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Save new image
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "content");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Ensure directory exists
                    Directory.CreateDirectory(uploadsFolder);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(fileStream);
                    }

                    content.ImageUrl = "/images/content/" + uniqueFileName;
                }

                await _contentService.UpdateContentAsync(content);
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Content/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var content = await _contentService.GetContentByIdAsync(id);
            if (content == null)
            {
                return NotFound();
            }

            return View(content);
        }

        // POST: Content/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _contentService.DeleteContentAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

