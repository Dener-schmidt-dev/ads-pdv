using MeuPdv.Domain.Common;

namespace MeuPdv.Domain.Entities;

public class HistoricoBackup : BaseEntity
{
    public string CaminhoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public bool Automatico { get; set; }
}
