import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { paginar, TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { DateInputComponent } from '../../shared/date-input/date-input.component';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { ChartConfiguration } from 'chart.js';
import { formatBrl } from '../../core/format/number-format';
import { NotificationService } from '../../core/notification/notification.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { RelatorioChartComponent } from '../../shared/charts/relatorio-chart.component';
import { ProdutoRankingComponent } from '../../shared/ranking/produto-ranking.component';
import {
  EstoqueBaixoItem,
  ProdutosMaisVendidos,
  VendasPorPeriodo
} from '../../core/models/relatorio.models';
import {
  buildEstoqueChart,
  buildPagamentosChart,
  buildVendasLinhaChart
} from './relatorio-chart-builders';

type Aba = 'vendas' | 'produtos' | 'estoque';

@Component({
  selector: 'app-relatorios',
  standalone: true,
  imports: [FormsModule, DatePipe, RelatorioChartComponent, ProdutoRankingComponent, PaginationComponent, DateInputComponent, ...FORMATTING_IMPORTS],
  templateUrl: './relatorios.component.html',
  styleUrl: './relatorios.component.scss'
})
export class RelatoriosComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  readonly aba = signal<Aba>('vendas');
  readonly carregando = signal(false);

  readonly vendas = signal<VendasPorPeriodo | null>(null);
  readonly produtos = signal<ProdutosMaisVendidos | null>(null);
  readonly estoque = signal<EstoqueBaixoItem[]>([]);

  dataInicio = this.formatarDataInput(new Date(Date.now() - 30 * 86400000));
  dataFim = this.formatarDataInput(new Date());

  readonly paginaVendas = signal(1);
  readonly paginaProdutos = signal(1);
  readonly paginaEstoque = signal(1);
  readonly tamanhoPagina = TAMANHO_PAGINA;

  readonly vendasTabela = computed(() => {
    const v = this.vendas();
    if (!v) return { itens: [], total: 0 };
    return paginar(v.vendas, this.paginaVendas(), this.tamanhoPagina);
  });

  readonly produtosTabela = computed(() => {
    const p = this.produtos();
    if (!p) return { itens: [], total: 0 };
    return paginar(p.itens, this.paginaProdutos(), this.tamanhoPagina);
  });

  readonly estoqueTabela = computed(() =>
    paginar(this.estoque(), this.paginaEstoque(), this.tamanhoPagina)
  );

  readonly chartVendasLinha = computed((): ChartConfiguration | null => {
    const v = this.vendas();
    if (!v) return null;
    return buildVendasLinhaChart(v.vendas, this.dataInicio, this.dataFim);
  });

  readonly chartPagamentos = computed((): ChartConfiguration | null => {
    const v = this.vendas();
    if (!v || v.porFormaPagamento.length === 0) return null;
    return buildPagamentosChart(v.porFormaPagamento);
  });

  readonly chartEstoque = computed((): ChartConfiguration | null => {
    const itens = this.estoque();
    if (itens.length === 0) return null;
    return buildEstoqueChart(itens);
  });

  readonly ticketMedio = computed(() => {
    const v = this.vendas();
    if (!v || v.quantidadeVendas === 0) return 0;
    return v.total / v.quantidadeVendas;
  });

  ngOnInit(): void {
    this.carregar();
  }

  trocarAba(aba: Aba): void {
    this.aba.set(aba);
    this.paginaVendas.set(1);
    this.paginaProdutos.set(1);
    this.paginaEstoque.set(1);
    this.carregar();
  }

  aplicarPeriodo(dias: number): void {
    const fim = new Date();
    const inicio = new Date(Date.now() - dias * 86400000);
    this.dataInicio = this.formatarDataInput(inicio);
    this.dataFim = this.formatarDataInput(fim);
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);
    this.paginaVendas.set(1);
    this.paginaProdutos.set(1);
    this.paginaEstoque.set(1);

    const params = this.periodoParams();
    const abaAtual = this.aba();

    if (abaAtual === 'vendas') {
      this.http.get<VendasPorPeriodo>(`/api/relatorios/vendas?${params}`).subscribe({
        next: (r) => { this.vendas.set(r); this.carregando.set(false); },
        error: (e) => this.falha(e)
      });
      return;
    }

    if (abaAtual === 'produtos') {
      this.http.get<ProdutosMaisVendidos>(`/api/relatorios/produtos-mais-vendidos?${params}&limite=20`).subscribe({
        next: (r) => { this.produtos.set(r); this.carregando.set(false); },
        error: (e) => this.falha(e)
      });
      return;
    }

    this.http.get<EstoqueBaixoItem[]>('/api/relatorios/estoque-baixo').subscribe({
      next: (r) => { this.estoque.set(r); this.carregando.set(false); },
      error: (e) => this.falha(e)
    });
  }

  exportarCsv(): void {
    const abaAtual = this.aba();
    let url = '';

    if (abaAtual === 'vendas') {
      url = `/api/relatorios/vendas/exportar-csv?${this.periodoParams()}`;
    } else if (abaAtual === 'produtos') {
      url = `/api/relatorios/produtos-mais-vendidos/exportar-csv?${this.periodoParams()}&limite=100`;
    } else {
      url = '/api/relatorios/estoque-baixo/exportar-csv';
    }

    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `relatorio-${abaAtual}-${this.dataFim}.csv`;
        a.click();
        URL.revokeObjectURL(a.href);
      },
      error: (e) => this.falha(e)
    });
  }

  exportarPdf(): void {
    const html = this.montarHtmlImpressao();
    const w = window.open('', '_blank', 'width=800,height=600');
    if (!w) return;
    w.document.write(html);
    w.document.close();
    w.print();
    w.close();
  }

  totalProdutos(p: ProdutosMaisVendidos): number {
    return p.itens.reduce((acc, item) => acc + item.valorTotal, 0);
  }

  labelForma(forma: string): string {
    const map: Record<string, string> = {
      Dinheiro: 'Dinheiro',
      CartaoDebito: 'Cartão débito',
      CartaoCredito: 'Cartão crédito',
      Pix: 'Pix',
      Outros: 'Outros'
    };
    return map[forma] ?? forma;
  }

  private periodoParams(): string {
    return new URLSearchParams({
      inicio: this.dataInicio,
      fim: this.dataFim
    }).toString();
  }

  private falha(e: unknown): void {
    this.carregando.set(false);
    this.notify.apiError(e, 'Erro ao carregar relatório.');
  }

  private formatarDataInput(d: Date): string {
    return d.toISOString().slice(0, 10);
  }

  private montarHtmlImpressao(): string {
    const abaAtual = this.aba();
    const titulo = abaAtual === 'vendas'
      ? 'Vendas por período'
      : abaAtual === 'produtos'
        ? 'Produtos mais vendidos'
        : 'Estoque abaixo do mínimo';

    let corpo = '';

    const vendas = this.vendas();
    const produtos = this.produtos();

    if (abaAtual === 'vendas' && vendas) {
      corpo = `
        <p>Período: ${this.dataInicio} a ${this.dataFim}</p>
        <p>Vendas: ${vendas.quantidadeVendas} | Total: ${formatBrl(vendas.total)}</p>
        <table border="1" cellpadding="4" cellspacing="0" width="100%">
          <tr><th>Nº</th><th>Data</th><th>Total</th><th>Pagamentos</th></tr>
          ${vendas.vendas.map(x => `<tr><td>${x.numero}</td><td>${new Date(x.data).toLocaleString('pt-BR')}</td><td>${formatBrl(x.total)}</td><td>${x.formasPagamento}</td></tr>`).join('')}
        </table>`;
    } else if (abaAtual === 'produtos' && produtos) {
      corpo = `
        <p>Período: ${this.dataInicio} a ${this.dataFim}</p>
        <table border="1" cellpadding="4" cellspacing="0" width="100%">
          <tr><th>Código</th><th>Descrição</th><th>Qtd</th><th>Valor</th></tr>
          ${produtos.itens.map(x => `<tr><td>${x.codigoInterno}</td><td>${x.descricao}</td><td>${x.quantidade}</td><td>${formatBrl(x.valorTotal)}</td></tr>`).join('')}
        </table>`;
    } else if (abaAtual === 'estoque') {
      corpo = `
        <table border="1" cellpadding="4" cellspacing="0" width="100%">
          <tr><th>Código</th><th>Descrição</th><th>Atual</th><th>Mínimo</th></tr>
          ${this.estoque().map(x => `<tr><td>${x.codigoInterno}</td><td>${x.descricao}</td><td>${x.estoqueAtual}</td><td>${x.estoqueMinimo}</td></tr>`).join('')}
        </table>`;
    }

    return `<!DOCTYPE html><html><head><meta charset="utf-8"><title>${titulo}</title>
      <style>body{font-family:Arial,sans-serif;font-size:12px}h1{font-size:16px}</style></head>
      <body><h1>MeuPDV — ${titulo}</h1>${corpo}</body></html>`;
  }
}
