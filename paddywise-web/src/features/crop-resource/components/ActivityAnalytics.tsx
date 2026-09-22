import React, { useMemo } from 'react';
import type { CropActivityDto } from '../services/activityApi';
import { Activity, Droplets, Leaf, ShieldAlert } from 'lucide-react';

interface ActivityAnalyticsProps {
  activities: CropActivityDto[];
}

interface FertilizerTypeStat {
  totalQuantity: number;
  count: number;
  stages: string[];
}

const FERTILIZER_COLORS: Record<string, { bg: string; text: string; bar: string }> = {
  'Urea': { bg: 'rgba(46, 125, 50, 0.14)', text: '#2e7d32', bar: '#2e7d32' },
  'TSP': { bg: 'rgba(47, 105, 184, 0.14)', text: '#2F69B8', bar: '#2F69B8' },
  'MOP': { bg: 'rgba(225, 166, 59, 0.18)', text: '#C48A28', bar: '#E1A63B' },
  'Organic': { bg: 'rgba(127, 166, 108, 0.2)', text: '#182A1E', bar: '#7FA66C' },
  'Organic Compost': { bg: 'rgba(127, 166, 108, 0.2)', text: '#182A1E', bar: '#7FA66C' },
};

const DEFAULT_FERTILIZER_COLOR = { bg: 'rgba(75, 86, 69, 0.14)', text: '#4B5645', bar: '#7FA66C' };

