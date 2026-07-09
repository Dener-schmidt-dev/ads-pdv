import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { LoginResponse, Permissao, UsuarioLogado } from '../models/auth.models';

const TOKEN_KEY = 'meupdv_token';
const USER_KEY = 'meupdv_user';
const LOGIN_KEY = 'meupdv_login';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly usuario = signal<UsuarioLogado | null>(this.carregarUsuario());

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.token && !!this.usuario();
  }

  login(login: string, senha: string, lembrarLogin: boolean) {
    return this.http.post<LoginResponse>('/api/auth/login', { login, senha }).pipe(
      tap((res) => {
        localStorage.setItem(TOKEN_KEY, res.token);
        localStorage.setItem(USER_KEY, JSON.stringify(res.usuario));
        if (lembrarLogin) {
          localStorage.setItem(LOGIN_KEY, login);
        } else {
          localStorage.removeItem(LOGIN_KEY);
        }
        this.usuario.set(res.usuario);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.usuario.set(null);
    this.router.navigate(['/login']);
  }

  lembrarLogin(): string {
    return localStorage.getItem(LOGIN_KEY) ?? '';
  }

  temPermissao(permissao: Permissao): boolean {
    return this.usuario()?.permissoes.includes(permissao) ?? false;
  }

  refreshMe() {
    return this.http.get<UsuarioLogado>('/api/auth/me').pipe(
      tap((u) => {
        localStorage.setItem(USER_KEY, JSON.stringify(u));
        this.usuario.set(u);
      })
    );
  }

  private carregarUsuario(): UsuarioLogado | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UsuarioLogado;
    } catch {
      return null;
    }
  }
}
