using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Repositories;
using RecipeSugesstionApp.Services;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/recipes")]
    [Produces("application/json")]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeService    _recipes;
        private readonly IRecipeRepository _repo;
        private readonly IWebHostEnvironment _env;

        public RecipesController(
            IRecipeService recipes,
            IRecipeRepository repo,
            IWebHostEnvironment env)
        {
            _recipes = recipes;
            _repo    = repo;
            _env     = env;
        }

        // ── GET /api/recipes ─────────────────────────────────────────────────
        /// <summary>
        /// Paginated recipe feed. Pass no query params for the default newest-first list.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<RecipeSummaryDto>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sort = "newest")
        {
            var query = new RecipeSearchQuery
            {
                Page     = page,
                PageSize = pageSize,
                Sort     = sort
            };
            var result = await _repo.SearchAsync(query);
            return Ok(new
            {
                recipes  = result.Items,
                total    = result.TotalCount,
                page     = result.Page,
                pageSize = result.PageSize,
                totalPages    = result.TotalPages,
                hasPreviousPage = result.HasPreviousPage,
                hasNextPage     = result.HasNextPage
            });
        }

        // ── GET /api/recipes/search ──────────────────────────────────────────
        /// <summary>
        /// Search recipes by keyword (title / description / ingredient / category name),
        /// a specific ingredient, and/or a category ID. Supports four sort modes:
        /// newest (default) | oldest | top-rated | most-rated.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<RecipeSummaryDto>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> Search([FromQuery] RecipeSearchQuery query)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _repo.SearchAsync(query);
            return Ok(new
            {
                recipes  = result.Items,
                total    = result.TotalCount,
                page     = result.Page,
                pageSize = result.PageSize,
                totalPages      = result.TotalPages,
                hasPreviousPage = result.HasPreviousPage,
                hasNextPage     = result.HasNextPage
            });
        }

        // ── GET /api/recipes/my ──────────────────────────────────────────────
        /// <summary>Get all recipes created by the authenticated user.</summary>
        [HttpGet("my")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<RecipeSummaryDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyRecipes()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();
            var recipes = await _recipes.GetByUserIdAsync(userId);
            return Ok(recipes);
        }

        // ── GET /api/recipes/{id} ─────────────────────────────────────────────
        /// <summary>Get the full detail of a single recipe.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RecipeDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new ErrorResponse { Message = "Recipe ID must be a positive integer." });

            var recipe = await _recipes.GetByIdAsync(id);
            if (recipe is null)
                return NotFound(new ErrorResponse { Message = $"Recipe {id} not found." });

            return Ok(recipe);
        }

        // ── POST /api/recipes ─────────────────────────────────────────────────
        /// <summary>Create a new recipe. Requires authentication.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RecipeDto), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var created = await _recipes.CreateAsync(userId, dto);
                return CreatedAtAction(nameof(GetById), new { id = created.RecipeId }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ── PUT /api/recipes/{id} ─────────────────────────────────────────────
        /// <summary>Update a recipe. Only the recipe owner can make changes.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(RecipeDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRecipeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            try
            {
                var updated = await _recipes.UpdateAsync(id, userId, dto);
                if (updated is null)
                    return NotFound(new ErrorResponse { Message = "Recipe not found, or you are not the owner." });

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
        }

        // ── DELETE /api/recipes/{id} ──────────────────────────────────────────
        /// <summary>Delete a recipe. Only the recipe owner can delete.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var deleted = await _recipes.DeleteAsync(id, userId);
            if (!deleted)
                return NotFound(new ErrorResponse { Message = "Recipe not found, or you are not the owner." });

            return NoContent();
        }

        // ── POST /api/recipes/{id}/image ──────────────────────────────────────
        /// <summary>Upload or replace the photo for a recipe (owner only).</summary>
        [HttpPost("{id:int}/image")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UploadImage(int id, IFormFile? file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ErrorResponse { Message = "No file provided." });

            const long maxBytes = 5 * 1024 * 1024; // 5 MB
            if (file.Length > maxBytes)
                return BadRequest(new ErrorResponse { Message = "File size must not exceed 5 MB." });

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var imageUrl = await _recipes.UploadImageAsync(id, userId, file, _env);
            if (imageUrl is null)
                return NotFound(new ErrorResponse
                {
                    Message = "Recipe not found, you are not the owner, or the file type is not supported."
                });

            return Ok(new { imageUrl });
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
