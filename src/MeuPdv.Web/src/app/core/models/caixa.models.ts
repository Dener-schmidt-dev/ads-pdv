export interface SessaoCaixaDto {
  id: string;
  caixaIdentificador: string;
  operadorAberturaId: string;
  status: string;
  valorInicial: number;
  abertaEm: string;
  fechadaEm?: string;
}

export interface ResumoFechamentoDto {
  sessaoCaixaId: string;
  valorInicial: number;
  vendasPorForma: { forma: string; total: number }[];
  totalSuprimentos: number;
  totalSangrias: number;
  valorEsperadoDinheiro: number;
  valorContado: number;
  diferenca: number;
  fechadaEm: string;
}
