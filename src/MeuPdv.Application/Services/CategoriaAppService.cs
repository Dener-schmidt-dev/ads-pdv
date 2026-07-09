using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Produtos;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Exceptions;

namespace MeuPdv.Application.Services;

public class CategoriaAppService : ICategoriaAppService
{
    private readonly ICategoriaProdutoRepository _repository;

    public CategoriaAppService(ICategoriaProdutoRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(CancellationToken ct = default)
    {
        var itens = await _repository.ListAsync(x => x.Ativo, take: 200, ct: ct);
        return itens.Select(c => new CategoriaDto(c.Id, c.Nome, c.Ativo)).ToList();
    }

    public async Task<CategoriaDto> CriarAsync(CriarCategoriaRequest request, string operadorId, CancellationToken ct = default)
    {
        var nome = request.Nome.Trim();
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome da categoria é obrigatório.");

        if (await _repository.GetByNomeAsync(nome, ct) is not null)
            throw new DomainException("Categoria já cadastrada.");

        var categoria = new CategoriaProduto
        {
            Nome = nome,
            Ativo = true,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _repository.InsertAsync(categoria, ct);
        return new CategoriaDto(categoria.Id, categoria.Nome, categoria.Ativo);
    }
}
