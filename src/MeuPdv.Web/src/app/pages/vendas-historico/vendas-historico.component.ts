import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { NotificationService } from '../../core/notification/notification.service';
import { VendaDetalhe, VendaListaItem, VendaListaResponse } from '../../core/models/venda.models';
import { TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-vendas-historico',
  standalone: true,
  imports: [FormsModule, DatePipe, PaginationComponent, ...FORMATTING_IMPORTS],
  templateUrl: './vendas-historico.component.html',
  styleUrl: './vendas-historico.component.scss'
})
export class VendasHistoricoComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);

  readonly vendas = signal<VendaListaItem[]>([]);
  readonly totalVendas = signal(0);
  pagina = 1;
  readonly tamanhoPagina = TAMANHO_PAGINA;
  readonly detalhe = signal<VendaDetalhe | null>(null);
  readonly processando = signal(false);
  readonly mostrarCancelar = signal(false);

  busca = '';
  motivoCancelamento = '';
  vendaSelecionada = signal<VendaListaItem | null>(null);

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
    params.set('pagina', String(this.pagina));
    params.set('tamanho', String(this.tamanhoPagina));

    this.http.get<VendaListaResponse>(`/api/vendas/recentes?${params}`).subscribe({
      next: (r) => {
        this.vendas.set(r.itens);
        this.totalVendas.set(r.total);
      },
      error: () => this.notify.error('Erro ao carregar vendas.')
    });
  }

  verDetalhe(v: VendaListaItem): void {
    this.http.get<VendaDetalhe>(`/api/vendas/detalhe/${v.id}`).subscribe({
      next: (d) => {
        this.detalhe.set(d);
        this.vendaSelecionada.set(v);
      }
    });
  }

  abrirCancelar(v: VendaListaItem): void {
    this.vendaSelecionada.set(v);
    this.motivoCancelamento = '';
    this.mostrarCancelar.set(true);
  }

  confirmarCancelamento(): void {
    const v = this.vendaSelecionada();
    if (!v) return;

    this.processando.set(true);
    this.http.post(`/api/vendas/cancelar/${v.id}`, { motivo: this.motivoCancelamento }).subscribe({
      next: () => {
        this.processando.set(false);
        this.mostrarCancelar.set(false);
        this.detalhe.set(null);
        this.carregar();
      },
      error: (e) => {
        this.processando.set(false);
        this.notify.apiError(e, 'Erro ao cancelar venda.');
      }
    });
  }
}
