using System.Diagnostics;
using MeuPdv.Application.Abstractions;
using MeuPdv.Infrastructure.Mongo.Backup;

namespace MeuPdv.Infrastructure.Mongo.Services;

public class MongoBackupExecutor : IBackupExecutor
{
    public bool FerramentasDisponiveis() =>
        MongoToolLocator.Encontrar("mongodump") is not null
        && MongoToolLocator.Encontrar("mongorestore") is not null;

    public async Task<BackupExecucaoResult> ExecutarDumpAsync(
        string connectionString,
        string databaseName,
        string arquivoDestino,
        CancellationToken ct = default)
    {
        var mongodump = MongoToolLocator.Encontrar("mongodump");
        if (mongodump is null)
        {
            return new BackupExecucaoResult(
                false,
                "mongodump não encontrado. Instale o MongoDB Database Tools e adicione ao PATH.",
                0);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(arquivoDestino)!);

        var args = new List<string>
        {
            $"--uri={connectionString}",
            $"--db={databaseName}",
            $"--archive={arquivoDestino}",
            "--gzip"
        };

        var (ok, erro) = await ExecutarProcessoAsync(mongodump, args, ct);
        if (!ok)
            return new BackupExecucaoResult(false, erro, 0);

        if (!File.Exists(arquivoDestino))
            return new BackupExecucaoResult(false, "Arquivo de backup não foi gerado.", 0);

        var tamanho = new FileInfo(arquivoDestino).Length;
        return new BackupExecucaoResult(true, null, tamanho);
    }

    public async Task<BackupExecucaoResult> ExecutarRestoreAsync(
        string connectionString,
        string arquivoOrigem,
        CancellationToken ct = default)
    {
        var mongorestore = MongoToolLocator.Encontrar("mongorestore");
        if (mongorestore is null)
        {
            return new BackupExecucaoResult(
                false,
                "mongorestore não encontrado. Instale o MongoDB Database Tools e adicione ao PATH.",
                0);
        }

        if (!File.Exists(arquivoOrigem))
            return new BackupExecucaoResult(false, "Arquivo de backup não encontrado.", 0);

        var args = new List<string>
        {
            $"--uri={connectionString}",
            $"--archive={arquivoOrigem}",
            "--gzip",
            "--drop"
        };

        var (ok, erro) = await ExecutarProcessoAsync(mongorestore, args, ct);
        return ok
            ? new BackupExecucaoResult(true, null, new FileInfo(arquivoOrigem).Length)
            : new BackupExecucaoResult(false, erro, 0);
    }

    private static async Task<(bool Sucesso, string? Erro)> ExecutarProcessoAsync(
        string executavel,
        IReadOnlyList<string> argumentos,
        CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executavel,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in argumentos)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        var saida = await process.StandardOutput.ReadToEndAsync(ct);
        var erro = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode == 0)
            return (true, null);

        var mensagem = string.IsNullOrWhiteSpace(erro) ? saida : erro;
        return (false, string.IsNullOrWhiteSpace(mensagem)
            ? $"Ferramenta retornou código {process.ExitCode}."
            : mensagem.Trim());
    }
}
