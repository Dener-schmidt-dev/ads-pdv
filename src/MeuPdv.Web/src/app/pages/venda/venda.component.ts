import {
  Component,
  ElementRef,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
  viewChild
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notification/notification.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import {
  FinalizarVendaResponse,
  PagamentoVenda,
  PixQrResponse,
  ProdutoLeitura
} from '../../core/models/venda.models';
import { SessaoCaixaDto } from '../../core/models/caixa.models';
import { urlImagemProduto } from '../../core/produto/produto-imagem';
import { VendaCartService } from './venda-cart.service';
import { ClienteLookupComponent } from '../../shared/lookup/cliente-lookup.component';
import { ClienteQuickModalComponent } from '../../shared/lookup/cliente-quick-modal.component';
import { ProdutoQuickModalComponent } from '../../shared/lookup/produto-quick-modal.component';
import { ClienteLookupValue, ProdutoLookupValue } from '../../shared/lookup/lookup.models';
import { PdvThemeService } from '../../core/pdv/pdv-theme.service';

type Modal = 'none' | 'qty' | 'desconto' | 'pagamento';

@Component({
  selector: 'app-venda',
  standalone: true,
  imports: [FormsModule, RouterLink, ClienteLookupComponent, ClienteQuickModalComponent, ProdutoQuickModalComponent, ...FORMATTING_IMPORTS],
  providers: [VendaCartService],
  templateUrl: './venda.component.html',
  styleUrl: './venda.component.scss'
})
export class VendaComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);
  readonly urlImagemProduto = urlImagemProduto;
  readonly cart = inject(VendaCartService);
  readonly pdvTheme = inject(PdvThemeService);

  readonly buscaInput = viewChild<ElementRef<HTMLInputElement>>('buscaInput');
  readonly clienteLookup = viewChild(ClienteLookupComponent);

  readonly sessaoCaixa = signal<SessaoCaixaDto | null>(null);
  readonly caixaAberto = computed(() => this.sessaoCaixa() !== null);
  readonly podeFinalizar = computed(() =>
    this.caixaAberto() && this.cart.itens().length > 0 && this.cart.total() > 0.009
  );
  readonly sugestoes = signal<ProdutoLeitura[]>([]);
  readonly sugestaoIndex = signal(0);
  readonly buscaProdutoConcluida = signal(false);
  readonly modal = signal<Modal>('none');
  readonly modalProdutoRapido = signal(false);
  readonly modalClienteRapido = signal(false);
  readonly processando = signal(false);

  busca = '';
  qtyModal = 1;
  descontoValor = 0;
  descontoPercentual = 0;
  descontoModo: 'valor' | 'percentual' = 'valor';
  motivoDesconto = '';
  produtoRapidoTermo = '';
  produtoRapidoBarras = '';
  clienteRapidoNome = '';

  pagamentos = signal<PagamentoVenda[]>([]);
  formaAtual = 'Dinheiro';
  valorPagamento = 0;
  valorRecebido = 0;
  pixQr = signal<PixQrResponse | null>(null);
  pixConfirmado = false;
  pixGerando = signal(false);

  caixaId = 'CAIXA-01';
  private buscaTimer?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.pdvTheme.carregar();
    this.carregarCaixa();
    setTimeout(() => {
      if (this.caixaAberto()) this.focarBusca();
    }, 100);
  }

  carregarCaixa(): void {
    this.http.get<SessaoCaixaDto>(`/api/caixa/atual?caixa=${this.caixaId}`).subscribe({
      next: (s) => {
        this.sessaoCaixa.set(s);
        if (s) setTimeout(() => this.focarBusca(), 50);
      },
      error: () => this.sessaoCaixa.set(null)
    });
  }

  @HostListener('window:focus')
  onWindowFocus(): void {
    this.carregarCaixa();
  }

  private exigirCaixaAberto(): boolean {
    if (this.caixaAberto()) return true;
    this.notify.warning('Caixa fechado. Abra o caixa para iniciar vendas.');
    return false;
  }

  focarBusca(): void {
    if (!this.caixaAberto()) return;
    this.buscaInput()?.nativeElement?.focus();
  }

  onBuscaInput(): void {
    if (!this.caixaAberto()) return;
    clearTimeout(this.buscaTimer);
    const termo = this.busca.trim();

    if (/^\d{8,}$/.test(termo)) {
      this.sugestoes.set([]);
      return;
    }

    if (termo.length < 2) {
      this.sugestoes.set([]);
      this.buscaProdutoConcluida.set(false);
      return;
    }

    this.buscaProdutoConcluida.set(false);
    this.buscaTimer = setTimeout(() => {
      this.http.get<ProdutoLeitura[]>(`/api/vendas/busca-produtos?termo=${encodeURIComponent(termo)}`).subscribe({
        next: (p) => {
          this.sugestoes.set(p);
          this.sugestaoIndex.set(0);
          this.buscaProdutoConcluida.set(true);
        },
        error: () => {
          this.sugestoes.set([]);
          this.buscaProdutoConcluida.set(true);
        }
      });
    }, 200);
  }

  onBuscaKeydown(event: KeyboardEvent): void {
    if (!this.caixaAberto()) {
      event.preventDefault();
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const max = this.totalOpcoesProduto() - 1;
      this.sugestaoIndex.update((i) => Math.min(i + 1, max));
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.sugestaoIndex.update((i) => Math.max(i - 1, 0));
      return;
    }
    if (event.key === 'Enter') {
      event.preventDefault();
      const sugs = this.sugestoes();
      const termo = this.busca.trim();
      if (this.mostrarCriarProduto() && this.sugestaoIndex() === sugs.length) {
        this.abrirCriarProduto();
      } else if (sugs.length > 0 && termo.length >= 2 && !/^\d{8,}$/.test(termo)) {
        this.selecionarSugestao(sugs[this.sugestaoIndex()]);
      } else {
        this.lerCodigo();
      }
    }
    if (event.key === 'Escape') {
      this.sugestoes.set([]);
    }
  }

  lerCodigo(): void {
    if (!this.exigirCaixaAberto()) return;

    const codigo = this.busca.trim();
    if (!codigo) return;

    this.http.get<ProdutoLeitura>(`/api/vendas/produto/${encodeURIComponent(codigo)}`).subscribe({
      next: (p) => {
        this.cart.adicionarProduto(p);
        this.busca = '';
        this.sugestoes.set([]);
        this.focarBusca();
      },
      error: () => {
        const termo = codigo.trim();
        if (termo.length >= 2 && !/^\d+$/.test(termo)) {
          this.abrirCriarProduto(termo);
          return;
        }
        this.notify.error('Produto não encontrado.');
        this.focarBusca();
      }
    });
  }

  selecionarSugestao(p: ProdutoLeitura): void {
    if (!this.exigirCaixaAberto()) return;

    this.cart.adicionarProduto(p);
    this.busca = '';
    this.sugestoes.set([]);
    this.focarBusca();
  }

  selecionarLinha(index: number): void {
    this.cart.linhaSelecionada.set(index);
  }

  abrirQtyModal(): void {
    if (!this.exigirCaixaAberto()) return;

    const idx = this.cart.linhaSelecionada();
    const item = this.cart.itens()[idx];
    if (!item) return;
    this.qtyModal = item.quantidade;
    this.modal.set('qty');
  }

  confirmarQty(): void {
    this.cart.alterarQuantidade(this.cart.linhaSelecionada(), this.qtyModal);
    this.fecharModal();
    this.focarBusca();
  }

  abrirDesconto(): void {
    if (!this.exigirCaixaAberto()) return;

    if (!this.auth.temPermissao('ConcederDesconto')) {
      this.notify.warning('Sem permissão para conceder desconto.');
      return;
    }
    this.descontoValor = this.cart.desconto();
    this.modal.set('desconto');
  }

  confirmarDesconto(): void {
    const sub = this.cart.subtotal();
    let valor = this.descontoModo === 'percentual'
      ? Math.round(sub * this.descontoPercentual) / 100
      : this.descontoValor;

    valor = Math.min(Math.max(0, valor), sub);
    this.cart.desconto.set(valor);
    this.cart.motivoDesconto.set(this.motivoDesconto);
    this.fecharModal();
    this.focarBusca();
  }

  mostrarCriarProduto(): boolean {
    const termo = this.busca.trim();
    return termo.length >= 2
      && !/^\d{8,}$/.test(termo)
      && this.buscaProdutoConcluida();
  }

  totalOpcoesProduto(): number {
    return this.sugestoes().length + (this.mostrarCriarProduto() ? 1 : 0);
  }

  abrirCriarProduto(termo?: string): void {
    if (!this.exigirCaixaAberto()) return;

    const descricao = (termo ?? this.busca).trim();
    if (!descricao) return;

    this.produtoRapidoTermo = descricao;
    this.produtoRapidoBarras = /^\d{8,}$/.test(descricao) ? descricao : '';
    this.modalProdutoRapido.set(true);
    this.sugestoes.set([]);
  }

  onProdutoRapidoCriado(produto: ProdutoLookupValue): void {
    this.modalProdutoRapido.set(false);
    this.cart.adicionarProduto(produto);
    this.busca = '';
    this.buscaProdutoConcluida.set(false);
    this.focarBusca();
  }

  onProdutoRapidoCancelado(): void {
    this.modalProdutoRapido.set(false);
    this.focarBusca();
  }

  onClienteChange(cliente: ClienteLookupValue | null): void {
    this.cart.cliente.set(cliente);
  }

  onClienteCriarSolicitado(nome: string): void {
    if (!this.exigirCaixaAberto()) return;
    this.clienteRapidoNome = nome;
    this.modalClienteRapido.set(true);
  }

  onClienteRapidoCriado(cliente: ClienteLookupValue): void {
    this.modalClienteRapido.set(false);
    this.onClienteChange(cliente);
    this.clienteLookup()?.focar();
  }

  onClienteRapidoCancelado(): void {
    this.modalClienteRapido.set(false);
    this.clienteLookup()?.focar();
  }

  abrirCliente(): void {
    if (!this.exigirCaixaAberto()) return;
    this.clienteLookup()?.focar();
  }

  abrirPagamento(): void {
    if (!this.exigirCaixaAberto()) return;
    if (!this.podeFinalizar()) {
      this.notify.warning('Adicione itens com valor para finalizar a venda.');
      return;
    }

    this.pagamentos.set([]);
    this.formaAtual = 'Dinheiro';
    this.valorPagamento = this.cart.total();
    this.valorRecebido = this.cart.total();
    this.pixQr.set(null);
    this.pixConfirmado = false;
    this.modal.set('pagamento');
  }

  onFormaPagamentoChange(): void {
    this.pixConfirmado = false;
    this.pixQr.set(null);
    if (this.formaAtual === 'Pix') {
      this.gerarPixQr();
    }
  }

  gerarPixQr(): void {
    const valor = this.valorPagamento > 0 ? this.valorPagamento : this.restantePagamento();
    if (valor <= 0) return;

    this.pixGerando.set(true);
    this.http.post<PixQrResponse>('/api/pix/qr', {
      valor,
      identificador: `PDV-${Date.now()}`,
      descricao: 'Venda MeuPDV'
    }).subscribe({
      next: (r) => {
        this.pixQr.set(r);
        this.pixGerando.set(false);
        this.pixConfirmado = false;
      },
      error: (e) => {
        this.pixGerando.set(false);
        this.notify.apiError(e, 'Erro ao gerar QR Code Pix.');
      }
    });
  }

  restantePagamento(): number {
    const pago = this.pagamentos().reduce((s, p) => s + p.valor, 0);
    return Math.max(0, this.cart.total() - pago);
  }

  adicionarPagamento(): void {
    if (this.formaAtual === 'Pix' && !this.pixConfirmado) {
      this.notify.warning('Confirme o recebimento Pix manualmente antes de adicionar.');
      return;
    }

    const restante = this.restantePagamento();
    const valor = this.valorPagamento > 0 ? this.valorPagamento : restante;

    if (valor <= 0) return;
    if (valor > restante && this.formaAtual !== 'Dinheiro') {
      this.notify.warning('Excesso só permitido em dinheiro.');
      return;
    }

    this.pagamentos.update((list) => [
      ...list,
      {
        forma: this.formaAtual,
        valor,
        valorRecebido: this.formaAtual === 'Dinheiro' ? this.valorRecebido : valor
      }
    ]);

    const novoRestante = this.restantePagamento();
    this.valorPagamento = novoRestante;
    this.valorRecebido = novoRestante;
    this.pixQr.set(null);
    this.pixConfirmado = false;
  }

  troco(): number {
    const dinheiro = this.pagamentos().filter((p) => p.forma === 'Dinheiro');
    return dinheiro.reduce((s, p) => s + Math.max(0, p.valorRecebido - p.valor), 0);
  }

  confirmarVenda(): void {
    if (!this.exigirCaixaAberto()) return;

    if (this.restantePagamento() > 0.009) {
      this.notify.warning('Valor pago insuficiente.');
      return;
    }

    this.processando.set(true);

    const body = {
      caixaIdentificador: this.caixaId,
      clienteId: this.cart.cliente()?.id ?? null,
      itens: this.cart.itens().map((i) => ({ produtoId: i.produtoId, quantidade: i.quantidade })),
      desconto: this.cart.desconto(),
      motivoDesconto: this.cart.motivoDesconto() || null,
      pagamentos: this.pagamentos().map((p) => ({
        forma: p.forma,
        valor: p.valor,
        valorRecebido: p.valorRecebido
      }))
    };

    this.http.post<FinalizarVendaResponse>('/api/vendas/finalizar', body).subscribe({
      next: (res) => {
        this.processando.set(false);
        if (res.imprimirComprovante && res.comprovanteTexto) {
          this.imprimirComprovante(res.comprovanteTexto);
        }
        this.cart.limpar();
        this.fecharModal();
        this.focarBusca();
      },
      error: (e) => {
        this.processando.set(false);
        this.notify.apiError(e, 'Erro ao finalizar venda.');
      }
    });
  }

  cancelarVenda(): void {
    if (this.cart.itens().length === 0) return;
    if (confirm('Cancelar venda atual?')) {
      this.cart.limpar();
      this.focarBusca();
    }
  }

  removerLinha(index: number): void {
    if (!this.exigirCaixaAberto()) return;

    if (!this.auth.temPermissao('CancelarItem')) {
      this.notify.warning('Sem permissão para remover item.');
      return;
    }
    if (this.cart.itens()[index]) {
      this.cart.removerLinha(index);
      this.focarBusca();
    }
  }

  removerItem(): void {
    this.removerLinha(this.cart.linhaSelecionada());
  }

  fecharModal(): void {
    this.modal.set('none');
    this.pixQr.set(null);
    this.pixConfirmado = false;
  }

  imprimirComprovante(texto: string): void {
    const w = window.open('', '_blank', 'width=320,height=600');
    if (!w) return;
    w.document.write(`<pre style="font-family:monospace;font-size:12px">${texto}</pre>`);
    w.document.close();
    w.print();
    w.close();
  }

  @HostListener('document:keydown', ['$event'])
  atalhos(event: KeyboardEvent): void {
    if (this.modalProdutoRapido() || this.modalClienteRapido()) {
      if (event.key === 'Escape') {
        event.preventDefault();
        if (this.modalProdutoRapido()) this.onProdutoRapidoCancelado();
        if (this.modalClienteRapido()) this.onClienteRapidoCancelado();
      }
      return;
    }

    if (this.modal() !== 'none') {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.fecharModal();
      }
      if (this.caixaAberto()) {
        if (event.key === 'Enter' && this.modal() === 'qty') this.confirmarQty();
        if (event.key === 'Enter' && this.modal() === 'desconto') this.confirmarDesconto();
      }
      return;
    }

    const tag = (event.target as HTMLElement)?.tagName;
    const emInput = tag === 'INPUT' || tag === 'TEXTAREA';

    if (!this.caixaAberto()) {
      return;
    }

    if (event.key === 'F2') { event.preventDefault(); this.abrirCliente(); }
    if (event.key === 'F3') { event.preventDefault(); this.abrirQtyModal(); }
    if (event.key === 'F4' && this.auth.temPermissao('ConcederDesconto')) {
      event.preventDefault();
      this.abrirDesconto();
    }
    if (event.key === 'F5') {
      event.preventDefault();
      if (this.podeFinalizar()) this.abrirPagamento();
      return;
    }
    if (event.key === 'F6') { event.preventDefault(); this.cancelarVenda(); }
    if (event.key === 'Delete' && !emInput && this.auth.temPermissao('CancelarItem')) {
      event.preventDefault();
      this.removerItem();
    }
  }

  labelForma(f: string): string {
    const map: Record<string, string> = {
      Dinheiro: 'Dinheiro',
      CartaoDebito: 'Débito',
      CartaoCredito: 'Crédito',
      Pix: 'Pix',
      Outros: 'Outros'
    };
    return map[f] ?? f;
  }
}
