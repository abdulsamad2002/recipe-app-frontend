using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Models;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/ratings")]
    [Produces("application/json")]
    public class RatingsController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public RatingsController(RecipeDbContext db) => _db = db;

        // ── GET /api/ratings/{recipeId} ──────────────────────────────────────
        /// <summary>
        /// Get all ratings for a recipe with average and distribution summary.
        /// </summary>
        [HttpGet("{recipeId:int}")]
        [ProducesResponseType(typeof(RatingSummaryResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetForRecipe(int recipeId)
        {
            if (recipeId <= 0)
                return BadRequest(new ErrorResponse { Message = "recipeId must be a positive integer." });

            var recipeExists = await _db.Recipes.AnyAsync(r => r.RecipeId == recipeId);
            if (!recipeExists)
                return NotFound(new ErrorResponse { Message = $"Recipe {recipeId} not found." });

            var ratings = await _db.Ratings
                .Where(r => r.RecipeId == recipeId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RatingDto
                {
                    RatingId  = r.RatingId,
                    RecipeId  = r.RecipeId,
                    UserId    = r.UserId,
                    Username  = r.User != null ? r.User.Username : "Unknown",
                    Score     = r.Score,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            // Build score distribution (1–5)
            var distribution = Enumerable.Range(1, 5).ToDictionary(
                s => s,
                s => ratings.Count(r => r.Score == s));

            return Ok(new RatingSummaryResponse
            {
                RecipeId     = recipeId,
                Ratings      = ratings,
                AverageScore = ratings.Count > 0
                    ? Math.Round(ratings.Average(r => r.Score), 2)
                    : 0,
                TotalRatings  = ratings.Count,
                Distribution  = distribution
            });
        }

        // ── GET /api/ratings/my/{recipeId} ───────────────────────────────────
        /// <summary>
        /// Get the authenticated user's own rating on a specific recipe.
        /// Returns HasRated: false when they haven't rated yet.
        /// </summary>
        [HttpGet("my/{recipeId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(MyRatingDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyRating(int recipeId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var recipeExists = await _db.Recipes.AnyAsync(r => r.RecipeId == recipeId);
            if (!recipeExists)
                return NotFound(new ErrorResponse { Message = $"Recipe {recipeId} not found." });

            var rating = await _db.Ratings
                .Where(r => r.RecipeId == recipeId && r.UserId == userId)
                .Select(r => new { r.RatingId, r.Score })
                .FirstOrDefaultAsync();

            return Ok(new MyRatingDto
            {
                HasRated = rating is not null,
                RatingId = rating?.RatingId,
                Score    = rating?.Score
            });
        }

        // ── POST /api/ratings ────────────────────────────────────────────────
        /// <summary>
        /// Rate a recipe (score 1–5). If the user has already rated,
        /// the existing rating is updated (upsert behaviour).
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RatingDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Rate([FromBody] CreateRatingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var recipeExists = await _db.Recipes.AnyAsync(r => r.RecipeId == dto.RecipeId);
            if (!recipeExists)
                return NotFound(new ErrorResponse { Message = $"Recipe {dto.RecipeId} not found." });

            // Upsert: update existing or insert new
            var existing = await _db.Ratings
                .FirstOrDefaultAsync(r => r.RecipeId == dto.RecipeId && r.UserId == userId);

            bool isNew = existing is null;
            if (isNew)
            {
                existing = new Rating
                {
                    RecipeId  = dto.RecipeId,
                    UserId    = userId,
                    Score     = dto.Score,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Ratings.Add(existing);
            }
            else
            {
                existing!.Score     = dto.Score;
                existing!.CreatedAt = DateTime.UtcNow; // treat as "last rated at"
            }

            await _db.SaveChangesAsync();

            var username = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Unknown";

            var result = new RatingDto
            {
                RatingId  = existing.RatingId,
                RecipeId  = existing.RecipeId,
                UserId    = existing.UserId,
                Username  = username,
                Score     = existing.Score,
                CreatedAt = existing.CreatedAt
            };

            // 201 for new ratings, 200 for updates
            return isNew
                ? CreatedAtAction(nameof(GetForRecipe), new { recipeId = existing.RecipeId }, result)
                : Ok(result);
        }

        // ── DELETE /api/ratings/{recipeId} ───────────────────────────────────
        /// <summary>Remove the authenticated user's rating from a recipe.</summary>
        [HttpDelete("{recipeId:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Delete(int recipeId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var rating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (rating is null)
                return NotFound(new ErrorResponse
                {
                    Message = "You have not rated this recipe, or the recipe does not exist."
                });

            _db.Ratings.Remove(rating);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        private int GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out var id) ? id : 0;
        }

        private ErrorResponse BuildValidationError() => new()
        {
            Message = "One or more validation errors occurred.",
            Errors  = ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray())
        };
    }

    // ── Response shapes for this controller ────────────────────────────────────

    /// <summary>Rich response for the GET /api/ratings/{recipeId} endpoint.</summary>
    public class RatingSummaryResponse
    {
        public int RecipeId          { get; init; }
        public IEnumerable<RatingDto> Ratings { get; init; } = Enumerable.Empty<RatingDto>();
        public double AverageScore   { get; init; }
        public int    TotalRatings   { get; init; }
        /// <summary>Score distribution: { 1: count, 2: count, … 5: count }</summary>
        public Dictionary<int, int> Distribution { get; init; } = new();
    }
}
