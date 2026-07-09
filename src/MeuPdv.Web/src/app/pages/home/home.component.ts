import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { ChartConfiguration } from 'chart.js';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notification/notification.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { ProdutosMaisVendidos, VendasPorPeriodo } from '../../core/models/relatorio.models';
import { SessaoCaixaDto } from '../../core/models/caixa.models';
import { AlertaEstoqueDto } from '../../core/models/produto.models';
import { RelatorioChartComponent } from '../../shared/charts/relatorio-chart.component';
import { ProdutoRankingComponent } from '../../shared/ranking/produto-ranking.component';
import {
  buildPagamentosChart,
  buildVendasLinhaChart
} from '../relatorios/relatorio-chart-builders';

interface HealthResponse {
  status: string;
  offline: boolean;
  mongodb: boolean;
  versao: string;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, RelatorioChartComponent, ProdutoRankingComponent, ...FORMATTING_IMPORTS],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);

  readonly carregando = signal(true);
  readonly health = signal<HealthResponse | null>(null);
  readonly vendas = signal<VendasPorPeriodo | null>(null);
  readonly produtos = signal<ProdutosMaisVendidos | null>(null);
  readonly alertasEstoque = signal<AlertaEstoqueDto[]>([]);
  readonly sessaoCaixa = signal<SessaoCaixaDto | null>(null);

  periodoDias = 30;
  dataInicio = '';
  dataFim = '';

  readonly podeVerRelatorios = computed(() =>
    this.auth.temPermissao('VisualizarRelatorios')
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

  readonly ticketMedio = computed(() => {
    const v = this.vendas();
    if (!v || v.quantidadeVendas === 0) return 0;
    return v.total / v.quantidadeVendas;
  });

  ngOnInit(): void {
    this.aplicarPeriodo(this.periodoDias);
    this.carregar();
  }

  aplicarPeriodo(dias: number): void {
    this.periodoDias = dias;
    const fim = new Date();
    const inicio = new Date(Date.now() - dias * 86400000);
    this.dataInicio = inicio.toISOString().slice(0, 10);
    this.dataFim = fim.toISOString().slice(0, 10);
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);

    this.http.get<HealthResponse>('/api/health').subscribe({
      next: (data) => this.health.set(data),
      error: () => this.notify.error('API indisponível.')
    });

    this.http.get<SessaoCaixaDto>('/api/caixa/atual?caixa=CAIXA-01').subscribe({
      next: (s) => this.sessaoCaixa.set(s),
      error: () => this.sessaoCaixa.set(null)
    });

    this.http.get<AlertaEstoqueDto[]>('/api/estoque/alertas').subscribe({
      next: (a) => this.alertasEstoque.set(a),
      error: () => this.alertasEstoque.set([])
    });

    if (!this.podeVerRelatorios()) {
      this.carregando.set(false);
      return;
    }

    const params = new URLSearchParams({
      inicio: this.dataInicio,
      fim: this.dataFim
    });

    let pendentes = 2;

    const finalizar = () => {
      pendentes -= 1;
      if (pendentes <= 0) this.carregando.set(false);
    };

    this.http.get<VendasPorPeriodo>(`/api/relatorios/vendas?${params}`).subscribe({
      next: (r) => { this.vendas.set(r); finalizar(); },
      error: () => { this.vendas.set(null); finalizar(); }
    });

    this.http.get<ProdutosMaisVendidos>(`/api/relatorios/produtos-mais-vendidos?${params}&limite=5`).subscribe({
      next: (r) => { this.produtos.set(r); finalizar(); },
      error: () => { this.produtos.set(null); finalizar(); }
    });
  }
}
