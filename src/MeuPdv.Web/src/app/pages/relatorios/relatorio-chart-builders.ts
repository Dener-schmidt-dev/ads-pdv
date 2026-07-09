import { ChartConfiguration } from 'chart.js';
import { formatBrl } from '../../core/format/number-format';
import {
  EstoqueBaixoItem,
  ProdutoMaisVendido,
  VendaResumo
} from '../../core/models/relatorio.models';

const PALETTE = ['#2563eb', '#16a34a', '#d97706', '#7c3aed', '#0891b2', '#db2777', '#dc2626'];

const FORMA_LABELS: Record<string, string> = {
  Dinheiro: 'Dinheiro',
  CartaoDebito: 'Cartão débito',
  CartaoCredito: 'Cartão crédito',
  Pix: 'Pix',
  Outros: 'Outros'
};

function basePlugins() {
  return {
    legend: {
      position: 'bottom' as const,
      labels: {
        usePointStyle: true,
        padding: 14,
        font: { family: "'Plus Jakarta Sans', 'Segoe UI', sans-serif", size: 12 }
      }
    },
    tooltip: {
      backgroundColor: '#18181b',
      titleFont: { family: "'Plus Jakarta Sans', 'Segoe UI', sans-serif", size: 13 },
      bodyFont: { family: "'Plus Jakarta Sans', 'Segoe UI', sans-serif", size: 12 },
      padding: 10,
      cornerRadius: 8
    }
  };
}

function baseLayout() {
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index' as const, intersect: false }
  };
}

function formatarDia(iso: string): string {
  const [, mes, dia] = iso.split('-');
  return `${dia}/${mes}`;
}

function diasNoPeriodo(inicio: string, fim: string): string[] {
  const dias: string[] = [];
  const atual = new Date(`${inicio}T12:00:00`);
  const limite = new Date(`${fim}T12:00:00`);

  while (atual <= limite) {
    dias.push(atual.toISOString().slice(0, 10));
    atual.setDate(atual.getDate() + 1);
  }

  return dias;
}

function agruparVendasPorDia(vendas: VendaResumo[], inicio: string, fim: string): number[] {
  const totais = new Map<string, number>();
  for (const dia of diasNoPeriodo(inicio, fim)) {
    totais.set(dia, 0);
  }

  for (const venda of vendas) {
    const dia = venda.data.slice(0, 10);
    if (!totais.has(dia)) continue;
    totais.set(dia, (totais.get(dia) ?? 0) + venda.total);
  }

  return [...totais.values()];
}

export function buildVendasLinhaChart(
  vendas: VendaResumo[],
  inicio: string,
  fim: string
): ChartConfiguration<'line'> {
  const labels = diasNoPeriodo(inicio, fim).map(formatarDia);
  const valores = agruparVendasPorDia(vendas, inicio, fim);

  return {
    type: 'line',
    data: {
      labels,
      datasets: [{
        label: 'Faturamento',
        data: valores,
        borderColor: '#2563eb',
        backgroundColor: 'rgba(37, 99, 235, 0.12)',
        fill: true,
        tension: 0.35,
        pointRadius: 4,
        pointHoverRadius: 6,
        pointBackgroundColor: '#2563eb',
        pointBorderColor: '#fff',
        pointBorderWidth: 2
      }]
    },
    options: {
      ...baseLayout(),
      scales: {
        x: {
          grid: { display: false },
          ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 12 }
        },
        y: {
          beginAtZero: true,
          grid: { color: 'rgba(228, 228, 231, 0.8)' },
          ticks: {
            callback: (v) => formatBrl(Number(v))
          }
        }
      },
      plugins: {
        ...basePlugins(),
        tooltip: {
          ...basePlugins().tooltip,
          callbacks: {
            label: (ctx) => ` ${formatBrl(Number(ctx.parsed.y))}`
          }
        }
      }
    }
  };
}

