export const TAMANHO_PAGINA = 20;

export function paginar<T>(
  itens: readonly T[],
  pagina: number,
  tamanho = TAMANHO_PAGINA
): { itens: T[]; total: number } {
  const total = itens.length;
  const inicio = (pagina - 1) * tamanho;
  return { itens: itens.slice(inicio, inicio + tamanho), total };
}
