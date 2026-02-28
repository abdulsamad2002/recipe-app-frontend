// ── Shared / Auth ────────────────────────────────────────────────────────────

export interface AuthResponse {
  token: string;
  username: string;
  userId: number;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

// ── Category ─────────────────────────────────────────────────────────────────

export interface Category {
  categoryId: number;
  name: string;
}

// ── Ingredient ────────────────────────────────────────────────────────────────

export interface Ingredient {
  name: string;
  quantity: string;
}

// ── Recipe ────────────────────────────────────────────────────────────────────

/** Returned by GET /api/recipes and GET /api/recipes/search (paginated list) */
export interface RecipeSummary {
  recipeId: number;
  title: string;
  imageUrl: string | null;
  authorUsername: string;
  categories: string[];
  averageRating: number;
  ratingCount: number;
  createdAt: string;
}

/** Returned by GET /api/recipes/{id} and POST /api/recipes */
export interface Recipe {
  recipeId: number;
  userId: number;
  authorUsername: string;
  title: string;
  description: string;
  steps: string[];
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
  ingredients: Ingredient[];
  categories: string[];
  averageRating: number;
  ratingCount: number;
}

/** Paginated response for the feed / search */
export interface PaginatedRecipes {
  recipes: RecipeSummary[];
  total: number;
  page: number;
  pageSize: number;
}

/** Payload to create a recipe */
export interface CreateRecipeRequest {
  title: string;
  description: string;
  steps: string[];
  ingredients: Ingredient[];
  categoryIds: number[];
}

/** Payload to update a recipe (all fields optional) */
export interface UpdateRecipeRequest {
  title?: string;
  description?: string;
  steps?: string[];
  ingredients?: Ingredient[];
  categoryIds?: number[];
}

// ── Comment ───────────────────────────────────────────────────────────────────

export interface Comment {
  commentId: number;
  recipeId: number;
  userId: number;
  username: string;
  body: string;
  createdAt: string;
}

export interface CreateCommentRequest {
  recipeId: number;
  body: string;
}

// ── Rating ────────────────────────────────────────────────────────────────────

export interface Rating {
  ratingId: number;
  recipeId: number;
  userId: number;
  username: string;
  score: number;
  createdAt: string;
}

export interface CreateRatingRequest {
  recipeId: number;
  score: number;
}

// Legacy alias kept for backward compatibility (recipe-card.ts uses it)
export type Recipes = Recipe;
