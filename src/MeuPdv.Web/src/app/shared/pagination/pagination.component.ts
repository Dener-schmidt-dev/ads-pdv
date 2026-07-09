import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (total > 0) {
      <nav class="pagination" aria-label="Paginação">
        <p class="pagination__info">
          Mostrando {{ inicio }}–{{ fim }} de {{ total }} {{ rotuloExibicao }}
        </p>
        <div class="pagination__controls">
          <button
            type="button"
            class="pagination__btn"
            [disabled]="pagina <= 1"
            (click)="anterior()"
          >
            <span class="mi mi--sm" aria-hidden="true">chevron_left</span>
            Anterior
          </button>
          <span class="pagination__status">Página {{ pagina }} de {{ totalPaginas }}</span>
          <button
            type="button"
            class="pagination__btn"
            [disabled]="pagina >= totalPaginas"
            (click)="proxima()"
          >
            Próxima
            <span class="mi mi--sm" aria-hidden="true">chevron_right</span>
          </button>
        </div>
      </nav>
    }
  `,
  styleUrl: './pagination.component.scss'
})
export class PaginationComponent {
  @Input() pagina = 1;
  @Input() tamanho = 20;
  @Input() total = 0;
  @Input() rotulo = 'registro';

  @Output() paginaChange = new EventEmitter<number>();

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamanho));
  }

  get inicio(): number {
    if (this.total === 0) return 0;
    return (this.pagina - 1) * this.tamanho + 1;
  }

  get fim(): number {
    return Math.min(this.pagina * this.tamanho, this.total);
  }

  get rotuloExibicao(): string {
    return this.total === 1 ? this.rotulo : `${this.rotulo}s`;
  }

  anterior(): void {
    if (this.pagina > 1) this.paginaChange.emit(this.pagina - 1);
  }

  proxima(): void {
    if (this.pagina < this.totalPaginas) this.paginaChange.emit(this.pagina + 1);
  }
}
