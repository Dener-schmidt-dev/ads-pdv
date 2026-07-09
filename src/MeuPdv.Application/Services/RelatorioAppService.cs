using System.Globalization;
using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Helpers;
using MeuPdv.Application.Relatorios;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;

namespace MeuPdv.Application.Services;

public class RelatorioAppService : IRelatorioAppService
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IProdutoRepository _produtoRepository;

    public RelatorioAppService(IVendaRepository vendaRepository, IProdutoRepository produtoRepository)
    {
        _vendaRepository = vendaRepository;
        _produtoRepository = produtoRepository;
    }

    public async Task<VendasPorPeriodoDto> VendasPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        ValidarPeriodo(inicio, fim);
        var vendas = await ObterVendasConcluidasAsync(inicio, fim, ct);

        var porForma = vendas
            .SelectMany(v => v.Pagamentos)
            .GroupBy(p => p.Forma)
            .Select(g => new ResumoFormaPagamentoDto(g.Key.ToString(), g.Sum(p => p.Valor)))
            .OrderByDescending(x => x.Valor)
            .ToList();

        var lista = vendas
            .OrderByDescending(v => v.ConcluidaEm)
            .Select(v => new VendaResumoDto(
                v.Id,
                v.Numero,
                v.ConcluidaEm ?? v.CreatedAt,
                v.Subtotal,
                v.Desconto,
                v.Total,
                string.Join(", ", v.Pagamentos.Select(p => $"{LabelForma(p.Forma)} {BrlFormatter.Moeda(p.Valor)}"))))
            .ToList();

        return new VendasPorPeriodoDto(
            inicio,
            fim,
            vendas.Count,
            vendas.Sum(v => v.Subtotal),
            vendas.Sum(v => v.Desconto),
            vendas.Sum(v => v.Total),
            porForma,
            lista);
    }

    public async Task<byte[]> ExportarVendasCsvAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var relatorio = await VendasPorPeriodoAsync(inicio, fim, ct);
        var sb = new StringBuilder();
        sb.AppendLine("numero,data,subtotal,desconto,total,formas_pagamento");

        foreach (var v in relatorio.Vendas)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(v.Numero),
                CsvEscape(v.Data.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                BrlFormatter.Decimal(v.Subtotal),
                BrlFormatter.Decimal(v.Desconto),
                BrlFormatter.Decimal(v.Total),
                CsvEscape(v.FormasPagamento)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<ProdutosMaisVendidosDto> ProdutosMaisVendidosAsync(
        DateTime inicio, DateTime fim, int limite = 20, CancellationToken ct = default)
    {
        ValidarPeriodo(inicio, fim);
        limite = Math.Clamp(limite, 1, 100);

        var vendas = await ObterVendasConcluidasAsync(inicio, fim, ct);
        var agrupado = vendas
            .SelectMany(v => v.Itens.Where(i => !i.Cancelado))
            .GroupBy(i => i.ProdutoId)
            .Select(g =>
            {
                var primeiro = g.First();
                return new ProdutoMaisVendidoDto(
                    g.Key,
                    primeiro.CodigoInterno,
                    primeiro.Descricao,
                    g.Sum(i => i.Quantidade),
                    g.Sum(i => i.Total));
            })
            .OrderByDescending(x => x.Quantidade)
            .ThenByDescending(x => x.ValorTotal)
            .Take(limite)
            .ToList();

        return new ProdutosMaisVendidosDto(inicio, fim, agrupado);
    }

    public async Task<byte[]> ExportarProdutosMaisVendidosCsvAsync(
        DateTime inicio, DateTime fim, int limite = 100, CancellationToken ct = default)
    {
        var relatorio = await ProdutosMaisVendidosAsync(inicio, fim, limite, ct);
        var sb = new StringBuilder();
        sb.AppendLine("codigo_interno,descricao,quantidade,valor_total");

        foreach (var p in relatorio.Itens)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(p.CodigoInterno),
                CsvEscape(p.Descricao),
                BrlFormatter.Decimal(p.Quantidade),
                BrlFormatter.Decimal(p.ValorTotal)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<IReadOnlyList<EstoqueBaixoItemDto>> EstoqueBaixoAsync(CancellationToken ct = default)
    {
        var (itens, _) = await _produtoRepository.BuscarPaginadoAsync(null, true, true, 0, 500, ct);
        return itens
            .OrderBy(p => p.EstoqueAtual - p.EstoqueMinimo)
            .Select(p => new EstoqueBaixoItemDto(
                p.Id, p.CodigoInterno, p.Descricao, p.EstoqueAtual, p.EstoqueMinimo))
            .ToList();
    }

    public async Task<byte[]> ExportarEstoqueBaixoCsvAsync(CancellationToken ct = default)
    {
        var itens = await EstoqueBaixoAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("codigo_interno,descricao,estoque_atual,estoque_minimo");

        foreach (var p in itens)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(p.CodigoInterno),
                CsvEscape(p.Descricao),
                p.EstoqueAtual.ToString("F2", CultureInfo.InvariantCulture),
                p.EstoqueMinimo.ToString("F2", CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<IReadOnlyList<Venda>> ObterVendasConcluidasAsync(
        DateTime inicio, DateTime fim, CancellationToken ct)
    {
        return await _vendaRepository.ListAsync(
            v => v.Status == StatusVenda.Concluida
                 && v.ConcluidaEm != null
                 && v.ConcluidaEm >= inicio
                 && v.ConcluidaEm <= fim,
            skip: 0,
            take: 50_000,
            ct);
    }

    private static void ValidarPeriodo(DateTime inicio, DateTime fim)
    {
        if (inicio > fim)
            throw new DomainException("Data inicial não pode ser maior que a final.");

        if ((fim - inicio).TotalDays > 366)
            throw new DomainException("Período máximo de 366 dias.");
    }

    private static string LabelForma(FormaPagamento forma) => forma switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.CartaoDebito => "Débito",
        FormaPagamento.CartaoCredito => "Crédito",
        FormaPagamento.Pix => "Pix",
        _ => "Outros"
    };

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
