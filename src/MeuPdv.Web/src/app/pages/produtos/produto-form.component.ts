import { Component, ElementRef, inject, OnDestroy, OnInit, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { extrairErroApi } from '../../core/http/api-error';
import { formatPercent } from '../../core/format/number-format';
import { NotificationService } from '../../core/notification/notification.service';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { CategoriaDto, ProdutoDto } from '../../core/models/produto.models';
import { urlImagemProduto } from '../../core/produto/produto-imagem';

@Component({
  selector: 'app-produto-form',
  standalone: true,
  imports: [FormsModule, RouterLink, ...FORMATTING_IMPORTS],
  templateUrl: './produto-form.component.html',
  styleUrl: './produto-form.component.scss'
})
export class ProdutoFormComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);
  readonly auth = inject(AuthService);

  @ViewChild('inputImagem') private inputImagem?: ElementRef<HTMLInputElement>;

  readonly editandoId = signal<string | null>(null);
  readonly categorias = signal<CategoriaDto[]>([]);
  readonly salvando = signal(false);
  readonly carregando = signal(false);
  readonly temImagemAtual = signal(false);
  readonly previewUrl = signal<string | null>(null);
  readonly removerImagemPendente = signal(false);

  readonly unidades = ['UN', 'KG', 'G', 'LT', 'ML', 'CX', 'PC', 'FD'];

  private imagemArquivo: File | null = null;
  private previewObjectUrl: string | null = null;

  form = this.novoForm();

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.carregarCategorias();

    if (id) {
      this.editandoId.set(id);
      this.carregarProduto(id);
    }
  }

  ngOnDestroy(): void {
    this.limparPreviewLocal();
  }

  abrirSeletorImagem(): void {
    this.inputImagem?.nativeElement.click();
  }

  onImagemSelecionada(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (file.size > 2 * 1024 * 1024) {
      this.notify.error('A imagem deve ter no máximo 2 MB.');
      input.value = '';
      return;
    }

    this.limparPreviewLocal();
    this.imagemArquivo = file;
    this.removerImagemPendente.set(false);
    this.previewObjectUrl = URL.createObjectURL(file);
    this.previewUrl.set(this.previewObjectUrl);
  }

  marcarRemoverImagem(): void {
    this.limparPreviewLocal();
    this.imagemArquivo = null;
    this.removerImagemPendente.set(true);
    this.previewUrl.set(null);
    if (this.inputImagem?.nativeElement) {
      this.inputImagem.nativeElement.value = '';
    }
  }

  margemLucro(): string {
    const preco = Number(this.form.precoVenda) || 0;
    const custo = Number(this.form.custo) || 0;
    if (preco <= 0) return '—';
    if (custo <= 0) return '—';
    const margem = ((preco - custo) / preco) * 100;
    return formatPercent(margem, 1);
  }

  salvar(): void {
    if (!this.form.descricao.trim()) {
      this.notify.warning('Informe a descrição do produto.');
      return;
    }

    (document.activeElement as HTMLElement | null)?.blur();
    this.salvando.set(true);

    const id = this.editandoId();
    const body = id
      ? {
          codigoBarras: this.form.codigoBarras || null,
          descricao: this.form.descricao,
          categoriaId: this.form.categoriaId || null,
          marca: this.form.marca || null,
          observacoes: this.form.observacoes || null,
          unidade: this.form.unidade,
          precoVenda: Number(this.form.precoVenda) || 0,
          custo: this.form.custo,
          estoqueMinimo: Number(this.form.estoqueMinimo) || 0,
          ativo: this.form.ativo
        }
      : {
          codigoInterno: this.form.codigoInterno || null,
          codigoBarras: this.form.codigoBarras || null,
          descricao: this.form.descricao,
          categoriaId: this.form.categoriaId || null,
          marca: this.form.marca || null,
          observacoes: this.form.observacoes || null,
          unidade: this.form.unidade,
          precoVenda: Number(this.form.precoVenda) || 0,
          custo: this.form.custo,
          estoqueAtual: Number(this.form.estoqueAtual) || 0,
          estoqueMinimo: Number(this.form.estoqueMinimo) || 0
        };

    const req$ = id
      ? this.http.put<ProdutoDto>(`/api/produtos/${id}`, body)
      : this.http.post<ProdutoDto>('/api/produtos', body);

    req$.subscribe({
      next: (produto) => {
        const produtoId = produto.id || id;
        if (!produtoId) {
          this.salvando.set(false);
          this.notify.error('Resposta inválida da API ao salvar o produto.');
          return;
        }
        this.enviarImagemSeNecessario(produtoId);
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao salvar produto.');
      }
    });
  }

  cancelar(): void {
    void this.router.navigate(['/produtos']);
  }

  private enviarImagemSeNecessario(produtoId: string): void {
    const precisaImagem = !!this.imagemArquivo || (this.removerImagemPendente() && this.temImagemAtual());

    if (!precisaImagem) {
      this.salvando.set(false);
      this.notify.success('Produto salvo.');
      void this.router.navigate(['/produtos']);
      return;
    }

    this.persistirImagem(produtoId).subscribe({
      next: () => {
        this.salvando.set(false);
        this.notify.success('Produto salvo.');
        void this.router.navigate(['/produtos']);
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.warning(
          `Produto salvo, mas a imagem não foi atualizada: ${extrairErroApi(e, 'falha no envio da imagem.')}`
        );
      }
    });
  }

  private persistirImagem(produtoId: string): Observable<void> {
    if (this.removerImagemPendente() && this.temImagemAtual()) {
      return this.http.delete<ProdutoDto>(`/api/produtos/${produtoId}/imagem`).pipe(map(() => undefined));
    }

    if (this.imagemArquivo) {
      const formData = new FormData();
      formData.append('arquivo', this.imagemArquivo);
      return this.http.post<ProdutoDto>(`/api/produtos/${produtoId}/imagem`, formData).pipe(map(() => undefined));
    }

    return of(undefined);
  }

  private carregarProduto(id: string): void {
    this.carregando.set(true);
    this.http.get<ProdutoDto>(`/api/produtos/${id}`).subscribe({
      next: (p) => {
        this.form = {
          codigoInterno: p.codigoInterno,
          codigoBarras: p.codigoBarras ?? '',
          descricao: p.descricao,
          categoriaId: p.categoriaId ?? '',
          marca: p.marca ?? '',
          observacoes: p.observacoes ?? '',
          unidade: this.unidades.includes(p.unidade) ? p.unidade : 'UN',
          precoVenda: p.precoVenda,
          custo: p.custo ?? null,
          estoqueAtual: p.estoqueAtual,
          estoqueMinimo: p.estoqueMinimo,
          ativo: p.ativo
        };
        this.temImagemAtual.set(!!p.temImagem);
        this.previewUrl.set(urlImagemProduto(p.id, p.temImagem, p.updatedAt));
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notify.error('Produto não encontrado.');
      }
    });
  }

  private carregarCategorias(): void {
    this.http.get<CategoriaDto[]>('/api/categorias').subscribe({
      next: (c) => this.categorias.set(c)
    });
  }

  private limparPreviewLocal(): void {
    if (this.previewObjectUrl) {
      URL.revokeObjectURL(this.previewObjectUrl);
      this.previewObjectUrl = null;
    }
  }

  private novoForm() {
    return {
      codigoInterno: '',
      codigoBarras: '',
      descricao: '',
      categoriaId: '',
      marca: '',
      observacoes: '',
      unidade: 'UN',
      precoVenda: 0,
      custo: null as number | null,
      estoqueAtual: 0,
      estoqueMinimo: 0,
      ativo: true
    };
  }
}
