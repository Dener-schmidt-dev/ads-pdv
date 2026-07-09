using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Helpers;
using MeuPdv.Application.Vendas;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Rules;

namespace MeuPdv.Application.Services;

public class VendaAppService : IVendaAppService
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IVendaRepository _vendaRepository;
    private readonly ISessaoCaixaRepository _sessaoCaixaRepository;
    private readonly IMovimentacaoCaixaRepository _movimentacaoCaixaRepository;
    private readonly IMovimentacaoEstoqueRepository _movimentacaoEstoqueRepository;
    private readonly IConfiguracaoSistemaRepository _configuracaoSistemaRepository;
    private readonly IConfiguracaoEmpresaRepository _configuracaoEmpresaRepository;
    private readonly IFiscalService _fiscalService;
    private readonly IPixQrService _pixQrService;
    private readonly ILicenseAppService _licenseAppService;

    public VendaAppService(
        IProdutoRepository produtoRepository,
        IVendaRepository vendaRepository,
        ISessaoCaixaRepository sessaoCaixaRepository,
        IMovimentacaoCaixaRepository movimentacaoCaixaRepository,
        IMovimentacaoEstoqueRepository movimentacaoEstoqueRepository,
        IConfiguracaoSistemaRepository configuracaoSistemaRepository,
        IConfiguracaoEmpresaRepository configuracaoEmpresaRepository,
        IFiscalService fiscalService,
        IPixQrService pixQrService,
        ILicenseAppService licenseAppService)
    {
        _produtoRepository = produtoRepository;
        _vendaRepository = vendaRepository;
        _sessaoCaixaRepository = sessaoCaixaRepository;
        _movimentacaoCaixaRepository = movimentacaoCaixaRepository;
        _movimentacaoEstoqueRepository = movimentacaoEstoqueRepository;
        _configuracaoSistemaRepository = configuracaoSistemaRepository;
        _configuracaoEmpresaRepository = configuracaoEmpresaRepository;
        _fiscalService = fiscalService;
        _pixQrService = pixQrService;
        _licenseAppService = licenseAppService;
    }

    public async Task<ProdutoLeituraDto?> LerProdutoAsync(string codigo, CancellationToken ct = default)
    {
        var c = codigo.Trim();
        var produto = await _produtoRepository.GetByCodigoBarrasAsync(c, ct)
            ?? await _produtoRepository.GetByCodigoInternoAsync(c, ct);

        return produto is null || !produto.Ativo ? null : MapearLeitura(produto);
    }

    public async Task<IReadOnlyList<ProdutoLeituraDto>> BuscarProdutosAsync(string termo, int limite = 10, CancellationToken ct = default)
    {
        var produtos = await _produtoRepository.BuscarPorDescricaoAsync(termo, limite, ct);
        return produtos.Select(MapearLeitura).ToList();
    }

    public async Task<FinalizarVendaResponse> FinalizarAsync(
        FinalizarVendaRequest request,
        string operadorId,
        bool podeConcederDesconto,
        CancellationToken ct = default)
    {
        await _licenseAppService.ExigirLicencaValidaParaOperacaoAsync(ct);

        if (request.Itens.Count == 0)
            throw new DomainException("Adicione ao menos um produto à venda.");

        if (request.Desconto > 0 && !podeConcederDesconto)
            throw new DomainException("Sem permissão para conceder desconto.");

        var caixa = string.IsNullOrWhiteSpace(request.CaixaIdentificador)
            ? "CAIXA-01"
            : request.CaixaIdentificador.Trim().ToUpperInvariant();

        var sessao = await _sessaoCaixaRepository.ObterAbertaAsync(caixa, ct)
            ?? throw new DomainException("Caixa fechado. Abra o caixa antes de vender.");

        var config = await _configuracaoSistemaRepository.ObterAsync(ct);
        var itensVenda = new List<VendaItem>();
        var produtosAtualizados = new List<Produto>();

        foreach (var itemReq in request.Itens)
        {
            var produto = await _produtoRepository.GetByIdAsync(itemReq.ProdutoId, ct)
                ?? throw new DomainException($"Produto não encontrado: {itemReq.ProdutoId}");

            if (!produto.Ativo)
                throw new DomainException($"Produto inativo: {produto.Descricao}");

            if (itemReq.Quantidade <= 0)
                throw new DomainException("Quantidade inválida.");

            var (ok, erro) = EstoqueCalculator.ValidarMovimentacao(
                produto.EstoqueAtual, -itemReq.Quantidade, config.EstoqueNegativoPermitido);

            if (!ok)
                throw new DomainException($"Estoque insuficiente para {produto.Descricao}. {erro}");

            itensVenda.Add(new VendaItem
            {
                ProdutoId = produto.Id,
                CodigoInterno = produto.CodigoInterno,
                CodigoBarras = produto.CodigoBarras,
                Descricao = produto.Descricao,
                Quantidade = itemReq.Quantidade,
                PrecoUnitario = produto.PrecoVenda
            });

            produtosAtualizados.Add(produto);
        }

        var venda = new Venda
        {
            SessaoCaixaId = sessao.Id,
            OperadorId = operadorId,
            ClienteId = request.ClienteId,
            Status = StatusVenda.Concluida,
            Itens = itensVenda,
            Desconto = request.Desconto,
            MotivoDesconto = request.MotivoDesconto,
            ConcluidaEm = DateTime.UtcNow,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        VendaCalculator.Recalcular(venda);

        var (descontoOk, exigeAuth, descontoErro) = DescontoValidator.Validar(
            venda.Subtotal, venda.Desconto, config.LimiteDescontoOperadorPercentual);

        if (!descontoOk)
            throw new DomainException(descontoErro!);

        if (exigeAuth && string.IsNullOrWhiteSpace(request.MotivoDesconto))
            throw new DomainException("Desconto acima do limite exige motivo.");

        var pagamentos = request.Pagamentos.Select(p => new Pagamento
        {
            Forma = p.Forma,
            Valor = p.Valor,
            ValorRecebido = p.Forma == FormaPagamento.Dinheiro ? p.ValorRecebido : p.Valor,
            Troco = p.Forma == FormaPagamento.Dinheiro
                ? PagamentoValidator.CalcularTroco(p.ValorRecebido, p.Valor)
                : 0m
        }).ToList();

        var (pagOk, pagErro) = PagamentoValidator.Validar(venda.Total, pagamentos);
        if (!pagOk)
            throw new DomainException(pagErro!);

        venda.Pagamentos = pagamentos;
        venda.Numero = (await _vendaRepository.ObterProximoNumeroAsync(ct)).ToString("D6");

        await _vendaRepository.InsertAsync(venda, ct);

        foreach (var itemReq in request.Itens)
        {
            var produto = produtosAtualizados.First(p => p.Id == itemReq.ProdutoId);
            var qtd = itemReq.Quantidade;
            var saldoAnterior = produto.EstoqueAtual;
            produto.EstoqueAtual -= qtd;
            produto.UpdatedByUserId = operadorId;

            var idempotencyKey = $"venda-{venda.Id}-{produto.Id}";
            if (!await _movimentacaoEstoqueRepository.ExisteIdempotencyKeyAsync(idempotencyKey, ct))
            {
                await _movimentacaoEstoqueRepository.InsertAsync(new MovimentacaoEstoque
                {
                    ProdutoId = produto.Id,
                    Origem = OrigemMovimentacaoEstoque.Venda,
                    Quantidade = qtd,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = produto.EstoqueAtual,
                    VendaId = venda.Id,
                    OperadorId = operadorId,
                    IdempotencyKey = idempotencyKey,
                    CreatedByUserId = operadorId,
                    UpdatedByUserId = operadorId
                }, ct);
            }

            await _produtoRepository.UpdateAsync(produto, ct);
        }

        foreach (var pag in pagamentos)
        {
            await _movimentacaoCaixaRepository.InsertAsync(new MovimentacaoCaixa
            {
                SessaoCaixaId = sessao.Id,
                Tipo = TipoMovimentacaoCaixa.Venda,
                FormaPagamento = pag.Forma,
                Valor = pag.Valor,
                VendaId = venda.Id,
                OperadorId = operadorId,
                CreatedByUserId = operadorId,
                UpdatedByUserId = operadorId
            }, ct);
        }

        await _fiscalService.ProcessarVendaAsync(venda, ct);
        await _vendaRepository.UpdateAsync(venda, ct);

        var troco = pagamentos.Where(p => p.Forma == FormaPagamento.Dinheiro).Sum(p => p.Troco);
        var empresa = await _configuracaoEmpresaRepository.ObterAsync(ct);
        var comprovante = GerarComprovante(empresa, venda, troco);

        PixQrResultadoDto? pix = null;
        if (pagamentos.Any(p => p.Forma == FormaPagamento.Pix))
        {
            var pixData = await _pixQrService.GerarAsync(venda.Total, venda.Numero, $"Venda {venda.Numero}", ct);
            pix = new PixQrResultadoDto(pixData.CopiaCola, pixData.QrCodeBase64);
        }

        return new FinalizarVendaResponse(
            venda.Id, venda.Numero, venda.Total, troco, comprovante, pix, config.ImpressaoAutomatica);
    }

    public async Task<VendaListaResponse> ListarRecentesAsync(
        string? termo, int pagina = 1, int tamanho = 20, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var skip = (pagina - 1) * tamanho;

        var (vendas, total) = await _vendaRepository.ListarConcluidasPaginadoAsync(termo, skip, tamanho, ct);
        var itens = vendas
            .Select(v => new VendaListaDto(
                v.Id,
                v.Numero,
                v.ConcluidaEm ?? v.CreatedAt,
                v.Total,
                v.Status.ToString(),
                string.Join(", ", v.Pagamentos.Select(p => $"{p.Forma} {p.Valor:F2}"))))
            .ToList();

        return new VendaListaResponse(itens, total);
    }

    public async Task<VendaDetalheDto?> ObterAsync(string id, CancellationToken ct = default)
    {
        var venda = await _vendaRepository.GetByIdAsync(id, ct);
        return venda is null ? null : MapearDetalhe(venda);
    }

    public async Task<CancelarVendaResponse> CancelarAsync(
        string id, CancelarVendaRequest request, string operadorId, CancellationToken ct = default)
    {
        await _licenseAppService.ExigirLicencaValidaParaOperacaoAsync(ct);

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new DomainException("Motivo do cancelamento é obrigatório.");

        var venda = await _vendaRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Venda não encontrada.");

        if (venda.Status != StatusVenda.Concluida)
            throw new DomainException("Somente vendas concluídas podem ser canceladas.");

        var sessao = await _sessaoCaixaRepository.GetByIdAsync(venda.SessaoCaixaId, ct)
            ?? throw new DomainException("Sessão de caixa da venda não encontrada.");

        if (sessao.Status != StatusSessaoCaixa.Aberta)
            throw new DomainException("O caixa da venda já foi fechado. Não é possível estornar.");

        foreach (var item in venda.Itens.Where(i => !i.Cancelado))
        {
            var produto = await _produtoRepository.GetByIdAsync(item.ProdutoId, ct)
                ?? throw new DomainException($"Produto não encontrado: {item.Descricao}");

            var saldoAnterior = produto.EstoqueAtual;
            produto.EstoqueAtual += item.Quantidade;
            produto.UpdatedByUserId = operadorId;

            var idempotencyKey = $"cancelamento-{venda.Id}-{produto.Id}";
            if (!await _movimentacaoEstoqueRepository.ExisteIdempotencyKeyAsync(idempotencyKey, ct))
            {
                await _movimentacaoEstoqueRepository.InsertAsync(new MovimentacaoEstoque
                {
                    ProdutoId = produto.Id,
                    Origem = OrigemMovimentacaoEstoque.Cancelamento,
                    Quantidade = item.Quantidade,
                    SaldoAnterior = saldoAnterior,
                    SaldoPosterior = produto.EstoqueAtual,
                    VendaId = venda.Id,
                    OperadorId = operadorId,
                    Motivo = request.Motivo.Trim(),
                    IdempotencyKey = idempotencyKey,
                    CreatedByUserId = operadorId,
                    UpdatedByUserId = operadorId
                }, ct);
            }

            await _produtoRepository.UpdateAsync(produto, ct);
        }

        foreach (var pag in venda.Pagamentos)
        {
            await _movimentacaoCaixaRepository.InsertAsync(new MovimentacaoCaixa
            {
                SessaoCaixaId = sessao.Id,
                Tipo = TipoMovimentacaoCaixa.CancelamentoVenda,
                FormaPagamento = pag.Forma,
                Valor = pag.Valor,
                VendaId = venda.Id,
                Motivo = request.Motivo.Trim(),
                OperadorId = operadorId,
                CreatedByUserId = operadorId,
                UpdatedByUserId = operadorId
            }, ct);
        }

        venda.Status = StatusVenda.Cancelada;
        venda.CanceladaEm = DateTime.UtcNow;
        venda.MotivoCancelamento = request.Motivo.Trim();
        venda.UpdatedByUserId = operadorId;
        await _vendaRepository.UpdateAsync(venda, ct);

        return new CancelarVendaResponse(venda.Id, venda.Numero, venda.Total);
    }

    private static VendaDetalheDto MapearDetalhe(Venda v) => new(
        v.Id,
        v.Numero,
        v.ConcluidaEm ?? v.CreatedAt,
        v.Subtotal,
        v.Desconto,
        v.Total,
        v.Status.ToString(),
        v.MotivoCancelamento,
        v.Itens.Where(i => !i.Cancelado).Select(i => new VendaItemDetalheDto(
            i.Descricao, i.Quantidade, i.PrecoUnitario, i.Total)).ToList(),
        v.Pagamentos.Select(p => new PagamentoDetalheDto(p.Forma.ToString(), p.Valor)).ToList());

    private static ProdutoLeituraDto MapearLeitura(Produto p) => new(
        p.Id, p.CodigoInterno, p.CodigoBarras, p.Descricao, p.PrecoVenda, p.EstoqueAtual, p.Unidade,
        p.ImagemExtensao is not null);

    private static string GerarComprovante(ConfiguracaoEmpresa empresa, Venda venda, decimal troco)
    {
        var sb = new StringBuilder();
        sb.AppendLine(empresa.NomeEstabelecimento);
        sb.AppendLine($"Venda: {venda.Numero}");
        sb.AppendLine($"Data: {venda.ConcluidaEm:dd/MM/yyyy HH:mm}");
        sb.AppendLine(new string('-', 32));

        foreach (var item in venda.Itens.Where(i => !i.Cancelado))
        {
            sb.AppendLine($"{item.Quantidade:0.##} x {item.Descricao}");
            sb.AppendLine($"  {BrlFormatter.Moeda(item.Total),10}");
        }

        sb.AppendLine(new string('-', 32));
        sb.AppendLine($"Subtotal: {BrlFormatter.Moeda(venda.Subtotal),10}");
        if (venda.Desconto > 0)
            sb.AppendLine($"Desconto: {BrlFormatter.Moeda(venda.Desconto),10}");
        sb.AppendLine($"TOTAL:    {BrlFormatter.Moeda(venda.Total),10}");

        foreach (var pag in venda.Pagamentos)
            sb.AppendLine($"{pag.Forma}: {BrlFormatter.Moeda(pag.Valor)}");

        if (troco > 0)
            sb.AppendLine($"Troco: {BrlFormatter.Moeda(troco)}");

        sb.AppendLine();
        sb.AppendLine("Obrigado pela preferência!");
        return sb.ToString();
    }
}
