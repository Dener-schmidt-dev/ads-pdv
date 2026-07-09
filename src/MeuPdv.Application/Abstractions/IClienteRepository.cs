using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<IReadOnlyList<Cliente>> BuscarAsync(string termo, int limite = 10, CancellationToken ct = default);

    Task<(IReadOnlyList<Cliente> Itens, long Total)> ListarPaginadoAsync(
        string? termo,
        bool? ativo,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<Cliente?> GetByCpfAsync(string cpf, CancellationToken ct = default);
}
