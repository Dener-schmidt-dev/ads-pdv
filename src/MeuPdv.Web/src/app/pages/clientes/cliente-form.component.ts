import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ClienteDto } from '../../core/models/cliente.models';
import { NotificationService } from '../../core/notification/notification.service';

@Component({
  selector: 'app-cliente-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './cliente-form.component.html',
  styleUrl: './cliente-form.component.scss'
})
export class ClienteFormComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  readonly editandoId = signal<string | null>(null);
  readonly salvando = signal(false);
  readonly carregando = signal(false);

  form = {
    nome: '',
    cpf: '',
    telefone: '',
    observacoes: ''
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editandoId.set(id);
      this.carregarCliente(id);
    }
  }

  salvar(): void {
    this.salvando.set(true);

    const body = {
      nome: this.form.nome,
      cpf: this.form.cpf || null,
      telefone: this.form.telefone || null,
      observacoes: this.form.observacoes || null
    };

    const id = this.editandoId();
    const req$ = id
      ? this.http.put<ClienteDto>(`/api/clientes/${id}`, body)
      : this.http.post<ClienteDto>('/api/clientes', body);

    req$.subscribe({
      next: () => {
        this.salvando.set(false);
        void this.router.navigate(['/clientes']);
      },
      error: (e) => {
        this.salvando.set(false);
        this.notify.apiError(e, 'Erro ao salvar cliente.');
      }
    });
  }

  cancelar(): void {
    void this.router.navigate(['/clientes']);
  }

  private carregarCliente(id: string): void {
    this.carregando.set(true);
    this.http.get<ClienteDto>(`/api/clientes/${id}`).subscribe({
      next: (cliente) => {
        this.form = {
          nome: cliente.nome,
          cpf: cliente.cpf ?? '',
          telefone: cliente.telefone ?? '',
          observacoes: cliente.observacoes ?? ''
        };
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notify.error('Cliente não encontrado.');
      }
    });
  }
}
