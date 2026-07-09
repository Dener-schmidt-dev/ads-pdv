using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IVendaRepository : IRepository<Venda>
{
    Task<long> ObterProximoNumeroAsync(CancellationToken ct = default);

    Task<(IReadOnlyList<Venda> Itens, long Total)> ListarConcluidasPaginadoAsync(
        string? termo, int skip, int take, CancellationToken ct = default);
}
