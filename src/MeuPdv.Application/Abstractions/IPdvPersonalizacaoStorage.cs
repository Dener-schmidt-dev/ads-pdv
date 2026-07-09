namespace MeuPdv.Application.Abstractions;

public interface IPdvPersonalizacaoStorage
{
    string ObterCaminhoImagemFundo();
    Task SalvarImagemFundoAsync(Stream conteudo, string extensao, CancellationToken ct = default);
    void RemoverImagemFundo();
    bool ExisteImagemFundo();
    (string Caminho, string ContentType)? ObterArquivoImagemFundo();
}
