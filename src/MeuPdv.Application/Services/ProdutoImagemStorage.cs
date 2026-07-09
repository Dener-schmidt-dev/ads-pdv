using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Options;
using MeuPdv.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace MeuPdv.Application.Services;

public class ProdutoImagemStorage : IProdutoImagemStorage
{
    private readonly string _diretorio;

    public ProdutoImagemStorage(IOptions<MeuPdvPathsOptions> pathsOptions)
    {
        var dataDir = pathsOptions.Value.DataDirectory;
        _diretorio = string.IsNullOrWhiteSpace(dataDir)
            ? Path.Combine(Path.GetTempPath(), "MeuPDV", "uploads", "produtos")
            : Path.Combine(dataDir, "uploads", "produtos");

        Directory.CreateDirectory(_diretorio);
    }

    public string ObterDiretorio() => _diretorio;

    public string ObterCaminhoArquivo(string produtoId, string extensao) =>
        Path.Combine(_diretorio, $"{produtoId}.{extensao.TrimStart('.').ToLowerInvariant()}");

    public async Task SalvarAsync(string produtoId, string extensao, Stream conteudo, CancellationToken ct = default)
    {
        var caminho = ObterCaminhoArquivo(produtoId, extensao);
        await using var arquivo = File.Create(caminho);
        await conteudo.CopyToAsync(arquivo, ct);
    }

    public void Remover(string produtoId, string? extensao)
    {
        if (string.IsNullOrWhiteSpace(extensao)) return;

        var caminho = ObterCaminhoArquivo(produtoId, extensao);
        if (File.Exists(caminho))
            File.Delete(caminho);
    }

    public bool Existe(string produtoId, string extensao) =>
        File.Exists(ObterCaminhoArquivo(produtoId, extensao));

    public static string NormalizarExtensao(string contentType, string? nomeArquivo)
    {
        var ext = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => Path.GetExtension(nomeArquivo ?? string.Empty).TrimStart('.').ToLowerInvariant()
        };

        if (ext is not ("jpg" or "jpeg" or "png" or "webp"))
            throw new DomainException("Formato de imagem não suportado. Use JPG, PNG ou WebP.");

        return ext == "jpeg" ? "jpg" : ext;
    }

    public static string ObterContentType(string extensao) =>
        extensao.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
