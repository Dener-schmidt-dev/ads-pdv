using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Produtos;
using MeuPdv.Api.Extensions;
using MeuPdv.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoAppService _produtoAppService;

    public ProdutosController(IProdutoAppService produtoAppService) => _produtoAppService = produtoAppService;

    [HttpGet]
    public async Task<ActionResult<ProdutoListaResponse>> Listar(
        [FromQuery] string? termo,
        [FromQuery] bool? ativo,
        [FromQuery] bool? abaixoMinimo,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 50,
        CancellationToken ct = default) =>
        Ok(await _produtoAppService.ListarAsync(termo, ativo, abaixoMinimo, pagina, tamanho, ct));

    [HttpGet("{id}/imagem")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterImagem(string id, CancellationToken ct)
    {
        var arquivo = await _produtoAppService.ObterImagemArquivoAsync(id, ct);
        if (arquivo is null) return NotFound();

        return PhysicalFile(arquivo.Value.Caminho, arquivo.Value.ContentType);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProdutoDto>> Obter(string id, CancellationToken ct)
    {
        var produto = await _produtoAppService.ObterPorIdAsync(id, ct);
        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<ActionResult<ProdutoDto>> Criar([FromBody] CriarProdutoRequest request, CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        var produto = await _produtoAppService.CriarAsync(request, operadorId, ct);
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto);
    }

    [HttpPost("rapido")]
    public async Task<ActionResult<ProdutoDto>> CriarRapido(
        [FromBody] CriarProdutoRapidoRequest request,
        CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        var produto = await _produtoAppService.CriarRapidoAsync(request, operadorId, ct);
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, produto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<ActionResult<ProdutoDto>> Atualizar(
        string id, [FromBody] AtualizarProdutoRequest request, CancellationToken ct)
    {
        var operadorId = User.ObterUsuarioId();
        var podeAlterarPreco = User.HasClaim("perm", Permissao.AlterarPreco.ToString());

        var produto = await _produtoAppService.AtualizarAsync(id, request, operadorId, podeAlterarPreco, ct);
        return Ok(produto);
    }

    [HttpPost("{id}/inativar")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<IActionResult> Inativar(string id, CancellationToken ct)
    {
        await _produtoAppService.InativarAsync(id, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    [HttpPost("{id}/imagem")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<ProdutoDto>> SalvarImagem(string id, IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo vazio." });

        await using var stream = arquivo.OpenReadStream();
        var produto = await _produtoAppService.SalvarImagemAsync(
            id, stream, arquivo.Length, arquivo.ContentType, arquivo.FileName, User.ObterUsuarioId(), ct);
        return Ok(produto);
    }

    [HttpDelete("{id}/imagem")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<ActionResult<ProdutoDto>> RemoverImagem(string id, CancellationToken ct) =>
        Ok(await _produtoAppService.RemoverImagemAsync(id, User.ObterUsuarioId(), ct));

    [HttpPost("importar-csv")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImportacaoCsvResultado>> ImportarCsv(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo vazio." });

        await using var stream = arquivo.OpenReadStream();
        var resultado = await _produtoAppService.ImportarCsvAsync(stream, User.ObterUsuarioId(), ct);
        return Ok(resultado);
    }

    [HttpGet("exportar-csv")]
    [Authorize(Policy = nameof(Permissao.CadastrarProduto))]
    public async Task<IActionResult> ExportarCsv(CancellationToken ct)
    {
        var bytes = await _produtoAppService.ExportarCsvAsync(ct);
        return File(bytes, "text/csv", $"produtos-{DateTime.Now:yyyyMMdd}.csv");
    }
}
