namespace MeuPdv.Domain.Entities;

public class VendaItem
{
    public string ProdutoId { get; set; } = string.Empty;
    public string CodigoInterno { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal DescontoItem { get; set; }
    public decimal Total => Math.Round(Quantidade * PrecoUnitario - DescontoItem, 2);
    public bool Cancelado { get; set; }
}
