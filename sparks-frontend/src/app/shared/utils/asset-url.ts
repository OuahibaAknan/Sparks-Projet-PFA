import { environment } from '../../../environments/environment';

export function toAssetUrl(path: string | null | undefined): string | null {
  if (!path) return null;
  if (/^https?:\/\//.test(path)) return path;
  const origin = environment.apiBaseUrl.replace(/\/api\/?$/, '');
  return `${origin}${path}`;
}
