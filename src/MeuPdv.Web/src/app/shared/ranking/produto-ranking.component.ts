import { Component, Input } from '@angular/core';
import { ProdutoMaisVendido } from '../../core/models/relatorio.models';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';

@Component({
  selector: 'app-produto-ranking',
  standalone: true,
  imports: [...FORMATTING_IMPORTS],
  template: `
    @if (lista.length === 0) {
      <div class="ranking-empty">
        <span class="mi" aria-hidden="true">inventory_2</span>
        <p>Nenhuma venda no período.</p>
      </div>
    } @else {
      <ol class="produto-ranking">
        @for (item of lista; track item.produtoId; let i = $index) {
          <li class="produto-ranking__item">
            <span class="produto-ranking__pos">{{ i + 1 }}</span>
            <div class="produto-ranking__body">
              <div class="produto-ranking__row">
                <span class="produto-ranking__nome" [title]="item.descricao">{{ item.descricao }}</span>
                <span class="produto-ranking__valor">{{ item.valorTotal | brl }}</span>
              </div>
              <div class="produto-ranking__bar" aria-hidden="true">
                <span class="produto-ranking__fill" [style.width.%]="percentual(item)"></span>
              </div>
              <span class="produto-ranking__qtd">{{ item.quantidade }} un.</span>
            </div>
          </li>
        }
      </ol>
    }
  `,
  styleUrl: './produto-ranking.component.scss'
})
export class ProdutoRankingComponent {
  @Input({ required: true }) itens: ProdutoMaisVendido[] = [];
  @Input() limite = 10;

  get lista(): ProdutoMaisVendido[] {
    return this.itens.slice(0, this.limite);
  }

  percentual(item: ProdutoMaisVendido): number {
    const max = Math.max(...this.lista.map((i) => i.valorTotal), 1);
    return (item.valorTotal / max) * 100;
  }
}
