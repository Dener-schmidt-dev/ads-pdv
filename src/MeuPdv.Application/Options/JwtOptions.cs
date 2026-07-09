namespace MeuPdv.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = "MeuPDV-Dev-Secret-Change-In-Production-Min32Chars!";
    public string Issuer { get; set; } = "MeuPDV";
    public string Audience { get; set; } = "MeuPDV-Desktop";
    public int ExpiracaoHoras { get; set; } = 12;
}
