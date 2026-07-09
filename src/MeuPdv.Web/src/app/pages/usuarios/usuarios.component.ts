import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { PerfilUsuario, UsuarioDto } from '../../core/models/auth.models';
import { NotificationService } from '../../core/notification/notification.service';
import { paginar, TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [RouterLink, PaginationComponent],
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.scss'
})
export class UsuariosComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  readonly usuarios = signal<UsuarioDto[]>([]);
  readonly pagina = signal(1);
  readonly tamanhoPagina = TAMANHO_PAGINA;

  readonly usuariosPagina = computed(() =>
    paginar(this.usuarios(), this.pagina(), this.tamanhoPagina)
  );

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.http.get<UsuarioDto[]>('/api/usuarios').subscribe({
      next: (u) => this.usuarios.set(u),
      error: () => this.notify.error('Não foi possível carregar usuários.')
    });
  }

  irParaPagina(pagina: number): void {
    this.pagina.set(pagina);
  }

  inativar(usuario: UsuarioDto): void {
    if (!confirm(`Inativar o usuário ${usuario.nome}?`)) return;

    this.http.post(`/api/usuarios/${usuario.id}/inativar`, {}).subscribe({
      next: () => this.carregar(),
      error: (e) => this.notify.apiError(e, 'Erro ao inativar.')
    });
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
