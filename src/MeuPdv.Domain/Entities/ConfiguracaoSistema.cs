using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class ConfiguracaoSistema : BaseEntity
{
    public bool ImpressaoAutomatica { get; set; }
    public string? NomeImpressora { get; set; }
    public bool EstoqueNegativoPermitido { get; set; }
    public decimal LimiteDescontoOperadorPercentual { get; set; } = 5m;
    public string DiretorioBackup { get; set; } = string.Empty;
    public int IntervaloBackupMinutos { get; set; } = 1440;
    public int DiasRetencaoBackup { get; set; } = 30;
    public string TemaVisual { get; set; } = "claro";
    public string PdvCorPrimaria { get; set; } = "#2563eb";
    public bool PdvExibirNomeEmpresa { get; set; } = true;
    public string? PdvImagemFundoExtensao { get; set; }
    public bool FiscalEnabled { get; set; }
    public int ToleranciaLicencaDias { get; set; } = 3;
}
