using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Security;

public static class PermissaoPorPerfil
{
    private static readonly IReadOnlyDictionary<PerfilUsuario, Permissao[]> Mapa = new Dictionary<PerfilUsuario, Permissao[]>
    {
        [PerfilUsuario.Administrador] = Enum.GetValues<Permissao>(),
        [PerfilUsuario.OperadorCaixa] =
        [
            Permissao.ConcederDesconto,
            Permissao.CancelarItem,
            Permissao.CancelarVenda,
            Permissao.AbrirCaixa,
            Permissao.FecharCaixa
        ],
        [PerfilUsuario.Fiscal] =
        [
            Permissao.VisualizarRelatorios,
            Permissao.AcessarConfiguracoes
        ]
    };

    public static IReadOnlyList<Permissao> ObterPermissoes(PerfilUsuario perfil, IEnumerable<Permissao>? extras = null)
    {
        var basePerms = Mapa.TryGetValue(perfil, out var perms)
            ? perms.ToList()
            : [];

        if (extras is not null)
        {
            foreach (var extra in extras)
            {
                if (!basePerms.Contains(extra))
                    basePerms.Add(extra);
            }
        }

        return basePerms;
    }

    public static bool PossuiPermissao(
        PerfilUsuario perfil,
        Permissao permissao,
        IEnumerable<Permissao>? extras = null) =>
        ObterPermissoes(perfil, extras).Contains(permissao);
}
