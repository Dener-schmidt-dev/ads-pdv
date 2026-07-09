export function urlImagemProduto(
  produtoId: string,
  temImagem: boolean,
  cacheKey?: string
): string | null {
  if (!temImagem) return null;
  const v = cacheKey ? `?v=${encodeURIComponent(cacheKey)}` : '';
  return `/api/produtos/${produtoId}/imagem${v}`;
}
