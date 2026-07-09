using MeuPdv.Application.Configuracoes;

namespace MeuPdv.Application.Abstractions;

public interface IConfiguracaoAppService
{
    Task<ConfiguracaoEmpresaDto> ObterEmpresaAsync(CancellationToken ct = default);
    Task<ConfiguracaoEmpresaDto> AtualizarEmpresaAsync(AtualizarConfiguracaoEmpresaRequest request, CancellationToken ct = default);
    Task<ConfiguracaoSistemaDto> ObterSistemaAsync(CancellationToken ct = default);
    Task<ConfiguracaoSistemaDto> AtualizarSistemaAsync(AtualizarConfiguracaoSistemaRequest request, CancellationToken ct = default);
    Task<ConfiguracaoPdvDto> ObterPdvAsync(CancellationToken ct = default);
    Task<ConfiguracaoPdvDto> AtualizarPdvAsync(AtualizarConfiguracaoPdvRequest request, CancellationToken ct = default);
    Task<ConfiguracaoPdvDto> SalvarImagemFundoPdvAsync(Stream arquivo, long tamanhoBytes, string contentType, string? nomeArquivo, CancellationToken ct = default);
    Task<(string Caminho, string ContentType)?> ObterImagemFundoPdvAsync(CancellationToken ct = default);
}
