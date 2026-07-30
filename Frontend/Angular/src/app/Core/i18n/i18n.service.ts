import { Injectable, signal } from '@angular/core';
import { AppLang, TRANSLATIONS } from './translations';

const STORAGE_KEY = 'hr_lang';

@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly lang = signal<AppLang>(this.readStoredLang());

  t(key: string, params?: Record<string, string | number>): string {
    const table = TRANSLATIONS[this.lang()] ?? TRANSLATIONS.en;
    let value = table[key] ?? TRANSLATIONS.en[key] ?? key;
    if (params) {
      for (const [name, raw] of Object.entries(params)) {
        value = value.replaceAll(`{${name}}`, String(raw));
      }
    }
    return value;
  }

  setLang(lang: AppLang): void {
    if (lang !== 'en' && lang !== 'km') return;
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    document.documentElement.lang = lang === 'km' ? 'km' : 'en';
  }

  toggle(): void {
    this.setLang(this.lang() === 'en' ? 'km' : 'en');
  }

  private readStoredLang(): AppLang {
    const stored = localStorage.getItem(STORAGE_KEY);
    const lang: AppLang = stored === 'km' ? 'km' : 'en';
    document.documentElement.lang = lang === 'km' ? 'km' : 'en';
    return lang;
  }
}
