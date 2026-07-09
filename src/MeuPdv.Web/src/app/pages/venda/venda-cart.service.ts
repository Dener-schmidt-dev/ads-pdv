import { Injectable, signal, computed } from '@angular/core';
import { CarrinhoItem, ClienteVenda, ProdutoLeitura } from '../../core/models/venda.models';

@Injectable()
export class VendaCartService {
  readonly itens = signal<CarrinhoItem[]>([]);
  readonly cliente = signal<ClienteVenda | null>(null);
  readonly desconto = signal(0);
  readonly motivoDesconto = signal('');
  readonly linhaSelecionada = signal(0);
  readonly feedback = signal<string | null>(null);

  readonly subtotal = computed(() =>
    this.itens().reduce((s, i) => s + i.quantidade * i.precoUnitario, 0)
  );

  readonly total = computed(() => Math.max(0, this.subtotal() - this.desconto()));

  adicionarProduto(p: ProdutoLeitura): void {
    const itens = [...this.itens()];
    const existente = itens.find((i) => i.produtoId === p.id);

    if (existente) {
      existente.quantidade += 1;
    } else {
      itens.push({
        produtoId: p.id,
        codigoInterno: p.codigoInterno,
        codigoBarras: p.codigoBarras,
        descricao: p.descricao,
        quantidade: 1,
        precoUnitario: p.precoVenda,
        temImagem: p.temImagem
      });
      this.linhaSelecionada.set(itens.length - 1);
    }

    this.itens.set(itens);
    this.feedback.set(p.descricao);
    setTimeout(() => this.feedback.set(null), 800);
  }

  removerLinha(index: number): void {
    const itens = this.itens().filter((_, i) => i !== index);
    this.itens.set(itens);
    this.linhaSelecionada.set(Math.min(index, Math.max(0, itens.length - 1)));
  }

  alterarQuantidade(index: number, quantidade: number): void {
    if (quantidade <= 0) {
      this.removerLinha(index);
      return;
    }
    const itens = [...this.itens()];
    itens[index] = { ...itens[index], quantidade };
    this.itens.set(itens);
  }

  limpar(): void {
    this.itens.set([]);
    this.cliente.set(null);
    this.desconto.set(0);
    this.motivoDesconto.set('');
    this.linhaSelecionada.set(0);
  }
}
