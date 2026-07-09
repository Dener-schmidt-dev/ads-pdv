export interface ProdutoDto {
  id: string;
  codigoInterno: string;
  codigoBarras?: string;
  descricao: string;
  categoriaId?: string;
  categoriaNome?: string;
  marca?: string;
  observacoes?: string;
  unidade: string;
  precoVenda: number;
  custo?: number;
  estoqueAtual: number;
  estoqueMinimo: number;
  ativo: boolean;
  abaixoMinimo: boolean;
  temImagem: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ProdutoListaResponse {
  itens: ProdutoDto[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

export interface CategoriaDto {
  id: string;
  nome: string;
  ativo: boolean;
}

export interface ImportacaoCsvResultado {
  importados: number;
  ignorados: number;
  erros: string[];
}

export interface SaldoProdutoDto {
  produtoId: string;
  codigoInterno: string;
  descricao: string;
  estoqueAtual: number;
  estoqueMinimo: number;
  abaixoMinimo: boolean;
}

export interface AlertaEstoqueDto {
  produtoId: string;
  codigoInterno: string;
  descricao: string;
  estoqueAtual: number;
  estoqueMinimo: number;
}

export interface MovimentacaoEstoqueDto {
  id: string;
  produtoId: string;
  produtoDescricao: string;
  origem: string;
  quantidade: number;
  saldoAnterior: number;
  saldoPosterior: number;
  motivo?: string;
  createdAt: string;
}
