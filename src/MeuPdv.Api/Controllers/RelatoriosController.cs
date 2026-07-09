using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Relatorios;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = nameof(Permissao.VisualizarRelatorios))]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioAppService _relatorioAppService;

    public RelatoriosController(IRelatorioAppService relatorioAppService) =>
        _relatorioAppService = relatorioAppService;

    [HttpGet("vendas")]
    public async Task<ActionResult<VendasPorPeriodoDto>> VendasPorPeriodo(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (i, f) = ResolverPeriodo(inicio, fim);
        return Ok(await _relatorioAppService.VendasPorPeriodoAsync(i, f, ct));
    }

    [HttpGet("vendas/exportar-csv")]
    public async Task<IActionResult> ExportarVendasCsv(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (i, f) = ResolverPeriodo(inicio, fim);
        var bytes = await _relatorioAppService.ExportarVendasCsvAsync(i, f, ct);
        return File(bytes, "text/csv", $"vendas-{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("produtos-mais-vendidos")]
    public async Task<ActionResult<ProdutosMaisVendidosDto>> ProdutosMaisVendidos(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] int limite = 20,
        CancellationToken ct = default)
    {
        var (i, f) = ResolverPeriodo(inicio, fim);
        return Ok(await _relatorioAppService.ProdutosMaisVendidosAsync(i, f, limite, ct));
    }

    [HttpGet("produtos-mais-vendidos/exportar-csv")]
    public async Task<IActionResult> ExportarProdutosMaisVendidosCsv(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] int limite = 100,
        CancellationToken ct = default)
    {
        var (i, f) = ResolverPeriodo(inicio, fim);
        var bytes = await _relatorioAppService.ExportarProdutosMaisVendidosCsvAsync(i, f, limite, ct);
        return File(bytes, "text/csv", $"produtos-mais-vendidos-{DateTime.Now:yyyyMMdd}.csv");
    }

    [HttpGet("estoque-baixo")]
    public async Task<ActionResult<IReadOnlyList<EstoqueBaixoItemDto>>> EstoqueBaixo(CancellationToken ct) =>
        Ok(await _relatorioAppService.EstoqueBaixoAsync(ct));

    [HttpGet("estoque-baixo/exportar-csv")]
    public async Task<IActionResult> ExportarEstoqueBaixoCsv(CancellationToken ct)
    {
        var bytes = await _relatorioAppService.ExportarEstoqueBaixoCsvAsync(ct);
        return File(bytes, "text/csv", $"estoque-baixo-{DateTime.Now:yyyyMMdd}.csv");
    }

    private static (DateTime Inicio, DateTime Fim) ResolverPeriodo(DateOnly? inicio, DateOnly? fim)
    {
        var fimDate = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioDate = inicio ?? fimDate.AddDays(-30);

        if (inicioDate > fimDate)
            throw new DomainException("Data inicial não pode ser maior que a final.");

        var inicioUtc = inicioDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = fimDate.ToDateTime(new TimeOnly(23, 59, 59, 999), DateTimeKind.Utc);
        return (inicioUtc, fimUtc);
    }
}
