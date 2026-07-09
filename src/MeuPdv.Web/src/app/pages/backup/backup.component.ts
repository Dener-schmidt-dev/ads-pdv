import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { NotificationService } from '../../core/notification/notification.service';
import {
  BackupHistorico,
  BackupStatus,
  ExecutarBackupResultado
} from '../../core/models/backup.models';
import { paginar, TAMANHO_PAGINA } from '../../core/pagination/paginate';
import { PaginationComponent } from '../../shared/pagination/pagination.component';

@Component({
  selector: 'app-backup',
  standalone: true,
  imports: [DatePipe, RouterLink, PaginationComponent],
  templateUrl: './backup.component.html',
  styleUrl: './backup.component.scss'
})
export class BackupComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  readonly status = signal<BackupStatus | null>(null);
  readonly historico = signal<BackupHistorico[]>([]);
  readonly pagina = signal(1);
  readonly tamanhoPagina = TAMANHO_PAGINA;

  readonly historicoPagina = computed(() =>
    paginar(this.historico(), this.pagina(), this.tamanhoPagina)
  );
  readonly carregando = signal(false);
  readonly executando = signal(false);

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.carregando.set(true);

    this.http.get<BackupStatus>('/api/backup/status').subscribe({
      next: (s) => {
        this.status.set(s);
        this.carregando.set(false);
      },
      error: () => {
        this.carregando.set(false);
        this.notify.error('Erro ao carregar status do backup.');
      }
    });

    this.http.get<BackupHistorico[]>('/api/backup/historico').subscribe({
      next: (h) => this.historico.set(h)
    });
  }

  executarBackup(): void {
    this.executando.set(true);

    this.http.post<ExecutarBackupResultado>('/api/backup/executar', {}).subscribe({
      next: (r) => {
        this.executando.set(false);
        if (r.sucesso) {
          this.notify.success(`Backup criado: ${r.nomeArquivo}`);
          this.carregar();
        } else {
          this.notify.error(r.mensagem ?? 'Falha ao executar backup.');
        }
      },
      error: (e) => {
        this.executando.set(false);
        this.notify.apiError(e, 'Falha ao executar backup.');
      }
    });
  }

  irParaPagina(pagina: number): void {
    this.pagina.set(pagina);
  }

  restaurar(item: BackupHistorico): void {
    if (!item.sucesso) return;

    const msg = `ATENÇÃO: Isso substituirá todos os dados atuais pelo backup "${item.nomeArquivo}".\n\nDeseja continuar?`;
    if (!confirm(msg)) return;

    this.executando.set(true);

    this.http.post<ExecutarBackupResultado>(`/api/backup/restaurar/${item.id}`, { confirmar: true }).subscribe({
      next: (r) => {
        this.executando.set(false);
        if (r.sucesso) {
          this.notify.success('Backup restaurado com sucesso. Recomenda-se reiniciar a API.');
        } else {
          this.notify.error(r.mensagem ?? 'Falha ao restaurar backup.');
        }
      },
      error: (e) => {
        this.executando.set(false);
        this.notify.apiError(e, 'Falha ao restaurar backup.');
      }
    });
  }

  formatarTamanho(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
