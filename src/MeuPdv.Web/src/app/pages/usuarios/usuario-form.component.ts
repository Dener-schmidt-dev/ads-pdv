import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { PerfilInfo, PerfilUsuario, UsuarioDto } from '../../core/models/auth.models';
import { NotificationService } from '../../core/notification/notification.service';

@Component({
  selector: 'app-usuario-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './usuario-form.component.html',
  styleUrl: './usuario-form.component.scss'
})
export class UsuarioFormComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  readonly perfis = signal<PerfilInfo[]>([]);
  readonly salvando = signal(false);

  form = {
    login: '',
    nome: '',
    senha: '',
    perfil: 'OperadorCaixa' as PerfilUsuario
  };

  ngOnInit(): void {
    this.http.get<PerfilInfo[]>('/api/perfis').subscribe({
      next: (p) => this.perfis.set(p)
    });
  }

  salvar(): void {
    this.salvando.set(true);

    this.http.post<UsuarioDto>('/api/usuarios', this.form).subscribe({
      next: () => {
        this.salvando.set(false);
        void this.router.navigate(['/usuarios']);
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao criar usuário.');
      }
    });
  }

  cancelar(): void {
    void this.router.navigate(['/usuarios']);
  }

  labelPerfil(perfil: PerfilUsuario): string {
    const map: Record<PerfilUsuario, string> = {
      Administrador: 'Administrador',
      OperadorCaixa: 'Operador de caixa',
      Fiscal: 'Fiscal'
    };
    return map[perfil] ?? perfil;
  }
}
