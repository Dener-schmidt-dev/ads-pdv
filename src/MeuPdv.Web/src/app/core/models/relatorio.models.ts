export interface VendasPorPeriodo {
  inicio: string;
  fim: string;
  quantidadeVendas: number;
  subtotal: number;
  descontos: number;
  total: number;
  porFormaPagamento: { forma: string; valor: number }[];
  vendas: VendaResumo[];
}

export interface VendaResumo {
  id: string;
  numero: string;
  data: string;
  subtotal: number;
  desconto: number;
  total: number;
  formasPagamento: string;
}

export interface ProdutosMaisVendidos {
  inicio: string;
  fim: string;
  itens: ProdutoMaisVendido[];
}

export interface ProdutoMaisVendido {
  produtoId: string;
  codigoInterno: string;
  descricao: string;
  quantidade: number;
  valorTotal: number;
}

export interface EstoqueBaixoItem {
  produtoId: string;
  codigoInterno: string;
  descricao: string;
  estoqueAtual: number;
  estoqueMinimo: number;
}
