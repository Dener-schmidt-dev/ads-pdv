import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { NotificationService } from '../../core/notification/notification.service';
import { ResumoFechamentoDto, SessaoCaixaDto } from '../../core/models/caixa.models';
import { PaginaLista } from '../../core/models/pagina-lista.models';
import { TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-caixa',
  standalone: true,
  imports: [FormsModule, DatePipe, PaginationComponent, ...FORMATTING_IMPORTS],
  templateUrl: './caixa.component.html',
  styleUrl: './caixa.component.scss'
})
export class CaixaComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);

  readonly sessao = signal<SessaoCaixaDto | null>(null);
  readonly historico = signal<SessaoCaixaDto[]>([]);
  readonly totalHistorico = signal(0);
  paginaHistorico = 1;
  readonly tamanhoPagina = TAMANHO_PAGINA;
  readonly resumoFechamento = signal<ResumoFechamentoDto | null>(null);
  readonly salvando = signal(false);

  caixaId = 'CAIXA-01';
  valorInicial = 0;
  valorMovimento = 0;
  motivoMovimento = '';
  valorContado = 0;
  observacaoFechamento = '';

  ngOnInit(): void {
    this.carregar();
    this.carregarHistorico();
  }

  carregar(): void {
    this.http.get<SessaoCaixaDto>(`/api/caixa/atual?caixa=${this.caixaId}`).subscribe({
      next: (s) => this.sessao.set(s),
      error: () => this.sessao.set(null)
    });
  }

  irParaPaginaHistorico(pagina: number): void {
    this.paginaHistorico = pagina;
    this.carregarHistorico();
  }

  carregarHistorico(): void {
    const params = new URLSearchParams({
      pagina: String(this.paginaHistorico),
      tamanho: String(this.tamanhoPagina)
    });

    this.http.get<PaginaLista<SessaoCaixaDto>>(`/api/caixa/historico?${params}`).subscribe({
      next: (r) => {
        this.historico.set(r.itens);
        this.totalHistorico.set(r.total);
      }
    });
  }

  abrir(): void {
    this.salvando.set(true);

    this.http.post<SessaoCaixaDto>('/api/caixa/abrir', {
      caixaIdentificador: this.caixaId,
      valorInicial: this.valorInicial
    }).subscribe({
      next: (s) => {
        this.sessao.set(s);
        this.salvando.set(false);
        this.carregarHistorico();
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao abrir caixa.');
      }
    });
  }

  suprimento(): void {
    this.movimentar('/api/caixa/suprimento');
  }

  sangria(): void {
    this.movimentar('/api/caixa/sangria');
  }

  fechar(): void {
    const s = this.sessao();
    if (!s) return;

    this.salvando.set(true);

    this.http.post<ResumoFechamentoDto>('/api/caixa/fechar', {
      sessaoCaixaId: s.id,
      valorContado: this.valorContado,
      observacao: this.observacaoFechamento || null
    }).subscribe({
      next: (r) => {
        this.resumoFechamento.set(r);
        this.sessao.set(null);
        this.salvando.set(false);
        this.carregarHistorico();
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao fechar caixa.');
      }
    });
  }

  private movimentar(url: string): void {
    const s = this.sessao();
    if (!s) return;

    this.salvando.set(true);

    this.http.post(url, {
      sessaoCaixaId: s.id,
      valor: this.valorMovimento,
      motivo: this.motivoMovimento
    }).subscribe({
      next: () => {
        this.valorMovimento = 0;
        this.motivoMovimento = '';
        this.salvando.set(false);
        this.carregar();
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro na movimentação.');
      }
    });
  }

  labelStatus(status: string): string {
    return status === 'Aberta' ? 'Aberto' : 'Fechado';
  }

  iconeStatus(status: string): string {
    return status === 'Aberta' ? 'lock_open' : 'lock';
  }

  classeStatus(status: string): string {
    return status === 'Aberta'
      ? 'caixa__badge caixa__badge--open'
      : 'caixa__badge caixa__badge--closed';
  }
}
