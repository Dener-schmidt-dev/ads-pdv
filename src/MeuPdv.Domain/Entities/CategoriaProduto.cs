using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class CategoriaProduto : AuditableEntity
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
