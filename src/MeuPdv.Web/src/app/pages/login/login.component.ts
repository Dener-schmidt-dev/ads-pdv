import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notification/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  login = this.auth.lembrarLogin();
  senha = '';
  lembrar = !!this.login;
  readonly carregando = signal(false);

  entrar(): void {
    this.carregando.set(true);

    this.auth.login(this.login, this.senha, this.lembrar).subscribe({
      next: () => {
        this.carregando.set(false);
        this.router.navigate(['/inicio']);
      },
      error: () => {
        this.carregando.set(false);
        this.notify.error('Usuário ou senha inválidos.');
        this.senha = '';
      }
    });
  }
}
