using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class Produto : AuditableEntity
{
    public string CodigoInterno { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string DescricaoNormalizada { get; set; } = string.Empty;
    public string? CategoriaId { get; set; }
    public string? Marca { get; set; }
    public string? Observacoes { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal PrecoVenda { get; set; }
    public decimal? Custo { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public bool Ativo { get; set; } = true;
    public string? ImagemExtensao { get; set; }
}
