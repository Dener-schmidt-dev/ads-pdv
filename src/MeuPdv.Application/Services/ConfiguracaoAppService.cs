using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Configuracoes;
using MeuPdv.Domain.Exceptions;

namespace MeuPdv.Application.Services;

public class ConfiguracaoAppService : IConfiguracaoAppService
{
    private readonly IConfiguracaoEmpresaRepository _empresaRepository;
    private readonly IConfiguracaoSistemaRepository _sistemaRepository;
    private readonly IPdvPersonalizacaoStorage _pdvStorage;

    public ConfiguracaoAppService(
        IConfiguracaoEmpresaRepository empresaRepository,
        IConfiguracaoSistemaRepository sistemaRepository,
        IPdvPersonalizacaoStorage pdvStorage)
    {
        _empresaRepository = empresaRepository;
        _sistemaRepository = sistemaRepository;
        _pdvStorage = pdvStorage;
    }

    public async Task<ConfiguracaoEmpresaDto> ObterEmpresaAsync(CancellationToken ct = default)
    {
        var e = await _empresaRepository.ObterAsync(ct);
        return MapearEmpresa(e);
    }

    public async Task<ConfiguracaoEmpresaDto> AtualizarEmpresaAsync(
        AtualizarConfiguracaoEmpresaRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NomeEstabelecimento))
            throw new DomainException("Nome do estabelecimento é obrigatório.");

        var e = await _empresaRepository.ObterAsync(ct);
        e.NomeEstabelecimento = request.NomeEstabelecimento.Trim();
        e.Cnpj = string.IsNullOrWhiteSpace(request.Cnpj) ? null : request.Cnpj.Trim();
        e.Endereco = string.IsNullOrWhiteSpace(request.Endereco) ? null : request.Endereco.Trim();
        e.ChavePix = string.IsNullOrWhiteSpace(request.ChavePix) ? null : request.ChavePix.Trim();
        e.TipoChavePix = request.TipoChavePix;

        await _empresaRepository.SalvarAsync(e, ct);
        return MapearEmpresa(e);
    }

    public async Task<ConfiguracaoSistemaDto> ObterSistemaAsync(CancellationToken ct = default)
    {
        var s = await _sistemaRepository.ObterAsync(ct);
        return MapearSistema(s);
    }

    public async Task<ConfiguracaoSistemaDto> AtualizarSistemaAsync(
        AtualizarConfiguracaoSistemaRequest request, CancellationToken ct = default)
    {
        if (request.LimiteDescontoOperadorPercentual < 0 || request.LimiteDescontoOperadorPercentual > 100)
            throw new DomainException("Limite de desconto deve estar entre 0 e 100.");

        var s = await _sistemaRepository.ObterAsync(ct);
        s.ImpressaoAutomatica = request.ImpressaoAutomatica;
        s.NomeImpressora = string.IsNullOrWhiteSpace(request.NomeImpressora) ? null : request.NomeImpressora.Trim();
        s.EstoqueNegativoPermitido = request.EstoqueNegativoPermitido;
        s.LimiteDescontoOperadorPercentual = request.LimiteDescontoOperadorPercentual;

        if (!string.IsNullOrWhiteSpace(request.DiretorioBackup))
            s.DiretorioBackup = request.DiretorioBackup.Trim();

        s.IntervaloBackupMinutos = Math.Max(60, request.IntervaloBackupMinutos);
        s.DiasRetencaoBackup = Math.Max(1, request.DiasRetencaoBackup);

        await _sistemaRepository.SalvarAsync(s, ct);
        return MapearSistema(s);
    }

    public async Task<ConfiguracaoPdvDto> ObterPdvAsync(CancellationToken ct = default)
    {
        var s = await _sistemaRepository.ObterAsync(ct);
        var e = await _empresaRepository.ObterAsync(ct);
        return MapearPdv(s, e);
    }

    public async Task<ConfiguracaoPdvDto> AtualizarPdvAsync(
        AtualizarConfiguracaoPdvRequest request, CancellationToken ct = default)
    {
        ValidarCorPrimaria(request.CorPrimaria);

        var s = await _sistemaRepository.ObterAsync(ct);
        s.PdvCorPrimaria = request.CorPrimaria.Trim();
        s.PdvExibirNomeEmpresa = request.ExibirNomeEmpresa;

        if (request.RemoverImagemFundo)
        {
            _pdvStorage.RemoverImagemFundo();
            s.PdvImagemFundoExtensao = null;
        }

        await _sistemaRepository.SalvarAsync(s, ct);
        var e = await _empresaRepository.ObterAsync(ct);
        return MapearPdv(s, e);
    }

    public async Task<ConfiguracaoPdvDto> SalvarImagemFundoPdvAsync(
        Stream arquivo, long tamanhoBytes, string contentType, string? nomeArquivo, CancellationToken ct = default)
    {
        if (tamanhoBytes <= 0)
            throw new DomainException("Arquivo vazio.");

        if (tamanhoBytes > 2 * 1024 * 1024)
            throw new DomainException("A imagem deve ter no máximo 2 MB.");

        var extensao = PdvPersonalizacaoStorage.NormalizarExtensao(contentType, nomeArquivo);
        await _pdvStorage.SalvarImagemFundoAsync(arquivo, extensao, ct);

        var s = await _sistemaRepository.ObterAsync(ct);
        s.PdvImagemFundoExtensao = extensao;
        await _sistemaRepository.SalvarAsync(s, ct);

        var e = await _empresaRepository.ObterAsync(ct);
        return MapearPdv(s, e);
    }

    public Task<(string Caminho, string ContentType)?> ObterImagemFundoPdvAsync(CancellationToken ct = default) =>
        Task.FromResult(_pdvStorage.ObterArquivoImagemFundo());

    private static void ValidarCorPrimaria(string cor)
    {
        if (string.IsNullOrWhiteSpace(cor) || !System.Text.RegularExpressions.Regex.IsMatch(cor.Trim(), "^#[0-9A-Fa-f]{6}$"))
            throw new DomainException("Informe uma cor primária válida (#RRGGBB).");
    }

    private ConfiguracaoPdvDto MapearPdv(
        Domain.Entities.ConfiguracaoSistema s,
        Domain.Entities.ConfiguracaoEmpresa e) => new(
        string.IsNullOrWhiteSpace(s.PdvCorPrimaria) ? "#2563eb" : s.PdvCorPrimaria,
        s.PdvExibirNomeEmpresa,
        e.NomeEstabelecimento,
        !string.IsNullOrWhiteSpace(s.PdvImagemFundoExtensao) && _pdvStorage.ExisteImagemFundo(),
        s.PdvImagemFundoExtensao is not null ? s.UpdatedAt.Ticks : null);

    private static ConfiguracaoEmpresaDto MapearEmpresa(Domain.Entities.ConfiguracaoEmpresa e) => new(
        e.NomeEstabelecimento, e.Cnpj, e.Endereco, e.ChavePix, e.TipoChavePix);

    private static ConfiguracaoSistemaDto MapearSistema(Domain.Entities.ConfiguracaoSistema s) => new(
        s.ImpressaoAutomatica,
        s.NomeImpressora,
        s.EstoqueNegativoPermitido,
        s.LimiteDescontoOperadorPercentual,
        s.DiretorioBackup,
        s.IntervaloBackupMinutos,
        s.DiasRetencaoBackup,
        s.TemaVisual);
}
