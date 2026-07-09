import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { ClienteDto, ClienteListaResponse } from '../../core/models/cliente.models';
import { NotificationService } from '../../core/notification/notification.service';
import { TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [FormsModule, RouterLink, PaginationComponent],
  templateUrl: './clientes.component.html',
  styleUrl: './clientes.component.scss'
})
export class ClientesComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  readonly clientes = signal<ClienteDto[]>([]);
  readonly total = signal(0);

  busca = '';
  somenteAtivos = true;
  pagina = 1;
  readonly tamanhoPagina = TAMANHO_PAGINA;

  ngOnInit(): void {
    this.carregar();
  }

  buscar(): void {
    this.pagina = 1;
    this.carregar();
  }

  irParaPagina(pagina: number): void {
    this.pagina = pagina;
    this.carregar();
  }

  carregar(): void {
    const params = new URLSearchParams();
    if (this.busca) params.set('termo', this.busca);
    if (this.somenteAtivos) params.set('ativo', 'true');
    params.set('pagina', String(this.pagina));
    params.set('tamanho', String(this.tamanhoPagina));

    this.http.get<ClienteListaResponse>(`/api/clientes?${params}`).subscribe({
      next: (r) => {
        this.clientes.set(r.itens);
        this.total.set(r.total);
      },
      error: () => this.notify.error('Erro ao carregar clientes.')
    });
  }

  inativar(cliente: ClienteDto): void {
    if (!cliente.ativo) return;
    if (!confirm(`Inativar o cliente ${cliente.nome}?`)) return;

    this.http.post(`/api/clientes/${cliente.id}/inativar`, {}).subscribe({
      next: () => this.carregar(),
      error: (e) => this.notify.apiError(e, 'Erro ao inativar.')
    });
  }
}
