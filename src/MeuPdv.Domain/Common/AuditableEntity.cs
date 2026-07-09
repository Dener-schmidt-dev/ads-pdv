namespace MeuPdv.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}
