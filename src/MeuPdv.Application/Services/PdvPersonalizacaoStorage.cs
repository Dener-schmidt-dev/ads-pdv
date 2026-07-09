using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Options;
using MeuPdv.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace MeuPdv.Application.Services;

public class PdvPersonalizacaoStorage : IPdvPersonalizacaoStorage
{
    private const string NomeArquivo = "pdv-fundo";
    private readonly string _diretorio;

    public PdvPersonalizacaoStorage(IOptions<MeuPdvPathsOptions> pathsOptions)
    {
        var dataDir = pathsOptions.Value.DataDirectory;
        _diretorio = string.IsNullOrWhiteSpace(dataDir)
            ? Path.Combine(Path.GetTempPath(), "MeuPDV", "uploads", "pdv")
            : Path.Combine(dataDir, "uploads", "pdv");

        Directory.CreateDirectory(_diretorio);
    }

    public string ObterCaminhoImagemFundo() => _diretorio;

    public async Task SalvarImagemFundoAsync(Stream conteudo, string extensao, CancellationToken ct = default)
    {
        RemoverImagemFundo();
        var caminho = Path.Combine(_diretorio, $"{NomeArquivo}.{extensao}");
        await using var arquivo = File.Create(caminho);
        await conteudo.CopyToAsync(arquivo, ct);
    }

    public void RemoverImagemFundo()
    {
        foreach (var ext in new[] { "jpg", "png", "webp" })
        {
            var caminho = Path.Combine(_diretorio, $"{NomeArquivo}.{ext}");
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
    }

    public bool ExisteImagemFundo() => ObterArquivoImagemFundo() is not null;

    public (string Caminho, string ContentType)? ObterArquivoImagemFundo()
    {
        foreach (var ext in new[] { "jpg", "png", "webp" })
        {
            var caminho = Path.Combine(_diretorio, $"{NomeArquivo}.{ext}");
            if (File.Exists(caminho))
                return (caminho, ObterContentType(ext));
        }

        return null;
    }

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

    private static string ObterContentType(string extensao) =>
        extensao.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
