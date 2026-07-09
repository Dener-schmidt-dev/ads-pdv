namespace MeuPdv.Application.Produtos;

public record CategoriaDto(string Id, string Nome, bool Ativo);

public record CriarCategoriaRequest(string Nome);
