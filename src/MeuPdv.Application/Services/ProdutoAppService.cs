using System.Globalization;
using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Helpers;
using MeuPdv.Application.Produtos;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Helpers;

namespace MeuPdv.Application.Services;

public class ProdutoAppService : IProdutoAppService
{
    private const long TamanhoMaximoImagemBytes = 2 * 1024 * 1024;

    private readonly IProdutoRepository _produtoRepository;
    private readonly ICategoriaProdutoRepository _categoriaRepository;
    private readonly IProdutoImagemStorage _imagemStorage;

    public ProdutoAppService(
        IProdutoRepository produtoRepository,
        ICategoriaProdutoRepository categoriaRepository,
        IProdutoImagemStorage imagemStorage)
    {
        _produtoRepository = produtoRepository;
        _categoriaRepository = categoriaRepository;
        _imagemStorage = imagemStorage;
    }

    public async Task<ProdutoListaResponse> ListarAsync(
        string? termo, bool? ativo, bool? abaixoMinimo, int pagina, int tamanho, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var skip = (pagina - 1) * tamanho;

        var (itens, total) = await _produtoRepository.BuscarPaginadoAsync(termo, ativo, abaixoMinimo, skip, tamanho, ct);
        var categorias = await CarregarCategoriasMap(ct);
        var dtos = itens.Select(p => Mapear(p, categorias)).ToList();

        return new ProdutoListaResponse(dtos, total, pagina, tamanho);
    }

    public async Task<ProdutoDto?> ObterPorIdAsync(string id, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, ct);
        if (produto is null) return null;

