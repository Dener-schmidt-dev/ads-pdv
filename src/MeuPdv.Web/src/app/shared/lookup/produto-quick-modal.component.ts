import { Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { FORMATTING_IMPORTS } from '../../core/format/formatting.imports';
import { NotificationService } from '../../core/notification/notification.service';
import { ProdutoDto } from '../../core/models/produto.models';
import { ProdutoLookupValue } from './lookup.models';

@Component({
  selector: 'app-produto-quick-modal',
  standalone: true,
  imports: [FormsModule, ...FORMATTING_IMPORTS],
  templateUrl: './produto-quick-modal.component.html',
  styleUrl: './produto-quick-modal.component.scss'
})
export class ProdutoQuickModalComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  @Input({ required: true }) descricaoInicial = '';
  @Input() codigoBarrasInicial = '';
  @Output() readonly criado = new EventEmitter<ProdutoLookupValue>();
  @Output() readonly cancelado = new EventEmitter<void>();

  readonly salvando = signal(false);

  form = {
    descricao: '',
    precoVenda: 0,
    codigoBarras: ''
  };

  ngOnInit(): void {
    this.form = {
      descricao: this.descricaoInicial,
      precoVenda: 0,
      codigoBarras: this.codigoBarrasInicial
    };
  }

  fechar(): void {
    this.cancelado.emit();
  }

  salvar(): void {
    const descricao = this.form.descricao.trim();
    if (!descricao) {
      this.notify.warning('Informe a descrição do produto.');
      return;
    }

    if (this.form.precoVenda <= 0) {
      this.notify.warning('Informe o preço de venda.');
      return;
    }

    this.salvando.set(true);
    this.http.post<ProdutoDto>('/api/produtos/rapido', {
      descricao,
      precoVenda: this.form.precoVenda,
      codigoBarras: this.form.codigoBarras.trim() || null
    }).subscribe({
      next: (p) => {
        this.salvando.set(false);
        this.criado.emit({
          id: p.id,
          codigoInterno: p.codigoInterno,
          codigoBarras: p.codigoBarras,
          descricao: p.descricao,
          precoVenda: p.precoVenda,
          estoqueAtual: p.estoqueAtual,
          unidade: p.unidade,
          temImagem: p.temImagem
        });
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao cadastrar produto.');
      }
    });
  }
}
