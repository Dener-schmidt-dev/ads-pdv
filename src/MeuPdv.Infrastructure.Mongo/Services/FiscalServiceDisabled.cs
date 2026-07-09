using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;

namespace MeuPdv.Infrastructure.Mongo.Services;

public class FiscalServiceDisabled : IFiscalService
{
    public bool Habilitado => false;

    public Task<FiscalResultado> ProcessarVendaAsync(Venda venda, CancellationToken ct = default)
    {
        venda.StatusFiscal = StatusFiscal.NaoAplicavel;
        return Task.FromResult(new FiscalResultado(true, "Módulo fiscal desabilitado."));
    }
}
