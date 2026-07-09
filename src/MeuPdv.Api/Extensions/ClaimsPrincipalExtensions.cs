using System.Security.Claims;

namespace MeuPdv.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string ObterUsuarioId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("Usuário não autenticado.");
}
