using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class Cliente : AuditableEntity
{
    public string Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
}
