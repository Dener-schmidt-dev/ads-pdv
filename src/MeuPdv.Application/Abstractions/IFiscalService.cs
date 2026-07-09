using MeuPdv.Domain.Entities;

namespace MeuPdv.Application.Abstractions;

public interface IFiscalService
{
    bool Habilitado { get; }
    Task<FiscalResultado> ProcessarVendaAsync(Venda venda, CancellationToken ct = default);
}

public record FiscalResultado(bool Sucesso, string? Mensagem);
