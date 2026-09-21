import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../../shared/components/icon/icon.component';
import { environment } from '../../../../environments/environment';

const STORAGE_KEY = 'sparks_settings';

interface PlatformSettings {
  emailNotifications: boolean;
  slackNotifications: boolean;
  autoEscalateOnSlaBreach: boolean;
  defaultPoolPageSize: number;
}

const DEFAULTS: PlatformSettings = {
  emailNotifications: true,
  slackNotifications: false,
  autoEscalateOnSlaBreach: true,
  defaultPoolPageSize: 8,
};

@Component({
  selector: 'sp-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  settings = signal<PlatformSettings>(this.load());
  saved = signal(false);
  useMockData = environment.useMockData;
  apiBaseUrl = environment.apiBaseUrl;

  private load(): PlatformSettings {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? { ...DEFAULTS, ...JSON.parse(raw) } : DEFAULTS;
  }

  toggle(key: keyof PlatformSettings): void {
    const current = this.settings();
    if (typeof current[key] !== 'boolean') return;
    this.settings.set({ ...current, [key]: !current[key] });
  }

  save(): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.settings()));
    this.saved.set(true);
    setTimeout(() => this.saved.set(false), 2000);
  }
}
