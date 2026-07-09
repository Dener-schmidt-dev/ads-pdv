using MeuPdv.Application.Abstractions;

namespace MeuPdv.Infrastructure.Mongo.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
