export interface ClienteLookupValue {
  id: string;
  nome: string;
}

export interface ProdutoLookupValue {
  id: string;
  codigoInterno: string;
  codigoBarras?: string;
  descricao: string;
  precoVenda: number;
  estoqueAtual: number;
  unidade: string;
  temImagem: boolean;
}