export const ActivityAnalytics: React.FC<ActivityAnalyticsProps> = ({ activities }) => {
  const stats = useMemo(() => {
    let totalFertilizer = 0;
    let totalWaterDuration = 0;
    let irrigationCount = 0;
    let fertilizerCount = 0;
    let pesticideCount = 0;
    let otherCount = 0;
    const fertilizerByType: Record<string, FertilizerTypeStat> = {};

    activities.forEach(activity => {
      try {
        const data = JSON.parse(activity.detailsJson || '{}');

        switch (activity.activityType) {
          case 'Fertilizer': {
            fertilizerCount++;
            const qty = Number(data.quantity) || 0;
            totalFertilizer += qty;
            const type = data.type?.trim() || 'Other';
            if (!fertilizerByType[type]) {
              fertilizerByType[type] = { totalQuantity: 0, count: 0, stages: [] };
            }
            fertilizerByType[type].count += 1;
            fertilizerByType[type].totalQuantity += qty;
            if (data.cropStage && !fertilizerByType[type].stages.includes(data.cropStage)) {
              fertilizerByType[type].stages.push(data.cropStage);
            }
            break;
          }
          case 'Irrigation':
            irrigationCount++;
            if (data.duration) totalWaterDuration += Number(data.duration);
            break;
          case 'Pesticide':
            pesticideCount++;
            break;
          case 'Other':
            otherCount++;
            break;
        }
      } catch (e) {
        // ignore parse errors
      }
    });

    const total = activities.length;

    return {
      total,
      totalFertilizer,
      totalWaterDuration,
      irrigationCount,
      fertilizerCount,
      pesticideCount,
      otherCount,
      fertilizerByType,
      percentages: {
        irrigation: total ? (irrigationCount / total) * 100 : 0,
        fertilizer: total ? (fertilizerCount / total) * 100 : 0,
        pesticide: total ? (pesticideCount / total) * 100 : 0,
        other: total ? (otherCount / total) * 100 : 0,
      }
    };
  }, [activities]);

  if (activities.length === 0) {
    return null;
  }

  const fertilizerTypeEntries = Object.entries(stats.fertilizerByType);

  return (
    <div className="activity-analytics" style={{ marginBottom: '2.5rem' }}>
      <h2 className="activity-title" style={{ fontSize: '1.25rem', marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <Activity size={20} className="text-forest" />
        Cycle Analysis & Insights
      </h2>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '1.5rem', marginBottom: '2rem' }}>
        {/* Stat Card 1: Total Fertilizer */}
        <div style={{ background: 'var(--cream-deep)', border: '1px solid var(--line)', borderRadius: '12px', padding: '1.5rem', display: 'flex', alignItems: 'flex-start', gap: '1rem' }}>
          <div style={{ background: 'rgba(127, 166, 108, 0.15)', color: 'var(--forest-deep)', padding: '0.75rem', borderRadius: '12px' }}>
            <Leaf size={24} />
          </div>
          <div style={{ flex: 1 }}>
            <div style={{ color: 'var(--ink-soft)', fontSize: '0.85rem', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.25rem' }}>Total Fertilizer</div>
            <div style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--ink)' }}>{stats.totalFertilizer}<span style={{ fontSize: '1rem', color: 'var(--ink-soft)', marginLeft: '4px' }}>kg/ha</span></div>
            <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>Over {stats.fertilizerCount} applications</div>
            {fertilizerTypeEntries.length > 0 && (
              <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap', marginTop: '0.6rem' }}>
                {fertilizerTypeEntries.map(([type, item]) => (
                  <span key={type} style={{ fontSize: '0.75rem', background: 'var(--cream)', padding: '0.15rem 0.45rem', borderRadius: '4px', border: '1px solid var(--line)', color: 'var(--forest-deep)', fontWeight: 600 }}>
                    {type}: {item.totalQuantity} kg/ha
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Stat Card 2: Irrigation Time */}
        <div style={{ background: 'var(--cream-deep)', border: '1px solid var(--line)', borderRadius: '12px', padding: '1.5rem', display: 'flex', alignItems: 'flex-start', gap: '1rem' }}>
          <div style={{ background: 'rgba(82, 137, 214, 0.1)', color: '#2F69B8', padding: '0.75rem', borderRadius: '12px' }}>
            <Droplets size={24} />
          </div>
          <div>
            <div style={{ color: 'var(--ink-soft)', fontSize: '0.85rem', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.25rem' }}>Irrigation Time</div>
            <div style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--ink)' }}>{stats.totalWaterDuration}<span style={{ fontSize: '1rem', color: 'var(--ink-soft)', marginLeft: '4px' }}>hrs</span></div>
            <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>Across {stats.irrigationCount} sessions</div>
          </div>
        </div>

        {/* Stat Card 3: Pest Control */}
        <div style={{ background: 'var(--cream-deep)', border: '1px solid var(--line)', borderRadius: '12px', padding: '1.5rem', display: 'flex', alignItems: 'flex-start', gap: '1rem' }}>
          <div style={{ background: 'rgba(225, 166, 59, 0.15)', color: 'var(--gold-deep)', padding: '0.75rem', borderRadius: '12px' }}>
            <ShieldAlert size={24} />
          </div>
          <div>
            <div style={{ color: 'var(--ink-soft)', fontSize: '0.85rem', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.25rem' }}>Pest Control</div>
            <div style={{ fontSize: '1.75rem', fontWeight: 700, color: 'var(--ink)' }}>{stats.pesticideCount}</div>
            <div style={{ fontSize: '0.85rem', color: 'var(--ink-soft)', marginTop: '0.25rem' }}>Treatments recorded</div>
          </div>
        </div>
      </div>

      {/* Breakdown by Fertilizer Type */}
      {fertilizerTypeEntries.length > 0 && (
        <div style={{ background: 'var(--cream-deep)', border: '1px solid var(--line)', borderRadius: '12px', padding: '1.5rem', marginBottom: '2rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '0.5rem' }}>
            <h3 style={{ fontSize: '1rem', color: 'var(--ink)', margin: 0, fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Leaf size={18} color="var(--forest)" />
              Fertilizer Breakdown by Type
            </h3>
            <span style={{ fontSize: '0.85rem', color: 'var(--ink-soft)' }}>
              Total: <strong style={{ color: 'var(--ink)' }}>{stats.totalFertilizer} kg/ha</strong> across {stats.fertilizerCount} applications
            </span>
          </div>

          {/* Proportional Bar */}
          {stats.totalFertilizer > 0 && (
            <div style={{ display: 'flex', height: '10px', borderRadius: '100px', overflow: 'hidden', marginBottom: '1.25rem', background: 'var(--line)' }}>
              {fertilizerTypeEntries.map(([type, item]) => {
                const pct = (item.totalQuantity / stats.totalFertilizer) * 100;
                if (pct <= 0) return null;
                const color = FERTILIZER_COLORS[type]?.bar || DEFAULT_FERTILIZER_COLOR.bar;
                return (
                  <div
                    key={type}
                    style={{ width: `${pct}%`, background: color }}
                    title={`${type}: ${item.totalQuantity} kg/ha (${Math.round(pct)}%)`}
                  />
                );
              })}
            </div>
          )}

          {/* Cards for each Fertilizer Type */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
            {fertilizerTypeEntries.map(([type, item]) => {
              const colors = FERTILIZER_COLORS[type] || DEFAULT_FERTILIZER_COLOR;
              const percentage = stats.totalFertilizer > 0 ? Math.round((item.totalQuantity / stats.totalFertilizer) * 100) : 0;

              return (
                <div 
                  key={type}
                  style={{ 
                    background: 'var(--cream)', 
                    border: '1px solid var(--line)', 
                    borderRadius: '10px', 
                    padding: '1.1rem',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'space-between',
                    gap: '0.6rem'
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span style={{ fontWeight: 600, color: 'var(--ink)', fontSize: '1rem' }}>{type}</span>
                    <span style={{ fontSize: '0.75rem', padding: '0.15rem 0.55rem', borderRadius: '100px', background: colors.bg, color: colors.text, fontWeight: 700 }}>
                      {percentage}%
                    </span>
                  </div>
                  <div>
                    <div style={{ fontSize: '1.45rem', fontWeight: 700, color: 'var(--ink)' }}>
                      {item.totalQuantity} <span style={{ fontSize: '0.85rem', fontWeight: 500, color: 'var(--ink-soft)' }}>kg/ha</span>
                    </div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--ink-soft)', marginTop: '0.2rem' }}>
                      {item.count} {item.count === 1 ? 'application' : 'applications'}
                    </div>
                  </div>
                  {item.stages.length > 0 && (
                    <div style={{ fontSize: '0.75rem', color: 'var(--ink-soft)', borderTop: '1px dashed var(--line)', paddingTop: '0.45rem', marginTop: '0.25rem' }}>
                      Applied at: <strong style={{ color: 'var(--forest)' }}>{item.stages.join(', ')}</strong>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Activity Distribution Bar */}
      <div style={{ background: 'var(--cream-deep)', border: '1px solid var(--line)', borderRadius: '12px', padding: '1.5rem' }}>
        <h3 style={{ fontSize: '1rem', color: 'var(--ink)', marginBottom: '1rem', fontWeight: 600 }}>Activity Distribution ({stats.total} Total)</h3>

        {/* Progress Bar Container */}
        <div style={{ display: 'flex', height: '12px', borderRadius: '100px', overflow: 'hidden', marginBottom: '1.5rem', background: 'var(--line)' }}>
          {stats.percentages.irrigation > 0 && <div style={{ width: `${stats.percentages.irrigation}%`, background: '#2F69B8' }} title="Irrigation"></div>}
          {stats.percentages.fertilizer > 0 && <div style={{ width: `${stats.percentages.fertilizer}%`, background: 'var(--forest-deep)' }} title="Fertilizer"></div>}
          {stats.percentages.pesticide > 0 && <div style={{ width: `${stats.percentages.pesticide}%`, background: 'var(--gold-deep)' }} title="Pesticide"></div>}
          {stats.percentages.other > 0 && <div style={{ width: `${stats.percentages.other}%`, background: 'var(--ink-soft)' }} title="Other"></div>}
        </div>

        {/* Legend */}
        <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem', color: 'var(--ink-soft)' }}>
            <div style={{ width: '12px', height: '12px', borderRadius: '3px', background: '#2F69B8' }}></div>
            Irrigation ({stats.irrigationCount})
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem', color: 'var(--ink-soft)' }}>
            <div style={{ width: '12px', height: '12px', borderRadius: '3px', background: 'var(--forest-deep)' }}></div>
            Fertilizer ({stats.fertilizerCount})
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem', color: 'var(--ink-soft)' }}>
            <div style={{ width: '12px', height: '12px', borderRadius: '3px', background: 'var(--gold-deep)' }}></div>
            Pesticide ({stats.pesticideCount})
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.85rem', color: 'var(--ink-soft)' }}>
            <div style={{ width: '12px', height: '12px', borderRadius: '3px', background: 'var(--ink-soft)' }}></div>
            Other ({stats.otherCount})
          </div>
        </div>
      </div>
    </div>
  );
};
