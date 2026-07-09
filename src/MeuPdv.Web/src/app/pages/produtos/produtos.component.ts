import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notification/notification.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import {
  ImportacaoCsvResultado,
  ProdutoDto,
  ProdutoListaResponse
} from '../../core/models/produto.models';
import { urlImagemProduto } from '../../core/produto/produto-imagem';
import { TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-produtos',
  standalone: true,
  imports: [FormsModule, RouterLink, PaginationComponent, ...FORMATTING_IMPORTS],
  templateUrl: './produtos.component.html',
  styleUrl: './produtos.component.scss'
})
export class ProdutosComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);
  readonly urlImagemProduto = urlImagemProduto;

  readonly produtos = signal<ProdutoDto[]>([]);
  readonly total = signal(0);
  readonly resultadoImport = signal<ImportacaoCsvResultado | null>(null);

  busca = '';
  somenteAtivos = true;
  somenteAbaixoMinimo = false;
  pagina = 1;
  readonly tamanhoPagina = TAMANHO_PAGINA;

  ngOnInit(): void {
    this.carregar();
  }

  buscar(): void {
    this.pagina = 1;
    this.carregar();
  }

  irParaPagina(pagina: number): void {
    this.pagina = pagina;
    this.carregar();
  }

  carregar(): void {
    const params = new URLSearchParams({
      pagina: String(this.pagina),
      tamanho: String(this.tamanhoPagina)
    });
    if (this.busca) params.set('termo', this.busca);
    if (this.somenteAtivos) params.set('ativo', 'true');
    if (this.somenteAbaixoMinimo) params.set('abaixoMinimo', 'true');

    this.http.get<ProdutoListaResponse>(`/api/produtos?${params}`).subscribe({
      next: (r) => {
        this.produtos.set(r.itens);
        this.total.set(r.total);
      },
      error: () => this.notify.error('Erro ao carregar produtos.')
    });
  }

  inativar(p: ProdutoDto): void {
    if (!confirm(`Inativar ${p.descricao}?`)) return;
    this.http.post(`/api/produtos/${p.id}/inativar`, {}).subscribe({
      next: () => this.carregar(),
      error: (e) => this.notify.apiError(e, 'Erro ao inativar.')
    });
  }

  exportarCsv(): void {
    this.http.get('/api/produtos/exportar-csv', { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `produtos-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.notify.error('Erro ao exportar CSV.')
    });
  }

  onArquivoCsv(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('arquivo', file);

    this.http.post<ImportacaoCsvResultado>('/api/produtos/importar-csv', formData).subscribe({
      next: (r) => {
        this.resultadoImport.set(r);
        this.carregar();
      },
      error: (e) => this.notify.apiError(e, 'Erro na importação.')
    });

    input.value = '';
  }
}
