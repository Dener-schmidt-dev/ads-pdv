using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class Venda : AuditableEntity
{
    public string Numero { get; set; } = string.Empty;
    public string SessaoCaixaId { get; set; } = string.Empty;
    public string OperadorId { get; set; } = string.Empty;
    public string? ClienteId { get; set; }
    public StatusVenda Status { get; set; } = StatusVenda.EmAndamento;
    public List<VendaItem> Itens { get; set; } = [];
    public List<Pagamento> Pagamentos { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public string? MotivoDesconto { get; set; }
    public string? MotivoCancelamento { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public StatusFiscal StatusFiscal { get; set; } = StatusFiscal.NaoAplicavel;
    public string? XmlFiscal { get; set; }
    public string? ChaveFiscal { get; set; }
    public string? ProtocoloFiscal { get; set; }
}
