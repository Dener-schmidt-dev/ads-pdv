using MeuPdv.Application.Auth;

namespace MeuPdv.Application.Abstractions;

public interface IUsuarioAppService
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default);
    Task<UsuarioDto?> ObterPorIdAsync(string id, CancellationToken ct = default);
    Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, string operadorId, CancellationToken ct = default);
    Task<UsuarioDto> AtualizarAsync(string id, AtualizarUsuarioRequest request, string operadorId, CancellationToken ct = default);
    Task InativarAsync(string id, string operadorId, CancellationToken ct = default);
}
