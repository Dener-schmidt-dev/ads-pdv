using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class LicencaLocal : BaseEntity
{
    public string Chave { get; set; } = string.Empty;
    public DateTime Validade { get; set; }
    public bool ModoDesenvolvimento { get; set; }
    public DateTime? UltimaValidacao { get; set; }
    public DateTime? AtivadaEm { get; set; }
    public string TipoAtivacao { get; set; } = "Local";
}
