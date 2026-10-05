import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import FieldDetailPage from '../pages/FieldDetailPage';
import CycleDetailPage from '../pages/CycleDetailPage';
import { getCycleById, getFieldById, getMyCycles, getPlansForCycle } from '../services/fieldApi';
import { useAuth } from '../../../hooks/useAuth';
import type { UserRole } from '../../../types/auth';
import { apiError, authAs, makeCycle, makeField, makePlan } from './fixtures';

vi.mock('../services/fieldApi', () => ({
  getFieldById: vi.fn(),
  getMyCycles: vi.fn(),
  getCycleById: vi.fn(),
  getPlansForCycle: vi.fn(),
  logStage: vi.fn(),
}));
vi.mock('../../../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../../../components/Sidebar', () => ({ Sidebar: () => null }));

const api = {
  getFieldById: vi.mocked(getFieldById),
  getMyCycles: vi.mocked(getMyCycles),
  getCycleById: vi.mocked(getCycleById),
  getPlansForCycle: vi.mocked(getPlansForCycle),
};

function renderAt(path: string, role: UserRole = 'AgriculturalOfficer') {
  vi.mocked(useAuth).mockReturnValue(authAs(role));
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/fields/:id" element={<FieldDetailPage />} />
        <Route path="/cycles/:id" element={<CycleDetailPage />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('FieldDetailPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it('shows a loading state, then the field and its cycles', async () => {
    api.getFieldById.mockResolvedValue(makeField());
    api.getMyCycles.mockResolvedValue([makeCycle()]);
    renderAt('/fields/4');

    expect(screen.getByText('Loading this field…')).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Lower paddy' })).toBeInTheDocument();
    expect(screen.getByText(/farmed by Sunil Perera/)).toBeInTheDocument();
    expect(api.getMyCycles).toHaveBeenCalledWith(4);
  });

  it('a field with no cycles shows its empty state', async () => {
    api.getFieldById.mockResolvedValue(makeField());
    api.getMyCycles.mockResolvedValue([]);
    renderAt('/fields/4');
    expect(await screen.findByText('No cycles on this field yet')).toBeInTheDocument();
  });

  it.each([
    [403, 'You do not have access to this field.'],
    [404, 'Field not found.'],
  ])('a %i shows the server message', async (status, message) => {
    api.getFieldById.mockRejectedValue(apiError(status, { message }));
    api.getMyCycles.mockResolvedValue([]);
    renderAt('/fields/4');
    expect(await screen.findByRole('alert')).toHaveTextContent(message);
  });

  it('a non-numeric id is refused without calling the API', () => {
    renderAt('/fields/abc');
    expect(screen.getByRole('alert')).toHaveTextContent('That field id is not a number.');
    expect(api.getFieldById).not.toHaveBeenCalled();
  });
});

describe('CycleDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    api.getCycleById.mockResolvedValue(makeCycle());
    api.getPlansForCycle.mockResolvedValue([makePlan()]);
  });

  it('renders the cycle with its latest plan', async () => {
    renderAt('/cycles/12');
    expect(screen.getByText('Loading this cultivation cycle…')).toBeInTheDocument();
    expect(await screen.findByText('Rest of the season, from Tillering.')).toBeInTheDocument();
    expect(api.getCycleById).toHaveBeenCalledWith(12);
    expect(api.getPlansForCycle).toHaveBeenCalledWith(12);
  });

  it('an AgriculturalOfficer can log a stage', async () => {
    renderAt('/cycles/12', 'AgriculturalOfficer');
    expect(await screen.findByRole('button', { name: /log stage/i })).toBeInTheDocument();
  });

  it('an Admin cannot — the backend refuses Admin stage logs', async () => {
    renderAt('/cycles/12', 'Admin');
    await screen.findByText('Rest of the season, from Tillering.');
    expect(screen.queryByRole('button', { name: /log stage/i })).not.toBeInTheDocument();
  });

  it('a 404 shows the server message', async () => {
    api.getCycleById.mockRejectedValue(apiError(404, { message: 'Cultivation cycle not found.' }));
    renderAt('/cycles/12');
    expect(await screen.findByRole('alert')).toHaveTextContent('Cultivation cycle not found.');
  });
});
