import { Component, inject } from '@angular/core';
import { NotificationService, NotificationType } from './notification.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.scss'
})
export class ToastContainerComponent {
  private readonly notify = inject(NotificationService);
  readonly notifications = this.notify.notifications;

  dismiss(id: string): void {
    this.notify.dismiss(id);
  }

  icon(type: NotificationType): string {
    switch (type) {
      case 'error':
        return 'error';
      case 'warning':
        return 'warning';
      case 'success':
        return 'check_circle';
      default:
        return 'info';
    }
  }
}
