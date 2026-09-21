import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AppNotification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private http: HttpClient) {}

  list(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${environment.apiBaseUrl}/notifications`);
  }

  unreadCount(): Observable<number> {
    return this.http.get<number>(`${environment.apiBaseUrl}/notifications/unread-count`);
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/notifications/${id}/read`, {});
  }

  markAllRead(): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/notifications/read-all`, {});
  }
}
