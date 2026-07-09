using MeuPdv.Application.Clientes;
using MeuPdv.Application.Vendas;

namespace MeuPdv.Application.Abstractions;

public interface IClienteAppService
{
    Task<ClienteListaResponse> ListarAsync(
        string? termo, bool? ativo, int pagina, int tamanho, CancellationToken ct = default);

    Task<ClienteDetalheDto?> ObterPorIdAsync(string id, CancellationToken ct = default);

    Task<ClienteDetalheDto> CriarAsync(CriarClienteRequest request, string operadorId, CancellationToken ct = default);

    Task<ClienteDetalheDto> AtualizarAsync(
        string id, AtualizarClienteRequest request, string operadorId, CancellationToken ct = default);

    Task InativarAsync(string id, string operadorId, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteDto>> BuscarAsync(string termo, CancellationToken ct = default);

    Task<ClienteDto> CriarRapidoAsync(CriarClienteRapidoRequest request, string operadorId, CancellationToken ct = default);
}
