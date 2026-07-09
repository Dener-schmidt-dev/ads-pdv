import { Component, inject, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit {
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly menuRecolhido = signal(false);

  ngOnInit(): void {
    if (this.auth.token) {
      this.auth.refreshMe().subscribe({ error: () => this.auth.logout() });
    }

    this.atualizarMenu(this.router.url);
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe((e) => {
      this.atualizarMenu((e as NavigationEnd).urlAfterRedirects);
    });
  }

  private atualizarMenu(url: string): void {
    this.menuRecolhido.set(url.startsWith('/venda'));
  }

  toggleMenu(): void {
    this.menuRecolhido.update((v) => !v);
  }

  sair(): void {
    this.auth.logout();
  }
}
