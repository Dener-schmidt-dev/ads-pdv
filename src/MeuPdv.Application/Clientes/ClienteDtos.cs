namespace MeuPdv.Application.Clientes;

public record ClienteDetalheDto(
    string Id,
    string Nome,
    string? Cpf,
    string? Telefone,
    string? Observacoes,
    bool Ativo,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ClienteListaResponse(
    IReadOnlyList<ClienteDetalheDto> Itens,
    long Total);

public record CriarClienteRequest(
    string Nome,
    string? Cpf,
    string? Telefone,
    string? Observacoes);

public record AtualizarClienteRequest(
    string Nome,
    string? Cpf,
    string? Telefone,
    string? Observacoes);
