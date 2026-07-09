using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class Pagamento
{
    public FormaPagamento Forma { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorRecebido { get; set; }
    public decimal Troco { get; set; }
}
