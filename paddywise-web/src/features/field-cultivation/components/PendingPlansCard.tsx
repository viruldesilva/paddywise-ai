import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getPendingPlans } from '../services/fieldApi';

/** What one read of the queue produced: a count, or the reason there isn't one. */
type CountState = { status: 'loading' } | { status: 'ok'; count: number } | { status: 'error' };

/**
 * The officer dashboard's approval tile: how many cultivation plans are actually
 * waiting on them, and the way into the queue.
 *
 * GET /api/plans/pending is AgriculturalOfficer only, so a caller in any other
 * role gets a 403 — the tile then says nothing rather than a wrong number. That
 * happens on /dashboard/officer, which any role may open as a role preview.
 */
export function PendingPlansCard() {
  const [state, setState] = useState<CountState>({ status: 'loading' });

  useEffect(() => {
    let isMounted = true;

    getPendingPlans()
      .then((rows) => {
        if (isMounted) setState({ status: 'ok', count: rows.length });
      })
      .catch(() => {
        if (isMounted) setState({ status: 'error' });
      });

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <div className="metric-card">
      <span className="metric-label">Plans Awaiting Approval</span>
      <span className="metric-value" style={{ color: '#d97706' }}>
        {state.status === 'ok' ? state.count : state.status === 'loading' ? '…' : '—'}
      </span>
      {state.status === 'error' ? (
        <span className="metric-sub">Count unavailable</span>
      ) : (
        <Link className="metric-sub" to="/plans/pending">
          Review the queue
        </Link>
      )}
    </div>
  );
}
