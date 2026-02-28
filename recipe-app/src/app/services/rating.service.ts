import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rating, CreateRatingRequest } from '../interface/recipes';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RatingService {
  private readonly url = `${environment.apiBase}/ratings`;

  constructor(private http: HttpClient) {}

  getForRecipe(recipeId: number): Observable<Rating[]> {
    return this.http.get<Rating[]>(`${this.url}/${recipeId}`);
  }

  rate(dto: CreateRatingRequest): Observable<Rating> {
    return this.http.post<Rating>(this.url, dto);
  }
}
