/**
 * Calendar arithmetic for the "YYYY-MM-DD" strings the API sends for DateOnly.
 *
 * Everything works in UTC: a DateOnly carries no zone, so parsing it as local
 * midnight would shift the day for anyone west of Greenwich and move the
 * timeline's today-marker onto the wrong stage.
 */
import type { IsoDate } from '../types';

const MS_PER_DAY = 86_400_000;

/** Midnight UTC on an ISO calendar date, or NaN-valued Date when unparseable. */
export function parseIsoDate(value: IsoDate): Date {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return new Date(Number.NaN);

  return new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
}

export function isValidIsoDate(value: IsoDate): boolean {
  return !Number.isNaN(parseIsoDate(value).getTime());
}

/** The UTC date as "YYYY-MM-DD". */
export function toIsoDate(date: Date): IsoDate {
  return date.toISOString().slice(0, 10);
}

/** Today in UTC, in the form a date input and the API both accept. */
export function todayIso(): IsoDate {
  return toIsoDate(new Date());
}

export function addDays(value: IsoDate, days: number): IsoDate {
  const date = parseIsoDate(value);
  if (Number.isNaN(date.getTime())) return value;

  return toIsoDate(new Date(date.getTime() + days * MS_PER_DAY));
}

/** Whole days from `from` to `to`; negative when `to` is earlier. */
export function daysBetween(from: IsoDate, to: IsoDate): number {
  return Math.round((parseIsoDate(to).getTime() - parseIsoDate(from).getTime()) / MS_PER_DAY);
}

/** "14 Mar 2026" — short enough for a table cell, unambiguous about the month. */
export function formatDate(value: IsoDate | null): string {
  if (!value) return '—';

  const date = parseIsoDate(value);
  if (Number.isNaN(date.getTime())) return value;

  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  });
}
