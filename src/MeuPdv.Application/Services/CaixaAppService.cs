using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Caixa;
using MeuPdv.Application.Common;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Application.Services;

public class CaixaAppService : ICaixaAppService
{
    private readonly ISessaoCaixaRepository _sessaoRepository;
    private readonly IMovimentacaoCaixaRepository _movimentacaoRepository;

    public CaixaAppService(
        ISessaoCaixaRepository sessaoRepository,
        IMovimentacaoCaixaRepository movimentacaoRepository)
    {
        _sessaoRepository = sessaoRepository;
        _movimentacaoRepository = movimentacaoRepository;
    }

    public async Task<SessaoCaixaDto?> ObterSessaoAbertaAsync(string caixaIdentificador, CancellationToken ct = default)
    {
        var sessao = await _sessaoRepository.ObterAbertaAsync(caixaIdentificador, ct);
        return sessao is null ? null : MapearSessao(sessao);
    }

    public async Task<SessaoCaixaDto> AbrirAsync(AbrirCaixaRequest request, string operadorId, CancellationToken ct = default)
    {
        var caixa = string.IsNullOrWhiteSpace(request.CaixaIdentificador)
            ? "CAIXA-01"
            : request.CaixaIdentificador.Trim().ToUpperInvariant();

        if (request.ValorInicial < 0)
            throw new DomainException("Valor inicial não pode ser negativo.");

        var aberta = await _sessaoRepository.ObterAbertaAsync(caixa, ct);
        if (aberta is not null)
            throw new DomainException("Já existe um caixa aberto para este terminal.");

        var sessao = new SessaoCaixa
        {
            CaixaIdentificador = caixa,
            OperadorAberturaId = operadorId,
            Status = StatusSessaoCaixa.Aberta,
            ValorInicial = request.ValorInicial,
            AbertaEm = DateTime.UtcNow,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _sessaoRepository.InsertAsync(sessao, ct);

        await _movimentacaoRepository.InsertAsync(new MovimentacaoCaixa
        {
            SessaoCaixaId = sessao.Id,
            Tipo = TipoMovimentacaoCaixa.Abertura,
            Valor = request.ValorInicial,
            Motivo = "Abertura de caixa",
            OperadorId = operadorId,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        }, ct);

        return MapearSessao(sessao);
    }

    public async Task<MovimentacaoCaixaDto> SuprimentoAsync(
        SuprimentoSangriaRequest request, string operadorId, CancellationToken ct = default) =>
        await RegistrarMovimentacaoAsync(request, operadorId, TipoMovimentacaoCaixa.Suprimento, ct);

    public async Task<MovimentacaoCaixaDto> SangriaAsync(
        SuprimentoSangriaRequest request, string operadorId, CancellationToken ct = default) =>
        await RegistrarMovimentacaoAsync(request, operadorId, TipoMovimentacaoCaixa.Sangria, ct);

    public async Task<ResumoFechamentoDto> FecharAsync(
        FecharCaixaRequest request, string operadorId, CancellationToken ct = default)
    {
        var sessao = await ObterSessaoAbertaPorIdAsync(request.SessaoCaixaId, ct);
        var movs = await _movimentacaoRepository.ListarPorSessaoAsync(sessao.Id, ct);

        var suprimentos = movs.Where(m => m.Tipo == TipoMovimentacaoCaixa.Suprimento).Sum(m => m.Valor);
        var sangrias = movs.Where(m => m.Tipo == TipoMovimentacaoCaixa.Sangria).Sum(m => m.Valor);

        var vendas = movs
            .Where(m => (m.Tipo == TipoMovimentacaoCaixa.Venda || m.Tipo == TipoMovimentacaoCaixa.CancelamentoVenda)
                        && m.FormaPagamento.HasValue)
            .Select(m => (
                m.FormaPagamento!.Value,
                m.Tipo == TipoMovimentacaoCaixa.CancelamentoVenda ? -m.Valor : m.Valor))
            .ToList();

        var vendasPorForma = CaixaCalculator.AgruparPorForma(vendas)
            .Select(kv => new ResumoFormaPagamentoDto(kv.Key, kv.Value))
            .ToList();

        var vendasDinheiro = vendasPorForma
            .FirstOrDefault(v => v.Forma == FormaPagamento.Dinheiro)?.Total ?? 0m;

        var esperado = CaixaCalculator.CalcularEsperadoDinheiro(
            sessao.ValorInicial, vendasDinheiro, suprimentos, sangrias);

        var diferenca = CaixaCalculator.CalcularDiferenca(request.ValorContado, esperado);

        sessao.Status = StatusSessaoCaixa.Fechada;
        sessao.OperadorFechamentoId = operadorId;
        sessao.ValorContado = request.ValorContado;
        sessao.ValorEsperado = esperado;
        sessao.Diferenca = diferenca;
        sessao.FechadaEm = DateTime.UtcNow;
        sessao.ObservacaoFechamento = request.Observacao;
        sessao.UpdatedByUserId = operadorId;

        await _sessaoRepository.UpdateAsync(sessao, ct);

        await _movimentacaoRepository.InsertAsync(new MovimentacaoCaixa
        {
            SessaoCaixaId = sessao.Id,
            Tipo = TipoMovimentacaoCaixa.Fechamento,
            Valor = request.ValorContado,
            Motivo = request.Observacao ?? "Fechamento de caixa",
            OperadorId = operadorId,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        }, ct);

        return new ResumoFechamentoDto(
            sessao.Id,
            sessao.ValorInicial,
            vendasPorForma,
            suprimentos,
            sangrias,
            esperado,
            request.ValorContado,
            diferenca,
            sessao.FechadaEm!.Value);
    }

    public async Task<PaginaListaDto<SessaoCaixaDto>> HistoricoAsync(int pagina, int tamanho, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 50);
        var skip = (pagina - 1) * tamanho;

        var total = await _sessaoRepository.CountAsync(ct: ct);
        var sessoes = await _sessaoRepository.ListarHistoricoAsync(skip, tamanho, ct);
        return new PaginaListaDto<SessaoCaixaDto>(sessoes.Select(MapearSessao).ToList(), total);
    }

