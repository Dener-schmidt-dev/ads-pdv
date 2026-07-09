using System.Net;
using System.Text.Json;
using MeuPdv.Domain.Exceptions;

namespace MeuPdv.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, message) = exception switch
        {
            DomainException domain => (HttpStatusCode.BadRequest, domain.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Registro não encontrado."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado."),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado. Consulte os logs do sistema.")
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Erro de negócio em {Path}: {Message}", context.Request.Path, message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = JsonSerializer.Serialize(new { erro = message });
        await context.Response.WriteAsync(payload);
    }
}
