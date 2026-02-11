const ISO_DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;
const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;

export function toDateKey(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function parseIsoDate(value: string | null | undefined): Date | null {
  const raw = String(value || '').trim();
  if (!raw) {
    return null;
  }

  const match = ISO_DATE_PATTERN.exec(raw);
  if (match) {
    return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
  }

  const parsed = new Date(raw);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function toIsoDate(value: unknown): string | null {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    if (Number.isNaN(value.getTime())) {
      return null;
    }

    return toDateKey(value);
  }

  const raw = String(value).trim();
  if (!raw) {
    return null;
  }

  if (ISO_DATE_PATTERN.test(raw)) {
    return raw;
  }

  const parsed = new Date(raw);
  return Number.isNaN(parsed.getTime()) ? null : toDateKey(parsed);
}

export function parseTime(value: string | null | undefined): Date | null {
  const raw = String(value || '').trim();
  if (!raw) {
    return null;
  }

  const match = TIME_PATTERN.exec(raw);
  if (!match) {
    return null;
  }

  const date = new Date();
  date.setHours(Number(match[1]), Number(match[2]), 0, 0);
  return date;
}

export function toTimeString(value: unknown): string | null {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    if (Number.isNaN(value.getTime())) {
      return null;
    }

    const hours = String(value.getHours()).padStart(2, '0');
    const minutes = String(value.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  const raw = String(value).trim();
  return TIME_PATTERN.test(raw) ? raw : null;
}

export function toTimeInputValue(date: Date): string {
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

export function combineDateAndTime(date: Date, time: string): Date {
  const match = TIME_PATTERN.exec((time || '').trim());
  const hours = match ? Number(match[1]) : 0;
  const minutes = match ? Number(match[2]) : 0;

  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
    hours,
    minutes,
    0,
    0
  );
}

export function isValidTime(value: string): boolean {
  return TIME_PATTERN.test((value || '').trim());
}
