export interface ProdutoLeitura {
  id: string;
  codigoInterno: string;
  codigoBarras?: string;
  descricao: string;
  precoVenda: number;
  estoqueAtual: number;
  unidade: string;
  temImagem: boolean;
}

export interface CarrinhoItem {
  produtoId: string;
  codigoInterno: string;
  codigoBarras?: string;
  descricao: string;
  quantidade: number;
  precoUnitario: number;
  temImagem: boolean;
}

export interface ClienteVenda {
  id: string;
  nome: string;
}

export interface PagamentoVenda {
  forma: string;
  valor: number;
  valorRecebido: number;
}

export interface PixQrResponse {
  copiaCola: string;
  qrCodeBase64?: string;
}

export interface FinalizarVendaResponse {
  vendaId: string;
  numero: string;
  total: number;
  troco: number;
  comprovanteTexto?: string;
  pix?: PixQrResponse;
  imprimirComprovante: boolean;
}

export interface VendaListaItem {
  id: string;
  numero: string;
  data: string;
  total: number;
  status: string;
  formasPagamento: string;
}

export interface VendaListaResponse {
  itens: VendaListaItem[];
  total: number;
}

export interface VendaDetalhe {
  id: string;
  numero: string;
  data: string;
  subtotal: number;
  desconto: number;
  total: number;
  status: string;
  motivoCancelamento?: string;
  itens: { descricao: string; quantidade: number; precoUnitario: number; total: number }[];
  pagamentos: { forma: string; valor: number }[];
}
