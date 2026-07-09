import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notification/notification.service';
import {
  AlertaEstoqueDto,
  MovimentacaoEstoqueDto,
  SaldoProdutoDto
} from '../../core/models/produto.models';
import { PaginaLista } from '../../core/models/pagina-lista.models';
import { paginar, TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

type TipoMov = 'entrada' | 'saida' | 'ajuste';

@Component({
  selector: 'app-estoque',
  standalone: true,
  imports: [FormsModule, DatePipe, PaginationComponent],
  templateUrl: './estoque.component.html',
  styleUrl: './estoque.component.scss'
})
export class EstoqueComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);

  readonly saldos = signal<SaldoProdutoDto[]>([]);
  readonly alertas = signal<AlertaEstoqueDto[]>([]);
  readonly historico = signal<MovimentacaoEstoqueDto[]>([]);
  readonly totalHistorico = signal(0);
  readonly paginaSaldos = signal(1);
  readonly paginaHistorico = signal(1);
  readonly tamanhoPagina = TAMANHO_PAGINA;

  readonly saldosPagina = computed(() =>
    paginar(this.saldos(), this.paginaSaldos(), this.tamanhoPagina)
  );
  readonly salvando = signal(false);
  readonly mostrarModal = signal(false);

  busca = '';
  somenteAbaixoMinimo = false;
  tipoMov: TipoMov = 'entrada';

  movForm = {
    produtoId: '',
    quantidade: 1,
    saldoFinal: 0,
    motivo: ''
  };

  ngOnInit(): void {
    this.carregar();
    this.carregarAlertas();
    this.carregarHistorico();
  }

  buscar(): void {
    this.paginaSaldos.set(1);
    this.carregar();
  }

  irParaPaginaSaldos(pagina: number): void {
    this.paginaSaldos.set(pagina);
  }

  irParaPaginaHistorico(pagina: number): void {
    this.paginaHistorico.set(pagina);
    this.carregarHistorico();
  }

  carregar(): void {
    const params = new URLSearchParams();
    if (this.busca) params.set('termo', this.busca);
    if (this.somenteAbaixoMinimo) params.set('abaixoMinimo', 'true');

    this.http.get<SaldoProdutoDto[]>(`/api/estoque/saldos?${params}`).subscribe({
      next: (s) => {
        this.saldos.set(s);
        this.paginaSaldos.set(1);
      },
      error: () => this.notify.error('Erro ao carregar saldos.')
    });
  }

  carregarAlertas(): void {
    this.http.get<AlertaEstoqueDto[]>('/api/estoque/alertas').subscribe({
      next: (a) => this.alertas.set(a)
    });
  }

  carregarHistorico(): void {
    const params = new URLSearchParams({
      pagina: String(this.paginaHistorico()),
      tamanho: String(this.tamanhoPagina)
    });

    this.http.get<PaginaLista<MovimentacaoEstoqueDto>>(`/api/estoque/historico?${params}`).subscribe({
      next: (r) => {
        this.historico.set(r.itens);
        this.totalHistorico.set(r.total);
      }
    });
  }

  abrirMovimento(s: SaldoProdutoDto, tipo: TipoMov): void {
    this.tipoMov = tipo;
    this.movForm = {
      produtoId: s.produtoId,
      quantidade: 1,
      saldoFinal: s.estoqueAtual,
      motivo: ''
    };
    this.mostrarModal.set(true);
  }

  confirmarMovimento(): void {
    this.salvando.set(true);

    let req$;
    if (this.tipoMov === 'entrada') {
      req$ = this.http.post('/api/estoque/entrada', {
        produtoId: this.movForm.produtoId,
        quantidade: this.movForm.quantidade,
        motivo: this.movForm.motivo
      });
    } else if (this.tipoMov === 'saida') {
      req$ = this.http.post('/api/estoque/saida', {
        produtoId: this.movForm.produtoId,
        quantidade: this.movForm.quantidade,
        motivo: this.movForm.motivo
      });
    } else {
      req$ = this.http.post('/api/estoque/ajuste', {
        produtoId: this.movForm.produtoId,
        saldoFinal: this.movForm.saldoFinal,
        motivo: this.movForm.motivo
      });
    }

    req$.subscribe({
      next: () => {
        this.salvando.set(false);
        this.mostrarModal.set(false);
        this.carregar();
        this.carregarAlertas();
        this.carregarHistorico();
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro na movimentação.');
      }
    });
  }

  labelOrigem(origem: string): string {
    const map: Record<string, string> = {
      EntradaManual: 'Entrada',
      SaidaManual: 'Saída',
      Ajuste: 'Ajuste',
      Venda: 'Venda',
      Cancelamento: 'Cancelamento'
    };
    return map[origem] ?? origem;
  }

  iconeOrigem(origem: string): string {
    const map: Record<string, string> = {
      EntradaManual: 'add_circle',
      SaidaManual: 'remove_circle',
      Ajuste: 'tune',
      Venda: 'shopping_cart',
      Cancelamento: 'undo'
    };
    return map[origem] ?? 'inventory_2';
  }

  classeOrigem(origem: string): string {
    const map: Record<string, string> = {
      EntradaManual: 'estoque__origem--entrada',
      SaidaManual: 'estoque__origem--saida',
      Ajuste: 'estoque__origem--ajuste',
      Venda: 'estoque__origem--venda',
      Cancelamento: 'estoque__origem--cancelamento'
    };
    return `estoque__origem ${map[origem] ?? 'estoque__origem--neutral'}`;
  }

  classeQuantidade(m: MovimentacaoEstoqueDto): string {
    if (m.origem === 'EntradaManual' || m.origem === 'Cancelamento') {
      return 'estoque__qtd estoque__qtd--pos';
    }
    if (m.origem === 'SaidaManual' || m.origem === 'Venda') {
      return 'estoque__qtd estoque__qtd--neg';
    }
    return 'estoque__qtd estoque__qtd--neutro';
  }

  formatarQuantidade(m: MovimentacaoEstoqueDto): string {
    const qtd = Math.abs(m.quantidade);
    if (m.origem === 'EntradaManual' || m.origem === 'Cancelamento') {
      return `+${qtd}`;
    }
    if (m.origem === 'SaidaManual' || m.origem === 'Venda') {
      return `-${qtd}`;
    }
    return String(qtd);
  }

  tituloModal(): string {
    return this.tipoMov === 'entrada' ? 'Entrada manual'
      : this.tipoMov === 'saida' ? 'Saída manual' : 'Ajuste de estoque';
  }
}
