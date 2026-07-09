using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Security;

namespace MeuPdv.Domain.Tests.Security;

public class PermissaoPorPerfilTests
{
    [Fact]
    public void Administrador_PossuiTodasPermissoes()
    {
        foreach (var permissao in Enum.GetValues<Permissao>())
        {
            Assert.True(PermissaoPorPerfil.PossuiPermissao(PerfilUsuario.Administrador, permissao));
        }
    }

    [Fact]
    public void OperadorCaixa_PodeAbrirCaixa()
    {
        Assert.True(PermissaoPorPerfil.PossuiPermissao(PerfilUsuario.OperadorCaixa, Permissao.AbrirCaixa));
        Assert.False(PermissaoPorPerfil.PossuiPermissao(PerfilUsuario.OperadorCaixa, Permissao.CadastrarProduto));
    }

    [Fact]
    public void PermissoesExtras_SaoAplicadas()
    {
        var extras = new[] { Permissao.CadastrarProduto };
        Assert.True(PermissaoPorPerfil.PossuiPermissao(PerfilUsuario.OperadorCaixa, Permissao.CadastrarProduto, extras));
    }
}
