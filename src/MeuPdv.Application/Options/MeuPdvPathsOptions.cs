namespace MeuPdv.Application.Options;

public class MeuPdvPathsOptions
{
    public const string SectionName = "MeuPdvPaths";

    public string DataDirectory { get; set; } = string.Empty;
    public string LogsDirectory { get; set; } = string.Empty;
    public string BackupsDirectory { get; set; } = string.Empty;
}