    public async Task ValidarCaixaAbertoAsync(string caixaIdentificador, CancellationToken ct = default)
    {
        var sessao = await _sessaoRepository.ObterAbertaAsync(caixaIdentificador, ct);
        if (sessao is null)
            throw new DomainException("Caixa fechado. Abra o caixa antes de realizar vendas.");
    }

    private async Task<MovimentacaoCaixaDto> RegistrarMovimentacaoAsync(
        SuprimentoSangriaRequest request,
        string operadorId,
        TipoMovimentacaoCaixa tipo,
        CancellationToken ct)
    {
        if (request.Valor <= 0)
            throw new DomainException("Valor deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new DomainException("Motivo é obrigatório.");

        var sessao = await ObterSessaoAbertaPorIdAsync(request.SessaoCaixaId, ct);

        var mov = new MovimentacaoCaixa
        {
            SessaoCaixaId = sessao.Id,
            Tipo = tipo,
            Valor = request.Valor,
            Motivo = request.Motivo.Trim(),
            OperadorId = operadorId,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _movimentacaoRepository.InsertAsync(mov, ct);
        return new MovimentacaoCaixaDto(mov.Id, mov.SessaoCaixaId, mov.Tipo, mov.Valor, mov.Motivo, mov.CreatedAt);
    }

    private async Task<SessaoCaixa> ObterSessaoAbertaPorIdAsync(string sessaoId, CancellationToken ct)
    {
        var sessao = await _sessaoRepository.GetByIdAsync(sessaoId, ct)
            ?? throw new DomainException("Sessão de caixa não encontrada.");

        if (sessao.Status != StatusSessaoCaixa.Aberta)
            throw new DomainException("Esta sessão de caixa já está fechada.");

        return sessao;
    }

    private static SessaoCaixaDto MapearSessao(SessaoCaixa s) => new(
        s.Id, s.CaixaIdentificador, s.OperadorAberturaId, s.Status,
        s.ValorInicial, s.AbertaEm, s.FechadaEm);
}
