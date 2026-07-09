using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Clientes;
using MeuPdv.Application.Vendas;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Exceptions;

namespace MeuPdv.Application.Services;

public class ClienteAppService : IClienteAppService
{
    private readonly IClienteRepository _clienteRepository;

    public ClienteAppService(IClienteRepository clienteRepository) => _clienteRepository = clienteRepository;

    public async Task<ClienteListaResponse> ListarAsync(
        string? termo, bool? ativo, int pagina, int tamanho, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);
        var skip = (pagina - 1) * tamanho;

        var (itens, total) = await _clienteRepository.ListarPaginadoAsync(termo, ativo, skip, tamanho, ct);
        return new ClienteListaResponse(itens.Select(MapearDetalhe).ToList(), total);
    }

    public async Task<ClienteDetalheDto?> ObterPorIdAsync(string id, CancellationToken ct = default)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id, ct);
        return cliente is null ? null : MapearDetalhe(cliente);
    }

    public async Task<ClienteDetalheDto> CriarAsync(
        CriarClienteRequest request, string operadorId, CancellationToken ct = default)
    {
        ValidarNome(request.Nome);
        var cpf = NormalizarOpcional(request.Cpf);
        await ValidarCpfUnicoAsync(cpf, null, ct);

        var cliente = new Cliente
        {
            Nome = request.Nome.Trim(),
            Cpf = cpf,
            Telefone = NormalizarOpcional(request.Telefone),
            Observacoes = NormalizarOpcional(request.Observacoes),
            Ativo = true,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _clienteRepository.InsertAsync(cliente, ct);
        return MapearDetalhe(cliente);
    }

    public async Task<ClienteDetalheDto> AtualizarAsync(
        string id, AtualizarClienteRequest request, string operadorId, CancellationToken ct = default)
    {
        ValidarNome(request.Nome);

        var cliente = await _clienteRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Cliente não encontrado.");

        if (!cliente.Ativo)
            throw new DomainException("Cliente inativo não pode ser alterado.");

        var cpf = NormalizarOpcional(request.Cpf);
        await ValidarCpfUnicoAsync(cpf, id, ct);

        cliente.Nome = request.Nome.Trim();
        cliente.Cpf = cpf;
        cliente.Telefone = NormalizarOpcional(request.Telefone);
        cliente.Observacoes = NormalizarOpcional(request.Observacoes);
        cliente.UpdatedByUserId = operadorId;

        await _clienteRepository.UpdateAsync(cliente, ct);
        return MapearDetalhe(cliente);
    }

    public async Task InativarAsync(string id, string operadorId, CancellationToken ct = default)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Cliente não encontrado.");

        if (!cliente.Ativo)
            return;

        cliente.Ativo = false;
        cliente.UpdatedByUserId = operadorId;
        await _clienteRepository.UpdateAsync(cliente, ct);
    }

    public async Task<IReadOnlyList<ClienteDto>> BuscarAsync(string termo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return [];

        var clientes = await _clienteRepository.BuscarAsync(termo.Trim(), 10, ct);
        return clientes.Select(c => new ClienteDto(c.Id, c.Nome, c.Cpf, c.Telefone)).ToList();
    }

    public async Task<ClienteDto> CriarRapidoAsync(
        CriarClienteRapidoRequest request, string operadorId, CancellationToken ct = default)
    {
        var detalhe = await CriarAsync(
            new CriarClienteRequest(request.Nome, request.Cpf, request.Telefone, null),
            operadorId,
            ct);

        return new ClienteDto(detalhe.Id, detalhe.Nome, detalhe.Cpf, detalhe.Telefone);
    }

    private async Task ValidarCpfUnicoAsync(string? cpf, string? ignorarId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return;

        var existente = await _clienteRepository.GetByCpfAsync(cpf, ct);
        if (existente is not null && existente.Id != ignorarId)
            throw new DomainException("Já existe um cliente com este CPF.");
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente é obrigatório.");
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ClienteDetalheDto MapearDetalhe(Cliente c) => new(
        c.Id, c.Nome, c.Cpf, c.Telefone, c.Observacoes, c.Ativo, c.CreatedAt, c.UpdatedAt);
}
