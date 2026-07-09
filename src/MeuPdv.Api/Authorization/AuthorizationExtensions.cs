using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace MeuPdv.Api.Authorization;

public static class AuthorizationExtensions
{
    public static void AddPermissaoPolicies(this AuthorizationOptions options)
    {
        foreach (var permissao in Enum.GetValues<Permissao>())
        {
            options.AddPolicy(permissao.ToString(), policy =>
                policy.Requirements.Add(new PermissaoRequirement(permissao)));
        }
    }
}
