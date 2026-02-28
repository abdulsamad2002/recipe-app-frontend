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
    [Route("api/comments")]
    [Produces("application/json")]
    public class CommentsController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public CommentsController(RecipeDbContext db) => _db = db;

        // ── GET /api/comments/{recipeId} ─────────────────────────────────────
        /// <summary>
        /// Get paginated comments for a recipe, newest-first.
        /// </summary>
        [HttpGet("{recipeId:int}")]
        [ProducesResponseType(typeof(PagedResult<CommentDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetForRecipe(
            int recipeId,
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 20)
        {
            if (recipeId <= 0)
                return BadRequest(new ErrorResponse { Message = "recipeId must be a positive integer." });

            // Clamp page size
            if (page < 1)     page     = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var recipeExists = await _db.Recipes.AnyAsync(r => r.RecipeId == recipeId);
            if (!recipeExists)
                return NotFound(new ErrorResponse { Message = $"Recipe {recipeId} not found." });

            var totalCount = await _db.Comments
                .CountAsync(c => c.RecipeId == recipeId);

            var comments = await _db.Comments
                .Where(c => c.RecipeId == recipeId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDto
                {
                    CommentId  = c.CommentId,
                    RecipeId   = c.RecipeId,
                    UserId     = c.UserId,
                    Username   = c.User != null ? c.User.Username : "Unknown",
                    Body       = c.Body,
                    CreatedAt  = c.CreatedAt,
                    UpdatedAt  = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(new PagedResult<CommentDto>
            {
                Items      = comments,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            });
        }

        // ── POST /api/comments ───────────────────────────────────────────────
        /// <summary>Post a comment on a recipe. Auth required.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CommentDto), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] CreateCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var recipeExists = await _db.Recipes.AnyAsync(r => r.RecipeId == dto.RecipeId);
            if (!recipeExists)
                return NotFound(new ErrorResponse { Message = $"Recipe {dto.RecipeId} not found." });

            var comment = new Comment
            {
                RecipeId  = dto.RecipeId,
                UserId    = userId,
                Body      = dto.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            // Reload username via projection
            var username = await _db.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Unknown";

            var result = new CommentDto
            {
                CommentId = comment.CommentId,
                RecipeId  = comment.RecipeId,
                UserId    = comment.UserId,
                Username  = username,
                Body      = comment.Body,
                CreatedAt = comment.CreatedAt
            };

            return CreatedAtAction(nameof(GetForRecipe), new { recipeId = comment.RecipeId }, result);
        }

        // ── PUT /api/comments/{commentId} ────────────────────────────────────
        /// <summary>
        /// Edit the body of a comment. Only the original author can edit.
        /// </summary>
        [HttpPut("{commentId:int}")]
        [Authorize]
        [ProducesResponseType(typeof(CommentDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(int commentId, [FromBody] UpdateCommentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var comment = await _db.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment is null)
                return NotFound(new ErrorResponse { Message = "Comment not found." });

            if (comment.UserId != userId)
                return Forbid();

            comment.Body      = dto.Body.Trim();
            comment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new CommentDto
            {
                CommentId = comment.CommentId,
                RecipeId  = comment.RecipeId,
                UserId    = comment.UserId,
                Username  = comment.User?.Username ?? "Unknown",
                Body      = comment.Body,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            });
        }

        // ── DELETE /api/comments/{commentId} ─────────────────────────────────
        /// <summary>Delete a comment. Only the author can delete their own comment.</summary>
        [HttpDelete("{commentId:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Delete(int commentId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var comment = await _db.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment is null)
                return NotFound(new ErrorResponse { Message = "Comment not found." });

            if (comment.UserId != userId)
                return Forbid(); // 403 — comment exists but requester is not the author

            _db.Comments.Remove(comment);
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
}
