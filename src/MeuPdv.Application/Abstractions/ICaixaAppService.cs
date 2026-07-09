using MeuPdv.Application.Caixa;
using MeuPdv.Application.Common;

namespace MeuPdv.Application.Abstractions;

public interface ICaixaAppService
{
    Task<SessaoCaixaDto?> ObterSessaoAbertaAsync(string caixaIdentificador, CancellationToken ct = default);
    Task<SessaoCaixaDto> AbrirAsync(AbrirCaixaRequest request, string operadorId, CancellationToken ct = default);
    Task<MovimentacaoCaixaDto> SuprimentoAsync(SuprimentoSangriaRequest request, string operadorId, CancellationToken ct = default);
    Task<MovimentacaoCaixaDto> SangriaAsync(SuprimentoSangriaRequest request, string operadorId, CancellationToken ct = default);
    Task<ResumoFechamentoDto> FecharAsync(FecharCaixaRequest request, string operadorId, CancellationToken ct = default);
    Task<PaginaListaDto<SessaoCaixaDto>> HistoricoAsync(int pagina, int tamanho, CancellationToken ct = default);
    Task ValidarCaixaAbertoAsync(string caixaIdentificador, CancellationToken ct = default);
}
