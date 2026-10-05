import { describe, it, expect } from 'vitest';
import { doaFertilizerRules, getFertilizerRule } from '../data/fertilizerRules';

describe('DOA fertilizer rules', () => {
  it('has one rule per zone/stage/type combination', () => {
    const keys = doaFertilizerRules.map((r) => `${r.region}|${r.cropStage}|${r.type}`);
    expect(new Set(keys).size).toBe(keys.length);
    expect(doaFertilizerRules).toHaveLength(19); // Wet 7, Intermediate 6, Dry 6
  });

  it.each([
    ['Wet', 'Tillering', 'Urea', 50],
    ['Intermediate', 'Tillering', 'Urea', 65],
    ['Dry', 'Tillering', 'Urea', 80],
    ['Dry', 'Basal', 'TSP', 75],
    ['Dry', 'Panicle Initiation', 'MOP', 30],
    ['Wet', 'Basal', 'Organic', 500],
  ])('%s / %s / %s recommends %d kg/ha', (region, stage, type, kg) => {
    expect(getFertilizerRule(region, stage, type)?.recommendedAmountKgPerHa).toBe(kg);
  });

  it('every rule cites a DOA source', () => {
    for (const rule of doaFertilizerRules) {
      expect(rule.sourceReference).toMatch(/^DOA Sri Lanka/);
    }
  });

  it.each([
    ['Dry', 'Tillering', 'TSP'],                 // no top-dressing TSP rule
    ['Dry', 'PanicleInitiation', 'Urea'],        // ActivityForm's option value — the rules spell it 'Panicle Initiation'
    ['dry', 'Tillering', 'Urea'],                // case-sensitive
  ])('has no rule for %s / %s / %s', (region, stage, type) => {
    expect(getFertilizerRule(region, stage, type)).toBeUndefined();
  });
});
