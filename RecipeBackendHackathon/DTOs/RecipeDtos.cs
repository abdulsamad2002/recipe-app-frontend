using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.DTOs
{
    // ── Auth ─────────────────────────────────────────────────────────────────

    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$",
            ErrorMessage = "Username may only contain letters, numbers, underscores, hyphens, and dots.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // ── User ─────────────────────────────────────────────────────────────────

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ── Recipe ───────────────────────────────────────────────────────────────

    public class CreateRecipeDto
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one step is required.")]
        [MinLength(1, ErrorMessage = "At least one step is required.")]
        public List<string> Steps { get; set; } = new();

        public List<IngredientDto> Ingredients { get; set; } = new();

        public List<int> CategoryIds { get; set; } = new();
    }

    public class UpdateRecipeDto
    {
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string? Title { get; set; }

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        public List<string>? Steps { get; set; }
        public List<IngredientDto>? Ingredients { get; set; }
        public List<int>? CategoryIds { get; set; }
    }

    public class RecipeDto
    {
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<IngredientDto> Ingredients { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }

    public class RecipeSummaryDto
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Ingredient ───────────────────────────────────────────────────────────

    public class IngredientDto
    {
        [Required(ErrorMessage = "Ingredient name is required.")]
        [MaxLength(200, ErrorMessage = "Ingredient name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Quantity cannot exceed 100 characters.")]
        public string Quantity { get; set; } = string.Empty;
    }

    // ── Category ─────────────────────────────────────────────────────────────

    public class CategoryDto
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    // ── Rating ───────────────────────────────────────────────────────────────

    public class CreateRatingDto
    {
        [Required(ErrorMessage = "RecipeId is required.")]
        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Score is required.")]
        [Range(1, 5, ErrorMessage = "Score must be between 1 and 5.")]
        public int Score { get; set; }
    }

    public class RatingDto
    {
        public int RatingId { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Returned when asking for the current user's own rating on a recipe.</summary>
    public class MyRatingDto
    {
        public int? RatingId { get; set; }
        public int? Score { get; set; }
        public bool HasRated { get; set; }
    }

    // ── Comment ──────────────────────────────────────────────────────────────

    public class CreateCommentDto
    {
        [Required(ErrorMessage = "RecipeId is required.")]
        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Comment body is required.")]
        [MinLength(1, ErrorMessage = "Comment cannot be empty.")]
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters.")]
        public string Body { get; set; } = string.Empty;
    }

    public class UpdateCommentDto
    {
        [Required(ErrorMessage = "Comment body is required.")]
        [MinLength(1, ErrorMessage = "Comment cannot be empty.")]
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters.")]
        public string Body { get; set; } = string.Empty;
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    /// <summary>Generic paginated result wrapper.</summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    /// <summary>Search / filter parameters for the recipe feed.</summary>
    public class RecipeSearchQuery
    {
        /// <summary>Free-text search across title, description, ingredients, and categories.</summary>
        public string? Q { get; set; }

        /// <summary>Filter by category ID.</summary>
        public int? CategoryId { get; set; }

        /// <summary>Filter by ingredient name (partial match).</summary>
        public string? Ingredient { get; set; }

        /// <summary>Sort order: newest | oldest | top-rated | most-rated</summary>
        public string? Sort { get; set; } = "newest";

        private int _page = 1;
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value is < 1 or > 50 ? 10 : value;
        }
    }

    // ── Error Response ────────────────────────────────────────────────────────

    /// <summary>Standardised error envelope returned by the API.</summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public IDictionary<string, string[]>? Errors { get; set; }
        public string? Detail { get; set; }
    }
}
