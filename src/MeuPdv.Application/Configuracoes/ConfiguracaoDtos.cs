using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Configuracoes;

public record ConfiguracaoEmpresaDto(
    string NomeEstabelecimento,
    string? Cnpj,
    string? Endereco,
    string? ChavePix,
    TipoChavePix? TipoChavePix);

public record AtualizarConfiguracaoEmpresaRequest(
    string NomeEstabelecimento,
    string? Cnpj,
    string? Endereco,
    string? ChavePix,
    TipoChavePix? TipoChavePix);

public record ConfiguracaoSistemaDto(
    bool ImpressaoAutomatica,
    string? NomeImpressora,
    bool EstoqueNegativoPermitido,
    decimal LimiteDescontoOperadorPercentual,
    string DiretorioBackup,
    int IntervaloBackupMinutos,
    int DiasRetencaoBackup,
    string TemaVisual);

public record AtualizarConfiguracaoSistemaRequest(
    bool ImpressaoAutomatica,
    string? NomeImpressora,
    bool EstoqueNegativoPermitido,
    decimal LimiteDescontoOperadorPercentual,
    string? DiretorioBackup,
    int IntervaloBackupMinutos,
    int DiasRetencaoBackup);

public record ConfiguracaoPdvDto(
    string CorPrimaria,
    bool ExibirNomeEmpresa,
    string? NomeEmpresa,
    bool TemImagemFundo,
    long? ImagemFundoVersao);

public record AtualizarConfiguracaoPdvRequest(
    string CorPrimaria,
    bool ExibirNomeEmpresa,
    bool RemoverImagemFundo);

public record GerarPixQrRequest(decimal Valor, string? Identificador, string? Descricao);
