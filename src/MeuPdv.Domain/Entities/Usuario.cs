using MeuPdv.Domain.Common;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Domain.Entities;

public class Usuario : AuditableEntity
{
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.OperadorCaixa;
    public List<Permissao> PermissoesExtras { get; set; } = [];
    public bool Ativo { get; set; } = true;
    public DateTime? UltimoAcesso { get; set; }
}
