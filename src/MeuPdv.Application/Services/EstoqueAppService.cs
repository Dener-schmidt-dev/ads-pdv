using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Common;
using MeuPdv.Application.Estoque;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Application.Services;

public class EstoqueAppService : IEstoqueAppService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IMovimentacaoEstoqueRepository _movimentacaoRepository;
    private readonly IConfiguracaoSistemaRepository _configuracaoRepository;

    public EstoqueAppService(
        IProdutoRepository produtoRepository,
        IMovimentacaoEstoqueRepository movimentacaoRepository,
        IConfiguracaoSistemaRepository configuracaoRepository)
    {
        _produtoRepository = produtoRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _configuracaoRepository = configuracaoRepository;
    }

    public async Task<SaldoProdutoDto?> ConsultarSaldoAsync(string produtoId, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(produtoId, ct);
        return produto is null ? null : MapearSaldo(produto);
    }

    public async Task<IReadOnlyList<SaldoProdutoDto>> ListarSaldosAsync(
        string? termo, bool? abaixoMinimo, CancellationToken ct = default)
    {
        var (itens, _) = await _produtoRepository.BuscarPaginadoAsync(termo, true, abaixoMinimo, 0, 500, ct);
        return itens.Select(MapearSaldo).ToList();
    }

    public async Task<IReadOnlyList<AlertaEstoqueDto>> ListarAlertasMinimoAsync(CancellationToken ct = default)
    {
        var (itens, _) = await _produtoRepository.BuscarPaginadoAsync(null, true, true, 0, 500, ct);
        return itens.Select(p => new AlertaEstoqueDto(
            p.Id, p.CodigoInterno, p.Descricao, p.EstoqueAtual, p.EstoqueMinimo)).ToList();
    }

    public Task<MovimentacaoEstoqueDto> RegistrarEntradaAsync(
        MovimentacaoManualRequest request, string operadorId, CancellationToken ct = default) =>
        RegistrarMovimentacaoAsync(request, operadorId, OrigemMovimentacaoEstoque.EntradaManual, request.Quantidade, ct);

    public Task<MovimentacaoEstoqueDto> RegistrarSaidaAsync(
        MovimentacaoManualRequest request, string operadorId, CancellationToken ct = default) =>
        RegistrarMovimentacaoAsync(request, operadorId, OrigemMovimentacaoEstoque.SaidaManual, -request.Quantidade, ct);

    public async Task<MovimentacaoEstoqueDto> RegistrarAjusteAsync(
        AjusteEstoqueRequest request, string operadorId, CancellationToken ct = default)
    {
        ValidarMotivo(request.Motivo);

        var produto = await _produtoRepository.GetByIdAsync(request.ProdutoId, ct)
            ?? throw new DomainException("Produto não encontrado.");

        var delta = EstoqueCalculator.CalcularDeltaAjuste(produto.EstoqueAtual, request.SaldoFinal);
        return await AplicarMovimentacaoAsync(
            produto,
            delta,
            OrigemMovimentacaoEstoque.Ajuste,
            request.Motivo,
            operadorId,
            $"ajuste-{request.ProdutoId}-{Guid.NewGuid():N}",
            null,
            ct);
    }

    public async Task<PaginaListaDto<MovimentacaoEstoqueDto>> HistoricoAsync(
        string? produtoId, int pagina, int tamanho, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var skip = (pagina - 1) * tamanho;

        var total = produtoId is not null
            ? await _movimentacaoRepository.CountAsync(m => m.ProdutoId == produtoId, ct)
            : await _movimentacaoRepository.CountAsync(ct: ct);

        IReadOnlyList<MovimentacaoEstoque> movs = produtoId is not null
            ? await _movimentacaoRepository.ListarPorProdutoAsync(produtoId, skip, tamanho, ct)
            : await _movimentacaoRepository.ListarRecentesAsync(skip, tamanho, ct);

        var produtoCache = new Dictionary<string, string>();
        var result = new List<MovimentacaoEstoqueDto>();

        foreach (var m in movs)
        {
            if (!produtoCache.TryGetValue(m.ProdutoId, out var descricao))
            {
                var p = await _produtoRepository.GetByIdAsync(m.ProdutoId, ct);
                descricao = p?.Descricao ?? "—";
                produtoCache[m.ProdutoId] = descricao;
            }

            result.Add(MapearMovimentacao(m, descricao));
        }

        return new PaginaListaDto<MovimentacaoEstoqueDto>(result, total);
    }

    private async Task<MovimentacaoEstoqueDto> RegistrarMovimentacaoAsync(
        MovimentacaoManualRequest request,
        string operadorId,
        OrigemMovimentacaoEstoque origem,
        decimal delta,
        CancellationToken ct)
    {
        ValidarMotivo(request.Motivo);

        if (request.Quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        var produto = await _produtoRepository.GetByIdAsync(request.ProdutoId, ct)
            ?? throw new DomainException("Produto não encontrado.");

        var idempotencyKey = $"{origem}-{request.ProdutoId}-{operadorId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        return await AplicarMovimentacaoAsync(produto, delta, origem, request.Motivo, operadorId, idempotencyKey, null, ct);
    }

    private async Task<MovimentacaoEstoqueDto> AplicarMovimentacaoAsync(
        Produto produto,
        decimal delta,
        OrigemMovimentacaoEstoque origem,
        string motivo,
        string operadorId,
        string idempotencyKey,
        string? vendaId,
        CancellationToken ct)
    {
        if (await _movimentacaoRepository.ExisteIdempotencyKeyAsync(idempotencyKey, ct))
            throw new DomainException("Movimentação já registrada.");

        var config = await _configuracaoRepository.ObterAsync(ct);
        var (permitido, erro) = EstoqueCalculator.ValidarMovimentacao(
            produto.EstoqueAtual, delta, config.EstoqueNegativoPermitido);

        if (!permitido)
            throw new DomainException(erro!);

        var saldoAnterior = produto.EstoqueAtual;
        var saldoPosterior = saldoAnterior + delta;

        var mov = new MovimentacaoEstoque
        {
            ProdutoId = produto.Id,
            Origem = origem,
            Quantidade = Math.Abs(delta),
            SaldoAnterior = saldoAnterior,
            SaldoPosterior = saldoPosterior,
            Motivo = motivo,
            VendaId = vendaId,
            OperadorId = operadorId,
            IdempotencyKey = idempotencyKey,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        produto.EstoqueAtual = saldoPosterior;
        produto.UpdatedByUserId = operadorId;

        await _movimentacaoRepository.InsertAsync(mov, ct);
        await _produtoRepository.UpdateAsync(produto, ct);

        return MapearMovimentacao(mov, produto.Descricao);
    }

    private static void ValidarMotivo(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Motivo é obrigatório para movimentação manual.");
    }

    private static SaldoProdutoDto MapearSaldo(Produto p) => new(
        p.Id, p.CodigoInterno, p.Descricao, p.EstoqueAtual, p.EstoqueMinimo,
        p.EstoqueAtual < p.EstoqueMinimo);

    private static MovimentacaoEstoqueDto MapearMovimentacao(MovimentacaoEstoque m, string descricao) => new(
        m.Id, m.ProdutoId, descricao, m.Origem, m.Quantidade,
        m.SaldoAnterior, m.SaldoPosterior, m.Motivo, m.CreatedAt);
}
