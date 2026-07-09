namespace MeuPdv.Application.Options;

public class LicenseOptions
{
    public const string SectionName = "License";

    /// <summary>URL opcional para validação online (POST JSON { chave, machineId }).</summary>
    public string? ValidationUrl { get; set; }
}
