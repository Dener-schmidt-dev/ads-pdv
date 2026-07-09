namespace MeuPdv.Application.Licenca;

public record LicencaInfoDto(
    bool Valida,
    bool EmTolerancia,
    DateTime? Validade,
    string? Aviso,
    string? ChaveMascarada,
    DateTime? AtivadaEm,
    string TipoAtivacao);

public record AtivarLicencaRequest(string Chave);

public record GerarChaveDemoResponse(string Chave, DateTime Validade);