export function buildPagamentosChart(
  formas: { forma: string; valor: number }[]
): ChartConfiguration<'doughnut'> {
  return {
    type: 'doughnut',
    data: {
      labels: formas.map((f) => FORMA_LABELS[f.forma] ?? f.forma),
      datasets: [{
        data: formas.map((f) => f.valor),
        backgroundColor: PALETTE.slice(0, formas.length),
        borderWidth: 2,
        borderColor: '#ffffff',
        hoverOffset: 8
      }]
    },
    options: {
      ...baseLayout(),
      cutout: '62%',
      plugins: {
        ...basePlugins(),
        tooltip: {
          ...basePlugins().tooltip,
          callbacks: {
            label: (ctx) => {
              const valor = Number(ctx.parsed);
              const total = (ctx.dataset.data as number[]).reduce((a, b) => a + b, 0);
              const pct = total > 0 ? ((valor / total) * 100).toFixed(1) : '0';
              return ` ${formatBrl(valor)} (${pct}%)`;
            }
          }
        }
      }
    }
  };
}

export function buildProdutosChart(itens: ProdutoMaisVendido[]): ChartConfiguration<'bar'> {
  const top = itens.slice(0, 10);
  const labels = top.map((i) => truncar(i.descricao, 22));

  return {
    type: 'bar',
    data: {
      labels,
      datasets: [{
        label: 'Valor vendido',
        data: top.map((i) => i.valorTotal),
        backgroundColor: 'rgba(37, 99, 235, 0.85)',
        borderRadius: 6,
        borderSkipped: false
      }]
    },
    options: {
      indexAxis: 'y',
      ...baseLayout(),
      scales: {
        x: {
          beginAtZero: true,
          grid: { color: 'rgba(228, 228, 231, 0.8)' },
          ticks: { callback: (v) => formatBrl(Number(v)) }
        },
        y: { grid: { display: false } }
      },
      plugins: {
        ...basePlugins(),
        legend: { display: false },
        tooltip: {
          ...basePlugins().tooltip,
          callbacks: {
            title: (items) => {
              const idx = items[0]?.dataIndex ?? 0;
              return top[idx]?.descricao ?? '';
            },
            label: (ctx) => {
              const idx = ctx.dataIndex;
              const qtd = top[idx]?.quantidade ?? 0;
              return [` ${formatBrl(Number(ctx.parsed.x))}`, ` Qtd: ${qtd}`];
            }
          }
        }
      }
    }
  };
}

export function buildEstoqueChart(itens: EstoqueBaixoItem[]): ChartConfiguration<'bar'> {
  const top = itens.slice(0, 12);
  const labels = top.map((i) => truncar(i.descricao, 18));

  return {
    type: 'bar',
    data: {
      labels,
      datasets: [
        {
          label: 'Estoque atual',
          data: top.map((i) => i.estoqueAtual),
          backgroundColor: 'rgba(220, 38, 38, 0.8)',
          borderRadius: 4
        },
        {
          label: 'Estoque mínimo',
          data: top.map((i) => i.estoqueMinimo),
          backgroundColor: 'rgba(217, 119, 6, 0.75)',
          borderRadius: 4
        }
      ]
    },
    options: {
      ...baseLayout(),
      scales: {
        x: {
          grid: { display: false },
          ticks: { maxRotation: 45, minRotation: 0, autoSkip: true, maxTicksLimit: 10 }
        },
        y: { beginAtZero: true, grid: { color: 'rgba(228, 228, 231, 0.8)' } }
      },
      plugins: {
        ...basePlugins(),
        tooltip: {
          ...basePlugins().tooltip,
          callbacks: {
            title: (items) => {
              const idx = items[0]?.dataIndex ?? 0;
              return top[idx]?.descricao ?? '';
            }
          }
        }
      }
    }
  };
}

function truncar(texto: string, max: number): string {
  if (texto.length <= max) return texto;
  return `${texto.slice(0, max - 1)}…`;
}
