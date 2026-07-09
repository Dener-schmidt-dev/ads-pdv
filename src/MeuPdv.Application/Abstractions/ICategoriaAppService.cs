using MeuPdv.Application.Produtos;

namespace MeuPdv.Application.Abstractions;

public interface ICategoriaAppService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct = default);
    Task<CategoriaDto> CriarAsync(CriarCategoriaRequest request, string operadorId, CancellationToken ct = default);
}
