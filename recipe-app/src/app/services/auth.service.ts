import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../interface/recipes';
import { environment } from '../../environments/environment';

const TOKEN_KEY = 'recipe_jwt';
const USER_KEY  = 'recipe_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authUrl = `${environment.apiBase}/auth`;

  // Reactive auth state
  private _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private _user  = signal<{ userId: number; username: string } | null>(
    this._loadUser()
  );

  readonly token    = this._token.asReadonly();
  readonly user     = this._user.asReadonly();
  readonly isLoggedIn = computed(() => !!this._token());

  constructor(private http: HttpClient) {}

  register(dto: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.authUrl}/register`, dto).pipe(
      tap(res => this._persist(res))
    );
  }

  login(dto: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.authUrl}/login`, dto).pipe(
      tap(res => this._persist(res))
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._user.set(null);
  }

  getToken(): string | null {
    return this._token();
  }

  private _persist(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    const u = { userId: res.userId, username: res.username };
    localStorage.setItem(USER_KEY, JSON.stringify(u));
    this._token.set(res.token);
    this._user.set(u);
  }

  private _loadUser(): { userId: number; username: string } | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }
}
