using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Abstractions;

public interface ISessaoCaixaRepository : IRepository<SessaoCaixa>
{
    Task<SessaoCaixa?> ObterAbertaAsync(string caixaIdentificador, CancellationToken ct = default);
    Task<IReadOnlyList<SessaoCaixa>> ListarHistoricoAsync(int skip, int take, CancellationToken ct = default);
}
