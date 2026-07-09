namespace MeuPdv.Application.Produtos;

public record ProdutoDto(
    string Id,
    string CodigoInterno,
    string? CodigoBarras,
    string Descricao,
    string? CategoriaId,
    string? CategoriaNome,
    string? Marca,
    string? Observacoes,
    string Unidade,
    decimal PrecoVenda,
    decimal? Custo,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    bool Ativo,
    bool AbaixoMinimo,
    bool TemImagem,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CriarProdutoRequest(
    string? CodigoInterno,
    string? CodigoBarras,
    string Descricao,
    string? CategoriaId,
    string? Marca,
    string? Observacoes,
    string Unidade,
    decimal PrecoVenda,
    decimal? Custo,
    decimal EstoqueAtual,
    decimal EstoqueMinimo);

public record CriarProdutoRapidoRequest(
    string Descricao,
    decimal PrecoVenda,
    string? CodigoBarras);

public record AtualizarProdutoRequest(
    string? CodigoBarras,
    string Descricao,
    string? CategoriaId,
    string? Marca,
    string? Observacoes,
    string Unidade,
    decimal PrecoVenda,
    decimal? Custo,
    decimal EstoqueMinimo,
    bool Ativo);

public record ProdutoListaResponse(
    IReadOnlyList<ProdutoDto> Itens,
    long Total,
    int Pagina,
    int TamanhoPagina);

public record ImportacaoCsvResultado(
    int Importados,
    int Ignorados,
    IReadOnlyList<string> Erros);
