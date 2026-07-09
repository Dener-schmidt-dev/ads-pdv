namespace MeuPdv.Application.Abstractions;

public interface IPixQrService
{
    Task<PixQrResultado> GerarAsync(decimal valor, string identificadorVenda, string descricao, CancellationToken ct = default);
}

public record PixQrResultado(string CopiaCola, string? QrCodeBase64);
