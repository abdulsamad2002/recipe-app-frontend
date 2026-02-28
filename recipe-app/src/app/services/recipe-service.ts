import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Recipe,
  RecipeSummary,
  PaginatedRecipes,
  CreateRecipeRequest,
  UpdateRecipeRequest
} from '../interface/recipes';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RecipeService {
  private readonly url = `${environment.apiBase}/recipes`;

  constructor(private http: HttpClient) {}

  /** Public feed with pagination */
  getRecipes(page = 1, pageSize = 10): Observable<PaginatedRecipes> {
    return this.http.get<PaginatedRecipes>(this.url, {
      params: { page, pageSize }
    });
  }

  /** Search by keyword / category */
  search(q: string, categoryId?: number, page = 1, pageSize = 10): Observable<PaginatedRecipes> {
    let params: Record<string, string | number> = { page, pageSize };
    if (q)          params['q'] = q;
    if (categoryId) params['categoryId'] = categoryId;
    return this.http.get<PaginatedRecipes>(`${this.url}/search`, { params });
  }

  /** Recipes belonging to the logged-in user (Auth required) */
  getMyRecipes(): Observable<RecipeSummary[]> {
    return this.http.get<RecipeSummary[]>(`${this.url}/my`);
  }

  /** Full detail of a single recipe */
  getById(id: number): Observable<Recipe> {
    return this.http.get<Recipe>(`${this.url}/${id}`);
  }

  /** Create a new recipe (Auth required) */
  create(dto: CreateRecipeRequest): Observable<Recipe> {
    return this.http.post<Recipe>(this.url, dto);
  }

  /** Update a recipe (Auth required, owner only) */
  update(id: number, dto: UpdateRecipeRequest): Observable<Recipe> {
    return this.http.put<Recipe>(`${this.url}/${id}`, dto);
  }

  /** Delete a recipe (Auth required, owner only) */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  /** Upload / replace a recipe image (Auth required, owner only) */
  uploadImage(id: number, file: File): Observable<{ imageUrl: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(`${this.url}/${id}/image`, form);
  }
}
