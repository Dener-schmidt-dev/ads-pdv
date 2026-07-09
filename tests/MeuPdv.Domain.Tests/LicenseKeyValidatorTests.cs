using MeuPdv.Domain.Security;

namespace MeuPdv.Domain.Tests;

public class LicenseKeyValidatorTests
{
    [Fact]
    public void GerarChaveDemonstracao_DeveValidar()
    {
        var validade = new DateTime(2027, 6, 12, 0, 0, 0, DateTimeKind.Utc);
        var chave = LicenseKeyValidator.GerarChaveDemonstracao(validade);

        var ok = LicenseKeyValidator.TryValidar(chave, out var parsed, out var erro);

        Assert.True(ok, erro);
        Assert.Null(erro);
        Assert.Equal(2027, parsed.Year);
        Assert.Equal(6, parsed.Month);
        Assert.Equal(12, parsed.Day);
    }

    [Fact]
    public void TryValidar_ChaveInvalida_RetornaFalso()
    {
        var ok = LicenseKeyValidator.TryValidar("INVALIDA", out _, out var erro);

        Assert.False(ok);
        Assert.NotNull(erro);
    }
}
