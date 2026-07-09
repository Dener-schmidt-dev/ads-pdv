import { Component, EventEmitter, inject, Input, OnInit, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ClienteDto } from '../../core/models/cliente.models';
import { NotificationService } from '../../core/notification/notification.service';
import { ClienteLookupValue } from './lookup.models';

@Component({
  selector: 'app-cliente-quick-modal',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './cliente-quick-modal.component.html',
  styleUrl: './cliente-quick-modal.component.scss'
})
export class ClienteQuickModalComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  @Input({ required: true }) nomeInicial = '';
  @Output() readonly criado = new EventEmitter<ClienteLookupValue>();
  @Output() readonly cancelado = new EventEmitter<void>();

  readonly salvando = signal(false);

  form = {
    nome: '',
    cpf: '',
    telefone: ''
  };

  ngOnInit(): void {
    this.form = {
      nome: this.nomeInicial,
      cpf: '',
      telefone: ''
    };
  }

  fechar(): void {
    this.cancelado.emit();
  }

  salvar(): void {
    const nome = this.form.nome.trim();
    if (!nome) {
      this.notify.warning('Informe o nome do cliente.');
      return;
    }

    this.salvando.set(true);
    this.http.post<ClienteDto>('/api/clientes/rapido', {
      nome,
      cpf: this.form.cpf.trim() || null,
      telefone: this.form.telefone.trim() || null
    }).subscribe({
      next: (c) => {
        this.salvando.set(false);
        this.criado.emit({ id: c.id, nome: c.nome });
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao cadastrar cliente.');
      }
    });
  }
}
