import React, { createContext, useContext, useState, type ReactNode } from 'react';

import {
  detectLanguage,
  getTranslation,
  interpolate,
  type Language,
} from '@/localization/translations';

interface I18nContextValue {
  language: Language;
  setLanguage: (language: Language) => void;
  toggleLanguage: () => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  formatDate: (value: string | number | Date, options?: Intl.DateTimeFormatOptions) => string;
  formatDisplayName: (displayName?: string | null) => string;
}

const I18nContext = createContext<I18nContextValue | null>(null);

function getRuntimeLocaleTag(): string {
  try {
    // Rely on JS-side Intl first to avoid native module crashes
    return Intl.DateTimeFormat().resolvedOptions().locale ?? 'en-US';
  } catch {
    return 'en-US';
  }
}

function getInitialLanguage(): Language {
  const localeTag = getRuntimeLocaleTag();
  const languageCode = localeTag.split(/[-_]/)[0] ?? null;
  return detectLanguage(localeTag, languageCode);
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [language, setLanguage] = useState<Language>(getInitialLanguage);

  const t = (key: string, params?: Record<string, string | number>) => {
    const message = getTranslation(language, key) ?? getTranslation('en', key) ?? key;
    return interpolate(message, params);
  };

  const formatDate = (value: string | number | Date, options?: Intl.DateTimeFormatOptions) => {
    const date = value instanceof Date ? value : new Date(value);
    const locale = language === 'mn' ? 'mn-MN' : 'en-US';
    return new Intl.DateTimeFormat(locale, options).format(date);
  };

  const formatDisplayName = (displayName?: string | null) => {
    if (!displayName) return t('common.guestName');

    const guestMatch = displayName.match(/^Guest\s+(.+)$/i);
    if (guestMatch?.[1]) {
      return t('common.guestWithCode', { code: guestMatch[1] });
    }

    return displayName;
  };

  return (
    <I18nContext.Provider
      value={{
        language,
        setLanguage,
        toggleLanguage: () => setLanguage((current) => (current === 'mn' ? 'en' : 'mn')),
        t,
        formatDate,
        formatDisplayName,
      }}
    >
      {children}
    </I18nContext.Provider>
  );
}

export function useI18n(): I18nContextValue {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error('useI18n must be used inside <I18nProvider>');
  return ctx;
}
