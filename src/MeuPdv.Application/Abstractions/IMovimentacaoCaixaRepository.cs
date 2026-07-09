using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IMovimentacaoCaixaRepository : IRepository<MovimentacaoCaixa>
{
    Task<IReadOnlyList<MovimentacaoCaixa>> ListarPorSessaoAsync(string sessaoCaixaId, CancellationToken ct = default);
}
