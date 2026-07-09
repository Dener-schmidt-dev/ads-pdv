using System.Globalization;
using System.Text;
using MeuPdv.Application.Abstractions;
using MeuPdv.Domain.Exceptions;
using QRCoder;

namespace MeuPdv.Infrastructure.Mongo.Services;

/// <summary>
/// Geração de Pix estático (copia e cola + QR Code PNG).
/// Confirmação manual pelo operador. Sem integração bancária.
/// </summary>
public class PixQrService : IPixQrService
{
    private readonly IConfiguracaoEmpresaRepository _empresaRepository;

    public PixQrService(IConfiguracaoEmpresaRepository empresaRepository) =>
        _empresaRepository = empresaRepository;

    public async Task<PixQrResultado> GerarAsync(
        decimal valor, string identificadorVenda, string descricao, CancellationToken ct = default)
    {
        var empresa = await _empresaRepository.ObterAsync(ct);

        if (string.IsNullOrWhiteSpace(empresa.ChavePix))
            throw new DomainException("Chave Pix não configurada. Acesse Configurações.");

        var copiaCola = MontarPayloadEstatico(
            empresa.ChavePix,
            empresa.NomeEstabelecimento,
            valor,
            identificadorVenda,
            descricao);

        var qrBase64 = GerarImagemQrBase64(copiaCola);
        return new PixQrResultado(copiaCola, qrBase64);
    }

    private static string GerarImagemQrBase64(string copiaCola)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(copiaCola, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(8);
        return Convert.ToBase64String(bytes);
    }

    private static string MontarPayloadEstatico(
        string chave, string nomeRecebedor, decimal valor, string txId, string descricao)
    {
        var campos = new List<(string Id, string Valor)>
        {
            ("00", "01"),
            ("26", MontarMerchantAccount(chave)),
            ("52", "0000"),
            ("53", "986"),
            ("54", valor.ToString("F2", CultureInfo.InvariantCulture)),
            ("58", "BR"),
            ("59", Truncar(nomeRecebedor, 25)),
            ("60", "BRASIL"),
            ("62", MontarAdditionalData(txId, descricao))
        };

        var payload = string.Concat(campos.Select(c => FormatarCampo(c.Id, c.Valor)));
        payload += "6304";
        var crc = Crc16(payload);
        return payload + crc;
    }

    private static string MontarMerchantAccount(string chave)
    {
        var gui = FormatarCampo("00", "br.gov.bcb.pix");
        var ch = FormatarCampo("01", chave);
        return gui + ch;
    }

    private static string MontarAdditionalData(string txId, string descricao)
    {
        var tx = FormatarCampo("05", Truncar(txId, 25));
        return tx;
    }

    private static string FormatarCampo(string id, string valor)
    {
        var len = valor.Length.ToString("D2");
        return $"{id}{len}{valor}";
    }

    private static string Truncar(string s, int max) =>
        s.Length <= max ? s : s[..max];

    private static string Crc16(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        ushort crc = 0xFFFF;

        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
            }
        }

        return (crc & 0xFFFF).ToString("X4");
    }
}
