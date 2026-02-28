using Microsoft.Data.SqlClient;
using RecipeSugesstionApp.DTOs;
using System.Data;
using System.Text;

namespace RecipeSugesstionApp.Repositories
{
    public interface IRecipeRepository
    {
        /// <summary>
        /// Full-text search with optional keyword, ingredient, and category filters.
        /// Returns a paginated result with category names populated.
        /// </summary>
        Task<PagedResult<RecipeSummaryDto>> SearchAsync(RecipeSearchQuery query);
    }

    /// <summary>
    /// ADO.NET Repository — performs high-performance parameterised SQL searches
    /// directly against the database, bypassing EF Core for the hot feed path.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        private readonly string _connectionString;

        public RecipeRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not found.");
        }

        public async Task<PagedResult<RecipeSummaryDto>> SearchAsync(RecipeSearchQuery query)
        {
            var recipes    = new List<RecipeSummaryDto>();
            int totalCount = 0;

            // ── Build WHERE clause ──────────────────────────────────────────────
            var where = new StringBuilder(" WHERE 1=1");
            if (!string.IsNullOrWhiteSpace(query.Q))
                where.Append(@" AND (
                    r.Title       LIKE @q
                 OR r.Description LIKE @q
                 OR EXISTS (SELECT 1 FROM Ingredients  i  WHERE i.RecipeId  = r.RecipeId AND i.Name  LIKE @q)
                 OR EXISTS (SELECT 1 FROM RecipeCategories rc2
                            JOIN   Categories c2 ON rc2.CategoryId = c2.CategoryId
                            WHERE  rc2.RecipeId = r.RecipeId AND c2.Name LIKE @q))");

            if (!string.IsNullOrWhiteSpace(query.Ingredient))
                where.Append(" AND EXISTS (SELECT 1 FROM Ingredients i2 WHERE i2.RecipeId = r.RecipeId AND i2.Name LIKE @ingredient)");

            if (query.CategoryId.HasValue)
                where.Append(" AND EXISTS (SELECT 1 FROM RecipeCategories rc3 WHERE rc3.RecipeId = r.RecipeId AND rc3.CategoryId = @catId)");

            // ── ORDER BY ────────────────────────────────────────────────────────
            var orderBy = query.Sort?.ToLowerInvariant() switch
            {
                "oldest"     => "r.CreatedAt ASC",
                "top-rated"  => "AvgRating DESC, r.CreatedAt DESC",
                "most-rated" => "RatingCount DESC, r.CreatedAt DESC",
                _            => "r.CreatedAt DESC"   // "newest" (default)
            };

            // ── Count SQL ───────────────────────────────────────────────────────
            var countSql = $"SELECT COUNT(DISTINCT r.RecipeId) FROM Recipes r{where}";

            // ── Paginated data SQL ──────────────────────────────────────────────
            var offset = (query.Page - 1) * query.PageSize;
            var dataSql = $@"
                SELECT  r.RecipeId,
                        r.Title,
                        r.ImageUrl,
                        r.CreatedAt,
                        u.Username                                                        AS AuthorUsername,
                        ISNULL((SELECT AVG(CAST(Score AS FLOAT))
                                FROM   Ratings rt WHERE rt.RecipeId = r.RecipeId), 0)    AS AvgRating,
                        (SELECT COUNT(*) FROM Ratings rt2 WHERE rt2.RecipeId = r.RecipeId) AS RatingCount
                FROM    Recipes r
                LEFT JOIN Users u ON r.UserId = u.UserId
                {where}
                ORDER BY {orderBy}
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            // ── Categories SQL (batch-load for the result page) ─────────────────
            //   We load categories AFTER the paginated IDs are known, to avoid
            //   inflating the row count with a JOIN in the main query.
            const string categoriesSql = @"
                SELECT rc.RecipeId, c.Name
                FROM   RecipeCategories rc
                JOIN   Categories c ON rc.CategoryId = c.CategoryId
                WHERE  rc.RecipeId IN (SELECT value FROM STRING_SPLIT(@ids, ','))";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Count
            await using (var cmd = new SqlCommand(countSql, conn))
            {
                AddSearchParameters(cmd, query);
                var result = await cmd.ExecuteScalarAsync();
                totalCount = result is DBNull || result is null ? 0 : Convert.ToInt32(result);
            }

            if (totalCount == 0)
                return new PagedResult<RecipeSummaryDto>
                {
                    Items = recipes,
                    TotalCount = 0,
                    Page = query.Page,
                    PageSize = query.PageSize
                };

            // 2. Main paginated rows
            await using (var cmd = new SqlCommand(dataSql, conn))
            {
                AddSearchParameters(cmd, query);
                cmd.Parameters.AddWithValue("@offset",   offset);
                cmd.Parameters.AddWithValue("@pageSize", query.PageSize);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    recipes.Add(new RecipeSummaryDto
                    {
                        RecipeId       = reader.GetInt32(0),
                        Title          = reader.GetString(1),
                        ImageUrl       = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CreatedAt      = reader.GetDateTime(3),
                        AuthorUsername = reader.IsDBNull(4) ? "Unknown" : reader.GetString(4),
                        AverageRating  = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                        RatingCount    = reader.GetInt32(6),
                        Categories     = new List<string>()
                    });
                }
            }

            // 3. Batch-load categories for the returned recipe IDs
            if (recipes.Count > 0)
            {
                var idsCsv = string.Join(',', recipes.Select(r => r.RecipeId));
                var catLookup = new Dictionary<int, List<string>>();

                await using var cmd = new SqlCommand(categoriesSql, conn);
                cmd.Parameters.AddWithValue("@ids", idsCsv);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int  recipeId = reader.GetInt32(0);
                    string catName = reader.GetString(1);
                    if (!catLookup.TryGetValue(recipeId, out var list))
                        catLookup[recipeId] = list = new List<string>();
                    list.Add(catName);
                }

                foreach (var recipe in recipes)
                    if (catLookup.TryGetValue(recipe.RecipeId, out var cats))
                        recipe.Categories = cats;
            }

            return new PagedResult<RecipeSummaryDto>
            {
                Items      = recipes,
                TotalCount = totalCount,
                Page       = query.Page,
                PageSize   = query.PageSize
            };
        }

        // ── Helper ──────────────────────────────────────────────────────────────
        private static void AddSearchParameters(SqlCommand cmd, RecipeSearchQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Q))
                cmd.Parameters.AddWithValue("@q", $"%{query.Q}%");

            if (!string.IsNullOrWhiteSpace(query.Ingredient))
                cmd.Parameters.AddWithValue("@ingredient", $"%{query.Ingredient}%");

            if (query.CategoryId.HasValue)
                cmd.Parameters.AddWithValue("@catId", query.CategoryId.Value);
        }
    }
}
