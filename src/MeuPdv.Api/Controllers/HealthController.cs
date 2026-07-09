using MeuPdv.Application.Abstractions;
using MeuPdv.Infrastructure.Mongo;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace MeuPdv.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly ILicenseService _licenseService;

    public HealthController(MongoDbContext context, ILicenseService licenseService)
    {
        _context = context;
        _licenseService = licenseService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var mongoOk = false;
        string? mongoErro = null;

        try
        {
            await _context.Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                new MongoDB.Bson.BsonDocument("ping", 1), cancellationToken: ct);
            mongoOk = true;
        }
        catch (Exception ex)
        {
            mongoErro = "MongoDB indisponível. Verifique se o serviço está em execução.";
            _ = ex;
        }

        var licenca = await _licenseService.ValidarAsync(ct);

        return Ok(new
        {
            status = mongoOk ? "ok" : "degraded",
            offline = true,
            mongodb = mongoOk,
            mongoErro,
            licenca = new
            {
                licenca.Valida,
                licenca.EmTolerancia,
                licenca.Validade,
                licenca.Aviso
            },
            versao = "0.1.0"
        });
    }
}
