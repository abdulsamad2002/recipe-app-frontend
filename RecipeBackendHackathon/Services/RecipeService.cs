using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Models;

namespace RecipeSugesstionApp.Services
{
    public interface IRecipeService
    {
        Task<RecipeDto?> GetByIdAsync(int id);
        Task<IEnumerable<RecipeSummaryDto>> GetByUserIdAsync(int userId);
        Task<RecipeDto> CreateAsync(int userId, CreateRecipeDto dto);
        Task<RecipeDto?> UpdateAsync(int recipeId, int userId, UpdateRecipeDto dto);
        Task<bool> DeleteAsync(int recipeId, int userId);
        Task<string?> UploadImageAsync(int recipeId, int userId, IFormFile file, IWebHostEnvironment env);
    }

    public class RecipeService : IRecipeService
    {
        private readonly RecipeDbContext _db;

        public RecipeService(RecipeDbContext db) => _db = db;

        // ── GET BY USER ───────────────────────────────────────────────────────
        public async Task<IEnumerable<RecipeSummaryDto>> GetByUserIdAsync(int userId)
        {
            // Uses a single EF Core projection to avoid loading full entity graphs.
            return await _db.Recipes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RecipeSummaryDto
                {
                    RecipeId       = r.RecipeId,
                    Title          = r.Title,
                    ImageUrl       = r.ImageUrl,
                    AuthorUsername = r.User != null ? r.User.Username : "Unknown",
                    Categories     = r.RecipeCategories
                                      .Select(rc => rc.Category != null ? rc.Category.Name : "")
                                      .ToList(),
                    AverageRating  = r.Ratings.Any()
                                      ? r.Ratings.Average(rt => (double)rt.Score)
                                      : 0,
                    RatingCount    = r.Ratings.Count(),
                    CreatedAt      = r.CreatedAt
                })
                .ToListAsync();
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<RecipeDto?> GetByIdAsync(int id)
        {
            // Single projection query — no N+1
            var r = await _db.Recipes
                .Where(r => r.RecipeId == id)
                .Select(r => new RecipeDto
                {
                    RecipeId       = r.RecipeId,
                    UserId         = r.UserId,
                    AuthorUsername = r.User != null ? r.User.Username : "Unknown",
                    Title          = r.Title,
                    Description    = r.Description,
                    Steps          = r.Steps
                                      .OrderBy(s => s.Order)
                                      .Select(s => s.Instruction)
                                      .ToList(),
                    ImageUrl       = r.ImageUrl,
                    CreatedAt      = r.CreatedAt,
                    UpdatedAt      = r.UpdatedAt,
                    Ingredients    = r.Ingredients
                                      .Select(i => new IngredientDto
                                      {
                                          Name     = i.Name,
                                          Quantity = i.Quantity
                                      })
                                      .ToList(),
                    Categories     = r.RecipeCategories
                                      .Select(rc => rc.Category != null ? rc.Category.Name : "")
                                      .ToList(),
                    AverageRating  = r.Ratings.Any()
                                      ? r.Ratings.Average(rt => (double)rt.Score)
                                      : 0,
                    RatingCount    = r.Ratings.Count()
                })
                .FirstOrDefaultAsync();

            return r;
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<RecipeDto> CreateAsync(int userId, CreateRecipeDto dto)
        {
            // Validate category IDs exist
            var validCatIds = dto.CategoryIds.Distinct().ToList();
            if (validCatIds.Count > 0)
            {
                var existingCount = await _db.Categories
                    .CountAsync(c => validCatIds.Contains(c.CategoryId));
                if (existingCount != validCatIds.Count)
                    throw new ArgumentException("One or more category IDs are invalid.");
            }

            var recipe = new Recipe
            {
                UserId      = userId,
                Title       = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };

            // Steps (ordered)
            recipe.Steps = dto.Steps
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select((s, index) => new Step
                {
                    Instruction = s.Trim(),
                    Order       = index + 1
                })
                .ToList();

            if (recipe.Steps.Count == 0)
                throw new ArgumentException("At least one non-empty step is required.");

            // Ingredients
            recipe.Ingredients = dto.Ingredients
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .Select(i => new Ingredient
                {
                    Name     = i.Name.Trim(),
                    Quantity = i.Quantity?.Trim() ?? string.Empty
                })
                .ToList();

            // Categories
            recipe.RecipeCategories = validCatIds
                .Select(catId => new RecipeCategory { CategoryId = catId })
                .ToList();

            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(recipe.RecipeId))!;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<RecipeDto?> UpdateAsync(int recipeId, int userId, UpdateRecipeDto dto)
        {
            var recipe = await _db.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .Include(r => r.RecipeCategories)
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (recipe == null) return null;

            if (dto.Title is not null)       recipe.Title       = dto.Title.Trim();
            if (dto.Description is not null) recipe.Description = dto.Description.Trim();
            recipe.UpdatedAt = DateTime.UtcNow;

            if (dto.Steps is not null)
            {
                var filtered = dto.Steps.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (filtered.Count == 0)
                    throw new ArgumentException("At least one non-empty step is required.");

                _db.Steps.RemoveRange(recipe.Steps);
                recipe.Steps = filtered.Select((s, i) => new Step
                {
                    RecipeId    = recipeId,
                    Instruction = s.Trim(),
                    Order       = i + 1
                }).ToList();
            }

            if (dto.Ingredients is not null)
            {
                _db.Ingredients.RemoveRange(recipe.Ingredients);
                recipe.Ingredients = dto.Ingredients
                    .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                    .Select(i => new Ingredient
                    {
                        RecipeId = recipeId,
                        Name     = i.Name.Trim(),
                        Quantity = i.Quantity?.Trim() ?? string.Empty
                    }).ToList();
            }

            if (dto.CategoryIds is not null)
            {
                var validCatIds = dto.CategoryIds.Distinct().ToList();
                if (validCatIds.Count > 0)
                {
                    var existingCount = await _db.Categories
                        .CountAsync(c => validCatIds.Contains(c.CategoryId));
                    if (existingCount != validCatIds.Count)
                        throw new ArgumentException("One or more category IDs are invalid.");
                }

                _db.RecipeCategories.RemoveRange(recipe.RecipeCategories);
                recipe.RecipeCategories = validCatIds
                    .Select(catId => new RecipeCategory { RecipeId = recipeId, CategoryId = catId })
                    .ToList();
            }

            await _db.SaveChangesAsync();
            return await GetByIdAsync(recipeId);
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int recipeId, int userId)
        {
            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (recipe == null) return false;

            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── IMAGE UPLOAD ──────────────────────────────────────────────────────
        public async Task<string?> UploadImageAsync(
            int recipeId, int userId, IFormFile file, IWebHostEnvironment env)
        {
            var recipe = await _db.Recipes
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (recipe == null) return null;

            var uploadsFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "recipes");
            Directory.CreateDirectory(uploadsFolder);

            var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext)) return null;

            // Delete old image if replacing
            if (recipe.ImageUrl is not null)
            {
                var old = Path.Combine(env.WebRootPath ?? "wwwroot", recipe.ImageUrl.TrimStart('/'));
                if (File.Exists(old)) File.Delete(old);
            }

            var fileName = $"{recipeId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            recipe.ImageUrl  = $"/uploads/recipes/{fileName}";
            recipe.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return recipe.ImageUrl;
        }
    }
}
