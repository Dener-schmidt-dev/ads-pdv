using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface ICategoriaProdutoRepository : IRepository<CategoriaProduto>
{
    Task<CategoriaProduto?> GetByNomeAsync(string nome, CancellationToken ct = default);
}
