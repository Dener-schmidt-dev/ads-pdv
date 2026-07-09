using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace MeuPdv.Api.Authorization;

public class PermissaoRequirement : IAuthorizationRequirement
{
    public Permissao Permissao { get; }

    public PermissaoRequirement(Permissao permissao) => Permissao = permissao;
}

public class PermissaoAuthorizationHandler : AuthorizationHandler<PermissaoRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissaoRequirement requirement)
    {
        var permissoes = context.User.FindAll("perm").Select(c => c.Value);
        if (permissoes.Contains(requirement.Permissao.ToString()))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
