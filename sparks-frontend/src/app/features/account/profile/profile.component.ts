import { Component, ElementRef, HostListener, ViewChild, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { AuthService } from '../../../core/services/auth.service';
import { roleColor } from '../../../shared/utils/badge-colors';
import { toAssetUrl } from '../../../shared/utils/asset-url';

@Component({
  selector: 'sp-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink, IconComponent, BadgeComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent {
  @ViewChild('languageDropdown') languageDropdown?: ElementRef<HTMLElement>;

  available = signal(true);
  roleColor = roleColor;
  toAssetUrl = toAssetUrl;

  firstName = '';
  lastName = '';
  idStellantisControl = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  emailStellantis = '';
  altenId = '';
  isActive = true;

  availableLanguages = [
    'French',
    'English',
    'German',
    'Spanish',
    'Italian',
    'Portuguese',
    'Dutch',
    'Polish',
    'Romanian',
    'Arabic',
    'Turkish',
  ];
  selectedLanguages: string[] = [];
  langDropdownOpen = signal(false);

  saving = signal(false);
  saveError = signal<string | null>(null);
  saveSuccess = signal(false);

  photoUploading = signal(false);
  photoError = signal<string | null>(null);
  avatarPreview = signal<string | null>(null);
  cropImage = signal<string | null>(null);
  cropZoom = signal(1);
  cropModalOpen = signal(false);
  expandedImageUrl = signal<string | null>(null);

  constructor(public auth: AuthService) {
    effect(() => {
      const user = this.auth.currentUser();
      if (user) {
        this.firstName = user.firstName;
        this.lastName = user.lastName;
        this.idStellantisControl.setValue(user.idStellantis ?? '');
        this.emailStellantis = user.emailStellantis ?? '';
        this.altenId = user.altenId ?? '';
        this.isActive = user.status === 'Active';
        this.selectedLanguages = [...user.languages];
      }
    });
  }

  saveInfo(): void {
    this.saveError.set(null);
    this.saveSuccess.set(false);

    if (!this.firstName.trim() || !this.lastName.trim() || this.idStellantisControl.invalid) {
      this.idStellantisControl.markAsTouched();
      this.saveError.set('First name, last name and Stellantis ID are required.');
      return;
    }

    this.saving.set(true);
    this.auth
      .updateProfile({
        firstName: this.firstName.trim(),
        lastName: this.lastName.trim(),
        idStellantis: this.idStellantisControl.value.trim(),
        emailStellantis: this.emailStellantis.trim() || null,
        altenId: this.altenId.trim() || null,
        languages: this.selectedLanguages,
        isActive: this.isActive,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.saveSuccess.set(true);
        },
        error: (err) => {
          this.saving.set(false);
          this.saveError.set(err?.error?.message ?? 'Unable to save your changes. Please try again.');
        },
      });
  }

  toggleLanguage(lang: string): void {
    this.selectedLanguages = this.selectedLanguages.includes(lang)
      ? this.selectedLanguages.filter((l) => l !== lang)
      : [...this.selectedLanguages, lang];
  }

  toggleLangDropdown(): void {
    this.langDropdownOpen.set(!this.langDropdownOpen());
  }

  @HostListener('document:click', ['$event'])
  closeLangDropdownOnOutsideClick(event: MouseEvent): void {
    if (this.langDropdownOpen() && !this.languageDropdown?.nativeElement.contains(event.target as Node)) {
      this.langDropdownOpen.set(false);
    }
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.photoError.set('Please select an image file.');
      input.value = '';
      return;
    }

    this.photoError.set(null);
    const reader = new FileReader();
    reader.onload = () => {
      this.cropImage.set(reader.result as string);
      this.cropZoom.set(1);
      this.cropModalOpen.set(true);
    };
    reader.onerror = () => this.photoError.set('Unable to read the selected image.');
    reader.readAsDataURL(file);
    input.value = '';
  }

  cancelCrop(): void {
    this.cropModalOpen.set(false);
    this.cropImage.set(null);
  }

  confirmCrop(): void {
    const source = this.cropImage();
    if (!source) return;

    const image = new Image();
    image.onload = () => {
      const size = 400;
      const canvas = document.createElement('canvas');
      canvas.width = size;
      canvas.height = size;
      const context = canvas.getContext('2d');
      if (!context) return;

      const scale = Math.max(size / image.naturalWidth, size / image.naturalHeight) * this.cropZoom();
      const width = image.naturalWidth * scale;
      const height = image.naturalHeight * scale;
      context.drawImage(image, (size - width) / 2, (size - height) / 2, width, height);

      const preview = canvas.toDataURL('image/jpeg', 0.9);
      this.avatarPreview.set(preview);
      this.cropModalOpen.set(false);
      this.cropImage.set(null);
      this.uploadCroppedPhoto(canvas);
    };
    image.onerror = () => this.photoError.set('Unable to process the selected image.');
    image.src = source;
  }

  avatarImageUrl(profilePhotoUrl: string | null): string | null {
    return this.avatarPreview() ?? this.toAssetUrl(profilePhotoUrl);
  }

  openAvatarPreview(): void {
    const currentUser = this.auth.currentUser();
    const url = this.avatarImageUrl(currentUser?.profilePhotoUrl ?? null);
    if (url) {
      this.expandedImageUrl.set(url);
    }
  }

  closeAvatarPreview(): void {
    this.expandedImageUrl.set(null);
  }

  private uploadCroppedPhoto(canvas: HTMLCanvasElement): void {
    canvas.toBlob((blob) => {
      if (!blob) {
        this.photoError.set('Unable to prepare the cropped image.');
        return;
      }

      this.photoUploading.set(true);
      this.auth.uploadProfilePhoto(new File([blob], 'profile-photo.jpg', { type: 'image/jpeg' })).subscribe({
        next: () => this.photoUploading.set(false),
        error: (err) => {
          this.photoUploading.set(false);
          this.photoError.set(err?.error?.message ?? 'Unable to upload photo. Please try again.');
        },
      });
    }, 'image/jpeg', 0.9);
  }
}
