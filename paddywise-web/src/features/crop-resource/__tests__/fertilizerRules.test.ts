import { describe, it, expect } from 'vitest';
import { getFertilizerRule, doaFertilizerRules } from '../data/fertilizerRules';

describe('Fertilizer DOA Rules (fertilizerRules) Agronomic Unit Tests', () => {
  it('contains valid DOA rules across Wet, Intermediate, and Dry zones', () => {
    expect(doaFertilizerRules.length).toBeGreaterThan(10);

    const wetZoneRules = doaFertilizerRules.filter(r => r.region === 'Wet');
    const dryZoneRules = doaFertilizerRules.filter(r => r.region === 'Dry');
    const interRules = doaFertilizerRules.filter(r => r.region === 'Intermediate');

    expect(wetZoneRules.length).toBeGreaterThan(0);
    expect(dryZoneRules.length).toBeGreaterThan(0);
    expect(interRules.length).toBeGreaterThan(0);
  });

  it('correctly retrieves Wet zone Tillering Urea recommendation (50 kg/ha)', () => {
    const rule = getFertilizerRule('Wet', 'Tillering', 'Urea');
    expect(rule).toBeDefined();
    expect(rule?.recommendedAmountKgPerHa).toBe(50);
    expect(rule?.sourceReference).toContain('DOA Sri Lanka');
  });

  it('correctly retrieves Dry zone Tillering Urea recommendation (80 kg/ha)', () => {
    const rule = getFertilizerRule('Dry', 'Tillering', 'Urea');
    expect(rule).toBeDefined();
    expect(rule?.recommendedAmountKgPerHa).toBe(80);
  });

  it('correctly retrieves Basal TSP recommendations', () => {
    const wetRule = getFertilizerRule('Wet', 'Basal', 'TSP');
    expect(wetRule?.recommendedAmountKgPerHa).toBe(55);

    const dryRule = getFertilizerRule('Dry', 'Basal', 'TSP');
    expect(dryRule?.recommendedAmountKgPerHa).toBe(75);
  });

  it('returns undefined when no matching rule exists for combination', () => {
    const rule = getFertilizerRule('Wet', 'Heading', 'TSP');
    expect(rule).toBeUndefined();
  });
});
