using System.Text;
using System.Text.Json.Serialization;
using MeuPdv.Api.Bootstrap;
using MeuPdv.Api.Authorization;
using MeuPdv.Api.Middleware;
using MeuPdv.Application;
using MeuPdv.Application.Options;
using MeuPdv.Infrastructure.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MeuPdv.Infrastructure.Mongo.Indexing;
using MeuPdv.Infrastructure.Mongo.Seed;
using Serilog;

var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var meuPdvRoot = Path.Combine(programData, "MeuPDV");
Directory.CreateDirectory(Path.Combine(meuPdvRoot, "logs"));
Directory.CreateDirectory(Path.Combine(meuPdvRoot, "backups"));
Directory.CreateDirectory(Path.Combine(meuPdvRoot, "data"));
Directory.CreateDirectory(Path.Combine(meuPdvRoot, "data", "uploads", "produtos"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(meuPdvRoot, "logs", "meupdv-.log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    builder.Configuration["MeuPdvPaths:DataDirectory"] = Path.Combine(meuPdvRoot, "data");
    builder.Configuration["MeuPdvPaths:LogsDirectory"] = Path.Combine(meuPdvRoot, "logs");
    builder.Configuration["MeuPdvPaths:BackupsDirectory"] = Path.Combine(meuPdvRoot, "backups");

    var dataDir = Path.Combine(meuPdvRoot, "data");
    var jwtSecret = JwtSecretInitializer.GarantirSecret(
        dataDir,
        builder.Configuration["Jwt:Secret"],
        builder.Environment.IsDevelopment());
    builder.Configuration["Jwt:Secret"] = jwtSecret;

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<LicenseOptions>(builder.Configuration.GetSection(LicenseOptions.SectionName));

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization(options => options.AddPermissaoPolicies());
    builder.Services.AddSingleton<IAuthorizationHandler, PermissaoAuthorizationHandler>();

    builder.Services.AddApplication();
    builder.Services.AddHttpClient();
    builder.Services.AddInfrastructureMongo(builder.Configuration);
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    });

    if (builder.Environment.IsDevelopment())
        builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    var wwwRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var serveSpa = Directory.Exists(wwwRootPath);

    if (serveSpa)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    if (serveSpa)
        app.MapFallbackToFile("index.html");

    try
    {
        using var scope = app.Services.CreateScope();
        var indexInitializer = scope.ServiceProvider.GetRequiredService<MongoIndexInitializer>();
        await indexInitializer.EnsureIndexesAsync();

        var seed = scope.ServiceProvider.GetRequiredService<DevelopmentSeedService>();
        await seed.SeedAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "MongoDB indisponível na inicialização. A API subirá em modo degradado.");
    }

    Log.Information("MeuPDV API iniciada. MongoDB: {Connection}",
        builder.Configuration.GetSection(MongoDbOptions.SectionName)["ConnectionString"]);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar a API MeuPDV");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
