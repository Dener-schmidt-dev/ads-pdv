using MeuPdv.Application.Auth;

namespace MeuPdv.Application.Abstractions;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UsuarioLogadoDto?> ObterUsuarioLogadoAsync(string usuarioId, CancellationToken ct = default);
    Task AlterarSenhaAsync(string usuarioId, AlterarSenhaRequest request, CancellationToken ct = default);
}
