using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class MovimentacaoCaixa : AuditableEntity
{
    public string SessaoCaixaId { get; set; } = string.Empty;
    public TipoMovimentacaoCaixa Tipo { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public string? Motivo { get; set; }
    public string? VendaId { get; set; }
    public string OperadorId { get; set; } = string.Empty;
}
