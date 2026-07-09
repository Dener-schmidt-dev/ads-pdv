using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Application.Auth;
using MeuPdv.Application.Options;
using MeuPdv.Domain.Exceptions;
using MeuPdv.Domain.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MeuPdv.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByLoginAsync(request.Login.Trim().ToLowerInvariant(), ct);
        if (usuario is null || !usuario.Ativo)
            return null;

        if (!_passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            return null;

        usuario.UltimoAcesso = DateTime.UtcNow;
        await _usuarioRepository.UpdateAsync(usuario, ct);

        var permissoes = PermissaoPorPerfil.ObterPermissoes(usuario.Perfil, usuario.PermissoesExtras);
        var expiraEm = DateTime.UtcNow.AddHours(_jwtOptions.ExpiracaoHoras);
        var token = GerarToken(usuario, permissoes, expiraEm);

        return new LoginResponse(
            token,
            expiraEm,
            MapearLogado(usuario, permissoes));
    }

    public async Task AlterarSenhaAsync(string usuarioId, AlterarSenhaRequest request, CancellationToken ct = default)
    {
        ValidarSenha(request.SenhaNova);

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, ct)
            ?? throw new DomainException("Usuário não encontrado.");

        if (!_passwordHasher.Verificar(request.SenhaAtual, usuario.SenhaHash))
            throw new DomainException("Senha atual incorreta.");

        usuario.SenhaHash = _passwordHasher.Hash(request.SenhaNova);
        usuario.UpdatedByUserId = usuarioId;
        await _usuarioRepository.UpdateAsync(usuario, ct);
    }

    public async Task<UsuarioLogadoDto?> ObterUsuarioLogadoAsync(string usuarioId, CancellationToken ct = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, ct);
        if (usuario is null || !usuario.Ativo)
            return null;

        var permissoes = PermissaoPorPerfil.ObterPermissoes(usuario.Perfil, usuario.PermissoesExtras);
        return MapearLogado(usuario, permissoes);
    }

    private string GerarToken(Domain.Entities.Usuario usuario, IReadOnlyList<Domain.Enums.Permissao> permissoes, DateTime expiraEm)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.UniqueName, usuario.Login),
            new(ClaimTypes.Name, usuario.Nome),
            new("perfil", usuario.Perfil.ToString())
        };

        foreach (var permissao in permissoes)
            claims.Add(new Claim("perm", permissao.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6)
            throw new DomainException("Senha deve ter ao menos 6 caracteres.");
    }

    private static UsuarioLogadoDto MapearLogado(
        Domain.Entities.Usuario usuario,
        IReadOnlyList<Domain.Enums.Permissao> permissoes) =>
        new(usuario.Id, usuario.Login, usuario.Nome, usuario.Perfil, permissoes);
}
