namespace MeuPdv.Application.Abstractions;

public interface IProdutoImagemStorage
{
    string ObterDiretorio();
    string ObterCaminhoArquivo(string produtoId, string extensao);
    Task SalvarAsync(string produtoId, string extensao, Stream conteudo, CancellationToken ct = default);
    void Remover(string produtoId, string? extensao);
    bool Existe(string produtoId, string extensao);
}
