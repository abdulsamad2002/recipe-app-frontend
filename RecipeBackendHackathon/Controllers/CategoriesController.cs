using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public CategoriesController(RecipeDbContext db) => _db = db;

        /// <summary>Get all categories (ordered alphabetically).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var cats = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    Name       = c.Name
                })
                .ToListAsync();

            return Ok(cats);
        }

        /// <summary>
        /// Get a single category by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryDto), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var cat = await _db.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDto { CategoryId = c.CategoryId, Name = c.Name })
                .FirstOrDefaultAsync();

            if (cat is null)
                return NotFound(new ErrorResponse { Message = $"Category {id} not found." });

            return Ok(cat);
        }

        /// <summary>Create a new category. Auth required.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CategoryDto), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(BuildValidationError());

            var name = dto.Name.Trim();
            if (string.IsNullOrEmpty(name))
                return BadRequest(new ErrorResponse { Message = "Category name cannot be empty." });

            var exists = await _db.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (exists)
                return Conflict(new ErrorResponse { Message = $"A category named '{name}' already exists." });

            var cat = new Models.Category { Name = name };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();

            var result = new CategoryDto { CategoryId = cat.CategoryId, Name = cat.Name };
            return CreatedAtAction(nameof(GetById), new { id = cat.CategoryId }, result);
        }

        // ── Helpers ─────────────────────────────────────────────────────────
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
