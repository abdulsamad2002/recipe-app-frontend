import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Comment, CreateCommentRequest } from '../interface/recipes';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CommentService {
  private readonly url = `${environment.apiBase}/comments`;

  constructor(private http: HttpClient) {}

  getForRecipe(recipeId: number): Observable<Comment[]> {
    return this.http.get<Comment[]>(`${this.url}/${recipeId}`);
  }

  create(dto: CreateCommentRequest): Observable<Comment> {
    return this.http.post<Comment>(this.url, dto);
  }

  delete(commentId: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${commentId}`);
  }
}
