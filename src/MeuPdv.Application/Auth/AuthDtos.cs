using MeuPdv.Domain.Enums;

namespace MeuPdv.Application.Auth;

public record LoginRequest(string Login, string Senha);

public record AlterarSenhaRequest(string SenhaAtual, string SenhaNova);

public record LoginResponse(
    string Token,
    DateTime ExpiraEm,
    UsuarioLogadoDto Usuario);

public record UsuarioLogadoDto(
    string Id,
    string Login,
    string Nome,
    PerfilUsuario Perfil,
    IReadOnlyList<Permissao> Permissoes);

public record UsuarioDto(
    string Id,
    string Login,
    string Nome,
    PerfilUsuario Perfil,
    IReadOnlyList<Permissao> PermissoesExtras,
    IReadOnlyList<Permissao> PermissoesEfetivas,
    bool Ativo,
    DateTime? UltimoAcesso,
    DateTime CreatedAt);

public record CriarUsuarioRequest(
    string Login,
    string Nome,
    string Senha,
    PerfilUsuario Perfil,
    IReadOnlyList<Permissao>? PermissoesExtras);

public record AtualizarUsuarioRequest(
    string Nome,
    string? Senha,
    PerfilUsuario Perfil,
    IReadOnlyList<Permissao>? PermissoesExtras,
    bool Ativo);
