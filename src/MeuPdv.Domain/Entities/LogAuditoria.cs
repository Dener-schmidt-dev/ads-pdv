using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class LogAuditoria : BaseEntity
{
    public string UsuarioId { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string? EntidadeId { get; set; }
    public string? Detalhes { get; set; }
}
