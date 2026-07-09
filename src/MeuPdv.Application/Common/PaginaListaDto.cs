namespace MeuPdv.Application.Common;

public record PaginaListaDto<T>(IReadOnlyList<T> Itens, long Total);
