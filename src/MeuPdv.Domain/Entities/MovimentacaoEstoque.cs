using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class MovimentacaoEstoque : AuditableEntity
{
    public string ProdutoId { get; set; } = string.Empty;
    public OrigemMovimentacaoEstoque Origem { get; set; }
    public decimal Quantidade { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    public string? Motivo { get; set; }
    public string? VendaId { get; set; }
    public string OperadorId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}
