import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { IconComponent, IconName } from '../../../shared/components/icon/icon.component';
import { NotificationService } from '../../../core/services/notification.service';
import { AppNotification } from '../../../core/models/notification.model';

const TYPE_META: Record<string, { title: string; icon: IconName; isAi: boolean }> = {
  escalation: { title: 'Ticket escalated', icon: 'arrow-up-right', isAi: false },
  comment: { title: 'New comment', icon: 'mail', isAi: false },
  'sla-warning': { title: 'SLA warning', icon: 'alert-triangle', isAi: false },
  'sla-breach': { title: 'SLA breached', icon: 'alert-triangle', isAi: false },
  'ai-solution': { title: 'New solution suggested', icon: 'sparkles', isAi: true },
};

@Component({
  selector: 'sp-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, IconComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent implements OnInit {
  notifications = signal<AppNotification[]>([]);
  loading = signal(true);

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.notificationService.list().subscribe((items) => {
      this.notifications.set(items);
      this.loading.set(false);
    });
  }

  markAllRead(): void {
    this.notificationService.markAllRead().subscribe(() => {
      this.notifications.set(this.notifications().map((n) => ({ ...n, read: true })));
    });
  }

  open(n: AppNotification): void {
    if (!n.read) {
      this.notificationService.markRead(n.id).subscribe();
      this.notifications.set(this.notifications().map((x) => (x.id === n.id ? { ...x, read: true } : x)));
    }
  }

  meta(n: AppNotification) {
    return TYPE_META[n.type] ?? { title: 'Notification', icon: 'bell' as IconName, isAi: false };
  }

  timeAgo(iso: string): string {
    const diffMs = Date.now() - new Date(iso).getTime();
    const minutes = Math.floor(diffMs / 60_000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days === 1) return 'Yesterday';
    return `${days}d ago`;
  }

  get unreadCount(): number {
    return this.notifications().filter((n) => !n.read).length;
  }
}
