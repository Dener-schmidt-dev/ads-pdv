using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class ConfiguracaoEmpresa : BaseEntity
{
    public string NomeEstabelecimento { get; set; } = "MeuPDV";
    public string? Cnpj { get; set; }
    public string? Endereco { get; set; }
    public string? ChavePix { get; set; }
    public TipoChavePix? TipoChavePix { get; set; }
}
