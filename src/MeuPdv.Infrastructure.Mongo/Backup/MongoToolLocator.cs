namespace MeuPdv.Infrastructure.Mongo.Backup;

internal static class MongoToolLocator
{
    public static string? Encontrar(string nomeFerramenta)
    {
        var exe = OperatingSystem.IsWindows() ? $"{nomeFerramenta}.exe" : nomeFerramenta;

        var noPath = EncontrarNoPath(exe);
        if (noPath is not null)
            return noPath;

        if (!OperatingSystem.IsWindows())
            return null;

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidatos = new[]
        {
            Path.Combine(programFiles, "MongoDB", "Tools", "100", "bin", exe),
            Path.Combine(programFiles, "MongoDB", "Tools", "bin", exe)
        };

        foreach (var dir in Directory.EnumerateDirectories(Path.Combine(programFiles, "MongoDB"), "Server*"))
        {
            candidatos = [.. candidatos, Path.Combine(dir, "bin", exe)];
        }

        return candidatos.FirstOrDefault(File.Exists);
    }

    private static string? EncontrarNoPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (var pasta in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var completo = Path.Combine(pasta.Trim(), exe);
            if (File.Exists(completo))
                return completo;
        }

        return null;
    }
}
