import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { CreateUserPayload, UpdateUserPayload, User } from '../models/user.model';
import { MOCK_USERS } from '../mocks/mock-users';

const AVATAR_COLORS = ['#008BD2', '#043962', '#7C3AED'];

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly usersSubject = new BehaviorSubject<User[]>(
    environment.useMockData ? structuredClone(MOCK_USERS) : []
  );
  readonly users$ = this.usersSubject.asObservable();

  constructor(private http: HttpClient) {}

  list(): Observable<User[]> {
    if (environment.useMockData) {
      return this.users$.pipe(delay(environment.mockLatencyMs));
    }
    return this.http.get<User[]>(`${environment.apiBaseUrl}/users`);
  }

  create(payload: CreateUserPayload): Observable<User> {
    if (environment.useMockData) {
      const initials = `${payload.firstName[0] ?? ''}${payload.lastName[0] ?? ''}`.toUpperCase();
      const user: User = {
        id: `u-${Date.now()}`,
        firstName: payload.firstName,
        lastName: payload.lastName,
        email: payload.email,
        role: payload.role,
        status: 'Active',
        availability: 'Available',
        activeTickets: 0,
        initials,
        avatarColor: AVATAR_COLORS[this.usersSubject.value.length % AVATAR_COLORS.length],
        createdAt: new Date().toISOString().slice(0, 10),
      };
      this.usersSubject.next([...this.usersSubject.value, user]);
      return of(user).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.post<User>(`${environment.apiBaseUrl}/users`, payload);
  }

  update(id: string, payload: UpdateUserPayload): Observable<User> {
    if (environment.useMockData) {
      const users = this.usersSubject.value;
      const idx = users.findIndex((u) => u.id === id);
      if (idx === -1) return throwError(() => new Error('User not found.')).pipe(delay(environment.mockLatencyMs));
      const updated = { ...users[idx], ...payload };
      const next = [...users];
      next[idx] = updated;
      this.usersSubject.next(next);
      return of(updated).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.put<User>(`${environment.apiBaseUrl}/users/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    if (environment.useMockData) {
      this.usersSubject.next(this.usersSubject.value.filter((u) => u.id !== id));
      return of(void 0).pipe(delay(environment.mockLatencyMs));
    }
    return this.http.delete<void>(`${environment.apiBaseUrl}/users/${id}`);
  }
}