        var categorias = await CarregarCategoriasMap(ct);
        return Mapear(produto, categorias);
    }

    public async Task<ProdutoDto> CriarAsync(CriarProdutoRequest request, string operadorId, CancellationToken ct = default)
    {
        ValidarDescricao(request.Descricao);
        ValidarPreco(request.PrecoVenda);

        var codigoInterno = string.IsNullOrWhiteSpace(request.CodigoInterno)
            ? (await _produtoRepository.ObterProximoCodigoInternoAsync(ct)).ToString("D4")
            : request.CodigoInterno.Trim();

        if (await _produtoRepository.ExistsAsync(x => x.CodigoInterno == codigoInterno, ct))
            throw new DomainException("Código interno já cadastrado.");

        if (!string.IsNullOrWhiteSpace(request.CodigoBarras) &&
            await _produtoRepository.ExistsAsync(x => x.CodigoBarras == request.CodigoBarras.Trim(), ct))
            throw new DomainException("Código de barras já cadastrado.");

        var produto = new Produto
        {
            CodigoInterno = codigoInterno,
            CodigoBarras = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim(),
            Descricao = request.Descricao.Trim(),
            DescricaoNormalizada = TextNormalizer.Normalizar(request.Descricao),
            CategoriaId = request.CategoriaId,
            Marca = string.IsNullOrWhiteSpace(request.Marca) ? null : request.Marca.Trim(),
            Observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim(),
            Unidade = string.IsNullOrWhiteSpace(request.Unidade) ? "UN" : request.Unidade.Trim().ToUpperInvariant(),
            PrecoVenda = request.PrecoVenda,
            Custo = request.Custo,
            EstoqueAtual = request.EstoqueAtual,
            EstoqueMinimo = request.EstoqueMinimo,
            Ativo = true,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _produtoRepository.InsertAsync(produto, ct);
        var categorias = await CarregarCategoriasMap(ct);
        return Mapear(produto, categorias);
    }

    public Task<ProdutoDto> CriarRapidoAsync(
        CriarProdutoRapidoRequest request, string operadorId, CancellationToken ct = default) =>
        CriarAsync(
            new CriarProdutoRequest(
                null,
                request.CodigoBarras,
                request.Descricao,
                null,
                null,
                null,
                "UN",
                request.PrecoVenda,
                null,
                0,
                0),
            operadorId,
            ct);

    public async Task<ProdutoDto> AtualizarAsync(
        string id, AtualizarProdutoRequest request, string operadorId, bool podeAlterarPreco, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        ValidarDescricao(request.Descricao);

        if (!podeAlterarPreco && produto.PrecoVenda != request.PrecoVenda)
            throw new DomainException("Sem permissão para alterar preço.");

        ValidarPreco(request.PrecoVenda);

        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            var barras = request.CodigoBarras.Trim();
            if (await _produtoRepository.ExistsAsync(x => x.CodigoBarras == barras && x.Id != id, ct))
                throw new DomainException("Código de barras já cadastrado.");
            produto.CodigoBarras = barras;
        }
        else
        {
            produto.CodigoBarras = null;
        }

        produto.Descricao = request.Descricao.Trim();
        produto.DescricaoNormalizada = TextNormalizer.Normalizar(request.Descricao);
        produto.CategoriaId = request.CategoriaId;
        produto.Marca = string.IsNullOrWhiteSpace(request.Marca) ? null : request.Marca.Trim();
        produto.Observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim();
        produto.Unidade = string.IsNullOrWhiteSpace(request.Unidade) ? "UN" : request.Unidade.Trim().ToUpperInvariant();
        produto.PrecoVenda = request.PrecoVenda;
        produto.Custo = request.Custo;
        produto.EstoqueMinimo = request.EstoqueMinimo;
        produto.Ativo = request.Ativo;
        produto.UpdatedByUserId = operadorId;

        await _produtoRepository.UpdateAsync(produto, ct);
        var categorias = await CarregarCategoriasMap(ct);
        return Mapear(produto, categorias);
    }

    public async Task InativarAsync(string id, string operadorId, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        produto.Ativo = false;
        produto.UpdatedByUserId = operadorId;
        await _produtoRepository.UpdateAsync(produto, ct);
    }

    public async Task<ImportacaoCsvResultado> ImportarCsvAsync(Stream csv, string operadorId, CancellationToken ct = default)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8);
        var erros = new List<string>();
        var importados = 0;
        var ignorados = 0;
        var linha = 0;

        var header = await reader.ReadLineAsync(ct);
        linha++;

        while (!reader.EndOfStream)
        {
            var text = await reader.ReadLineAsync(ct);
            linha++;
            if (string.IsNullOrWhiteSpace(text)) continue;

            try
            {
                var cols = ParseCsvLine(text);
                if (cols.Count < 3)
                {
                    erros.Add($"Linha {linha}: colunas insuficientes.");
                    ignorados++;
                    continue;
                }

                var barras = cols.Count > 1 ? cols[1].Trim() : null;
                if (!string.IsNullOrWhiteSpace(barras) &&
                    await _produtoRepository.ExistsAsync(x => x.CodigoBarras == barras, ct))
                {
                    ignorados++;
                    continue;
                }

                var descricao = cols[2].Trim();
                var preco = cols.Count > 5 ? ParseDecimal(cols[5]) : 0m;
                var estoque = cols.Count > 7 ? ParseDecimal(cols[7]) : 0m;
                var minimo = cols.Count > 8 ? ParseDecimal(cols[8]) : 0m;

                await CriarAsync(new CriarProdutoRequest(
                    cols[0].Trim(),
                    barras,
                    descricao,
                    null,
                    null,
                    null,
                    cols.Count > 4 ? cols[4].Trim() : "UN",
                    preco,
                    cols.Count > 6 ? ParseDecimalNullable(cols[6]) : null,
                    estoque,
                    minimo), operadorId, ct);

                importados++;
            }
            catch (Exception ex)
            {
                erros.Add($"Linha {linha}: {ex.Message}");
                ignorados++;
            }
        }

        return new ImportacaoCsvResultado(importados, ignorados, erros);
    }

    public async Task<ProdutoDto> SalvarImagemAsync(
        string id, Stream arquivo, long tamanhoBytes, string contentType, string? nomeArquivo, string operadorId, CancellationToken ct = default)
    {
        if (tamanhoBytes > TamanhoMaximoImagemBytes)
            throw new DomainException("A imagem deve ter no máximo 2 MB.");

        var produto = await _produtoRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        var extensao = ProdutoImagemStorage.NormalizarExtensao(contentType, nomeArquivo);
        var extensaoAnterior = produto.ImagemExtensao;

        await _imagemStorage.SalvarAsync(produto.Id, extensao, arquivo, ct);

        if (!string.Equals(extensaoAnterior, extensao, StringComparison.OrdinalIgnoreCase))
            _imagemStorage.Remover(produto.Id, extensaoAnterior);

        produto.ImagemExtensao = extensao;
        produto.UpdatedByUserId = operadorId;
        await _produtoRepository.UpdateAsync(produto, ct);

        var categorias = await CarregarCategoriasMap(ct);
        return Mapear(produto, categorias);
    }

    public async Task<ProdutoDto> RemoverImagemAsync(string id, string operadorId, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Produto não encontrado.");

        _imagemStorage.Remover(produto.Id, produto.ImagemExtensao);
        produto.ImagemExtensao = null;
        produto.UpdatedByUserId = operadorId;
        await _produtoRepository.UpdateAsync(produto, ct);

        var categorias = await CarregarCategoriasMap(ct);
        return Mapear(produto, categorias);
    }

    public async Task<(string Caminho, string ContentType)?> ObterImagemArquivoAsync(string id, CancellationToken ct = default)
    {
        var produto = await _produtoRepository.GetByIdAsync(id, ct);
        if (produto?.ImagemExtensao is null) return null;

        var caminho = _imagemStorage.ObterCaminhoArquivo(produto.Id, produto.ImagemExtensao);
        if (!File.Exists(caminho)) return null;

        return (caminho, ProdutoImagemStorage.ObterContentType(produto.ImagemExtensao));
    }

    public async Task<byte[]> ExportarCsvAsync(CancellationToken ct = default)
    {
        var (itens, _) = await _produtoRepository.BuscarPaginadoAsync(null, null, null, 0, 10000, ct);
        var categorias = await CarregarCategoriasMap(ct);
        var sb = new StringBuilder();
        sb.AppendLine("codigo_interno,codigo_barras,descricao,categoria,unidade,preco_venda,custo,estoque_atual,estoque_minimo,ativo");

        foreach (var p in itens)
        {
            var cat = p.CategoriaId is not null && categorias.TryGetValue(p.CategoriaId, out var nome) ? nome : "";
            sb.AppendLine(string.Join(",",
                CsvEscape(p.CodigoInterno),
                CsvEscape(p.CodigoBarras ?? ""),
                CsvEscape(p.Descricao),
                CsvEscape(cat),
                CsvEscape(p.Unidade),
                BrlFormatter.Decimal(p.PrecoVenda),
                p.Custo.HasValue ? BrlFormatter.Decimal(p.Custo.Value) : "",
                p.EstoqueAtual.ToString("F2", CultureInfo.InvariantCulture),
                p.EstoqueMinimo.ToString("F2", CultureInfo.InvariantCulture),
                p.Ativo ? "1" : "0"));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<Dictionary<string, string>> CarregarCategoriasMap(CancellationToken ct)
    {
        var cats = await _categoriaRepository.ListAsync(take: 500, ct: ct);
        return cats.ToDictionary(c => c.Id, c => c.Nome);
    }

    private static ProdutoDto Mapear(Produto p, Dictionary<string, string> categorias)
    {
        string? catNome = null;
        if (p.CategoriaId is not null)
            categorias.TryGetValue(p.CategoriaId, out catNome);

        return new ProdutoDto(
            p.Id, p.CodigoInterno, p.CodigoBarras, p.Descricao, p.CategoriaId, catNome,
            p.Marca, p.Observacoes,
            p.Unidade, p.PrecoVenda, p.Custo, p.EstoqueAtual, p.EstoqueMinimo,
            p.Ativo, p.EstoqueAtual < p.EstoqueMinimo, p.ImagemExtensao is not null,
            p.CreatedAt, p.UpdatedAt);
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição é obrigatória.");
    }

    private static void ValidarPreco(decimal preco)
    {
        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);

    private static decimal? ParseDecimalNullable(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value);

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        result.Add(current.ToString());
        return result;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
