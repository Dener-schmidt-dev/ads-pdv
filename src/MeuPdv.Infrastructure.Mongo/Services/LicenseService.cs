using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Security;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace MeuPdv.Infrastructure.Mongo.Services;

public class LicenseService : ILicenseService
{
    private readonly MongoDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguracaoSistemaRepository _configuracaoRepository;

    public LicenseService(
        MongoDbContext context,
        IHostEnvironment environment,
        IConfiguracaoSistemaRepository configuracaoRepository)
    {
        _context = context;
        _environment = environment;
        _configuracaoRepository = configuracaoRepository;
    }

    public async Task<LicenseStatus> ValidarAsync(CancellationToken ct = default)
    {
        if (_environment.IsDevelopment())
            return new LicenseStatus(true, false, DateTime.UtcNow.AddYears(10), null);

        var config = await _configuracaoRepository.ObterAsync(ct);
        var col = _context.GetCollection<LicencaLocal>(MongoCollections.LicencaLocal);
        var licenca = await col.Find(FilterDefinition<LicencaLocal>.Empty).FirstOrDefaultAsync(ct);

        if (licenca is null || licenca.ModoDesenvolvimento)
            return new LicenseStatus(false, false, null, "Licença não ativada. Informe uma chave em Configurações.");

        licenca.UltimaValidacao = DateTime.UtcNow;
        await col.ReplaceOneAsync(x => x.Id == licenca.Id, licenca, cancellationToken: ct);

        return AvaliarValidade(licenca.Validade, config.ToleranciaLicencaDias);
    }

    public async Task<LicenseStatus> AtivarLocalAsync(string chave, CancellationToken ct = default)
    {
        if (!LicenseKeyValidator.TryValidar(chave, out var validade, out var erro))
            return new LicenseStatus(false, false, null, erro);

        var col = _context.GetCollection<LicencaLocal>(MongoCollections.LicencaLocal);
        var existente = await col.Find(FilterDefinition<LicencaLocal>.Empty).FirstOrDefaultAsync(ct);

        var licenca = existente ?? new LicencaLocal();
        licenca.Chave = chave.Trim().ToUpperInvariant();
        licenca.Validade = validade;
        licenca.ModoDesenvolvimento = false;
        licenca.AtivadaEm = DateTime.UtcNow;
        licenca.TipoAtivacao = "Local";
        licenca.UltimaValidacao = DateTime.UtcNow;

        if (existente is null)
            await col.InsertOneAsync(licenca, cancellationToken: ct);
        else
            await col.ReplaceOneAsync(x => x.Id == licenca.Id, licenca, cancellationToken: ct);

        var config = await _configuracaoRepository.ObterAsync(ct);
        return AvaliarValidade(validade, config.ToleranciaLicencaDias);
    }

    internal static LicenseStatus AvaliarValidade(DateTime validade, int toleranciaDias)
    {
        var agora = DateTime.UtcNow;
        if (agora <= validade)
            return new LicenseStatus(true, false, validade, null);

        var tolerancia = validade.AddDays(Math.Max(0, toleranciaDias));
        if (agora <= tolerancia)
            return new LicenseStatus(true, true, validade, "Licença expirada — período de tolerância ativo.");

        return new LicenseStatus(false, false, validade, "Licença expirada. Renove a ativação.");
    }
}
