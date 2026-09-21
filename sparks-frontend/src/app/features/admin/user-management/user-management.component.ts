import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';
import { UserService } from '../../../core/services/user.service';
import { CreateUserPayload, User, UserRole } from '../../../core/models/user.model';
import { roleColor } from '../../../shared/utils/badge-colors';

@Component({
  selector: 'sp-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, BadgeComponent, IconComponent, ModalComponent, ConfirmModalComponent],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss',
})
export class UserManagementComponent implements OnInit {
  users = signal<User[]>([]);
  loading = signal(true);
  showAddModal = signal(false);
  saving = signal(false);
  deleteError = signal<string | null>(null);
  addError = signal<string | null>(null);
  userToDelete = signal<User | null>(null);
  deleting = signal(false);
  roleColor = roleColor;

  form: CreateUserPayload = { firstName: '', lastName: '', email: '', role: 'Generalist' };
  roles: UserRole[] = ['Generalist', 'Specialist', 'Admin', 'TeamLead', 'Polyvalent'];

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.userService.list().subscribe((users) => {
      this.users.set(users);
      this.loading.set(false);
    });
  }

  openAddModal(): void {
    this.form = { firstName: '', lastName: '', email: '', role: 'Generalist' };
    this.addError.set(null);
    this.showAddModal.set(true);
  }

  submitAdd(): void {
    if (!this.form.firstName || !this.form.lastName || !this.form.email) return;
    this.saving.set(true);
    this.addError.set(null);
    this.userService.create(this.form).subscribe({
      next: (user) => {
        this.users.set([...this.users(), user]);
        this.saving.set(false);
        this.showAddModal.set(false);
      },
      error: (err) => {
        this.saving.set(false);
        this.addError.set(err?.error?.message ?? 'Unable to create this user. Please try again.');
        // The account may still have been created even if this request errored
        // (e.g. the invitation email failed to send after the user was saved).
        this.userService.list().subscribe((users) => this.users.set(users));
      },
    });
  }

  toggleStatus(user: User): void {
    const status = user.status === 'Active' ? 'Inactive' : 'Active';
    this.userService.update(user.id, { status }).subscribe((updated) => {
      this.users.set(this.users().map((u) => (u.id === updated.id ? updated : u)));
    });
  }

  remove(user: User): void {
    this.deleteError.set(null);
    this.userToDelete.set(user);
  }

  confirmDelete(): void {
    const user = this.userToDelete();
    if (!user) return;
    this.deleting.set(true);
    this.userService.delete(user.id).subscribe({
      next: () => {
        this.users.set(this.users().filter((u) => u.id !== user.id));
        this.deleting.set(false);
        this.userToDelete.set(null);
      },
      error: (err) => {
        this.deleting.set(false);
        this.userToDelete.set(null);
        this.deleteError.set(err?.error?.message ?? 'Unable to delete this user. Please try again.');
      },
    });
  }
}
