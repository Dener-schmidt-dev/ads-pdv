export interface ClienteDto {
  id: string;
  nome: string;
  cpf?: string;
  telefone?: string;
  observacoes?: string;
  ativo: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ClienteListaResponse {
  itens: ClienteDto[];
  total: number;
}
