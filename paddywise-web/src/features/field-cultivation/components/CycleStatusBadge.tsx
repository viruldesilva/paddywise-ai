import type { CycleStatus } from '../types';

/** One class per status so the badge colour comes from the stylesheet, not inline styles. */
const STATUS_CLASS: Record<CycleStatus, string> = {
  Planned: 'fc-badge fc-badge-planned',
  Active: 'fc-badge fc-badge-active',
  Harvested: 'fc-badge fc-badge-harvested',
  Abandoned: 'fc-badge fc-badge-abandoned',
};

export function CycleStatusBadge({ status }: { status: CycleStatus }) {
  return <span className={STATUS_CLASS[status] ?? 'fc-badge'}>{status}</span>;
}
