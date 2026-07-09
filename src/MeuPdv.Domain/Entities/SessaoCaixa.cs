using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class SessaoCaixa : AuditableEntity
{
    public string CaixaIdentificador { get; set; } = "CAIXA-01";
    public string OperadorAberturaId { get; set; } = string.Empty;
    public string? OperadorFechamentoId { get; set; }
    public StatusSessaoCaixa Status { get; set; } = StatusSessaoCaixa.Aberta;
    public decimal ValorInicial { get; set; }
    public decimal ValorContado { get; set; }
    public decimal ValorEsperado { get; set; }
    public decimal Diferenca { get; set; }
    public DateTime AbertaEm { get; set; } = DateTime.UtcNow;
    public DateTime? FechadaEm { get; set; }
    public string? ObservacaoFechamento { get; set; }
}
