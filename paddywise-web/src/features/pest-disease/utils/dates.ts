/** "14 Mar 2026, 09:12" — for the DateTime values (CreatedAt, ReviewedAt) the API sends. */
export function formatDateTime(value: string | null): string {
  if (!value) return '—';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  return date.toLocaleString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}
