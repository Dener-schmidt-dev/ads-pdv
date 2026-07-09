using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Auth;
using MeuPdv.Domain.Entities;
using MeuPdv.Domain.Enums;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Security;

namespace MeuPdv.Application.Services;

public class UsuarioAppService : IUsuarioAppService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UsuarioAppService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct = default)
    {
        var usuarios = await _usuarioRepository.ListAsync(take: 200, ct: ct);
        return usuarios.Select(Mapear).ToList();
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(string id, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct);
        return usuario is null ? null : Mapear(usuario);
    }

    public async Task<UsuarioDto> CriarAsync(CriarUsuarioRequest request, string operadorId, CancellationToken ct = default)
    {
        ValidarSenha(request.Senha);

        var login = request.Login.Trim().ToLowerInvariant();
        if (await _usuarioRepository.ExistsAsync(x => x.Login == login, ct))
            throw new DomainException("Já existe um usuário com este login.");

        var usuario = new Usuario
        {
            Login = login,
            Nome = request.Nome.Trim(),
            SenhaHash = _passwordHasher.Hash(request.Senha),
            Perfil = request.Perfil,
            PermissoesExtras = request.PermissoesExtras?.ToList() ?? [],
            Ativo = true,
            CreatedByUserId = operadorId,
            UpdatedByUserId = operadorId
        };

        await _usuarioRepository.InsertAsync(usuario, ct);
        return Mapear(usuario);
    }

    public async Task<UsuarioDto> AtualizarAsync(
        string id,
        AtualizarUsuarioRequest request,
        string operadorId,
        CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Usuário não encontrado.");

        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            ValidarSenha(request.Senha);
            usuario.SenhaHash = _passwordHasher.Hash(request.Senha);
        }

        usuario.Nome = request.Nome.Trim();
        usuario.Perfil = request.Perfil;
        usuario.PermissoesExtras = request.PermissoesExtras?.ToList() ?? [];
        usuario.Ativo = request.Ativo;
        usuario.UpdatedByUserId = operadorId;

        await _usuarioRepository.UpdateAsync(usuario, ct);
        return Mapear(usuario);
    }

    public async Task InativarAsync(string id, string operadorId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException("Usuário não encontrado.");

        if (usuario.Login == "admin")
            throw new DomainException("Não é permitido inativar o usuário administrador padrão.");

        usuario.Ativo = false;
        usuario.UpdatedByUserId = operadorId;
        await _usuarioRepository.UpdateAsync(usuario, ct);
    }

    private static void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6)
            throw new DomainException("A senha deve ter no mínimo 6 caracteres.");
    }

    private static UsuarioDto Mapear(Usuario usuario)
    {
        var efetivas = PermissaoPorPerfil.ObterPermissoes(usuario.Perfil, usuario.PermissoesExtras);
        return new UsuarioDto(
            usuario.Id,
            usuario.Login,
            usuario.Nome,
            usuario.Perfil,
            usuario.PermissoesExtras,
            efetivas,
            usuario.Ativo,
            usuario.UltimoAcesso,
            usuario.CreatedAt);
    }
}
