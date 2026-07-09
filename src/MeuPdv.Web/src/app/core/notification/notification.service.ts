import { Injectable, signal } from '@angular/core';
import { extrairErroApi } from '../http/api-error';

export type NotificationType = 'error' | 'warning' | 'success' | 'info';

export interface Notification {
  id: string;
  type: NotificationType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly items = signal<Notification[]>([]);
  readonly notifications = this.items.asReadonly();

  private seq = 0;

  error(message: string, durationMs = 6000): void {
    this.show('error', message, durationMs);
  }

  warning(message: string, durationMs = 7000): void {
    this.show('warning', message, durationMs);
  }

  success(message: string, durationMs = 4500): void {
    this.show('success', message, durationMs);
  }

  info(message: string, durationMs = 5000): void {
    this.show('info', message, durationMs);
  }

  apiError(err: unknown, fallback: string): void {
    this.error(extrairErroApi(err, fallback));
  }

  dismiss(id: string): void {
    this.items.update((list) => list.filter((n) => n.id !== id));
  }

  clear(): void {
    this.items.set([]);
  }

  private show(type: NotificationType, message: string, durationMs: number): void {
    const text = message.trim();
    if (!text) return;

    const id = `${Date.now()}-${++this.seq}`;
    this.items.update((list) => [...list, { id, type, message: text }]);

    if (durationMs > 0) {
      setTimeout(() => this.dismiss(id), durationMs);
    }
  }
}
