export type Region = 'Wet' | 'Intermediate' | 'Dry';
export type CropStage = 'Basal' | 'Tillering' | 'Panicle Initiation' | 'Heading';
export type FertilizerType = 'Urea' | 'TSP' | 'MOP' | 'Organic';

export interface FertilizerRule {
  region: Region;
  cropStage: CropStage;
  type: FertilizerType;
  recommendedAmountKgPerHa: number;
  sourceReference: string;
}

// Based on Department of Agriculture Sri Lanka standard guidelines (mocked ranges for simulation)
export const doaFertilizerRules: FertilizerRule[] = [
  // WET ZONE
  { region: 'Wet', cropStage: 'Basal', type: 'TSP', recommendedAmountKgPerHa: 55, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Basal', type: 'MOP', recommendedAmountKgPerHa: 20, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Basal', type: 'Organic', recommendedAmountKgPerHa: 500, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Tillering', type: 'Urea', recommendedAmountKgPerHa: 50, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Panicle Initiation', type: 'Urea', recommendedAmountKgPerHa: 60, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Panicle Initiation', type: 'MOP', recommendedAmountKgPerHa: 20, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },
  { region: 'Wet', cropStage: 'Heading', type: 'Urea', recommendedAmountKgPerHa: 30, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Wet Zone)' },

  // INTERMEDIATE ZONE
  { region: 'Intermediate', cropStage: 'Basal', type: 'TSP', recommendedAmountKgPerHa: 60, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },
  { region: 'Intermediate', cropStage: 'Basal', type: 'MOP', recommendedAmountKgPerHa: 25, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },
  { region: 'Intermediate', cropStage: 'Tillering', type: 'Urea', recommendedAmountKgPerHa: 65, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },
  { region: 'Intermediate', cropStage: 'Panicle Initiation', type: 'Urea', recommendedAmountKgPerHa: 70, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },
  { region: 'Intermediate', cropStage: 'Panicle Initiation', type: 'MOP', recommendedAmountKgPerHa: 25, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },
  { region: 'Intermediate', cropStage: 'Heading', type: 'Urea', recommendedAmountKgPerHa: 35, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Intermediate Zone)' },

  // DRY ZONE
  { region: 'Dry', cropStage: 'Basal', type: 'TSP', recommendedAmountKgPerHa: 75, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
  { region: 'Dry', cropStage: 'Basal', type: 'MOP', recommendedAmountKgPerHa: 30, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
  { region: 'Dry', cropStage: 'Tillering', type: 'Urea', recommendedAmountKgPerHa: 80, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
  { region: 'Dry', cropStage: 'Panicle Initiation', type: 'Urea', recommendedAmountKgPerHa: 85, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
  { region: 'Dry', cropStage: 'Panicle Initiation', type: 'MOP', recommendedAmountKgPerHa: 30, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
  { region: 'Dry', cropStage: 'Heading', type: 'Urea', recommendedAmountKgPerHa: 40, sourceReference: 'DOA Sri Lanka - 2023 Fertilizer Recommendation for Paddy (Dry Zone)' },
];

export const getFertilizerRule = (region: string, cropStage: string, type: string): FertilizerRule | undefined => {
  return doaFertilizerRules.find(
    r => r.region === region && r.cropStage === cropStage && r.type === type
  );
};
