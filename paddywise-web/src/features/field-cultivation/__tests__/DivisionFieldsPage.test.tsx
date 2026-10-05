import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import DivisionFieldsPage from '../pages/DivisionFieldsPage';
import { getDivisions, getFieldsByDivision } from '../services/fieldApi';
import { useAuth } from '../../../hooks/useAuth';
import { apiError, authAs, divisions, makeField } from './fixtures';

vi.mock('../services/fieldApi', () => ({ getDivisions: vi.fn(), getFieldsByDivision: vi.fn() }));
vi.mock('../../../hooks/useAuth', () => ({ useAuth: vi.fn() }));
vi.mock('../../../components/Sidebar', () => ({ Sidebar: () => null }));

const mockedDivisions = vi.mocked(getDivisions);
const mockedFields = vi.mocked(getFieldsByDivision);

function renderPage(path = '/officer/fields') {
  render(
    <MemoryRouter initialEntries={[path]}>
      <DivisionFieldsPage />
    </MemoryRouter>
  );
  return userEvent.setup();
}

describe('DivisionFieldsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuth).mockReturnValue(authAs('AgriculturalOfficer'));
  });

  it('shows a loading state while divisions load', () => {
    mockedDivisions.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText('Loading divisions…')).toBeInTheDocument();
  });

  it('shows the error state when divisions cannot be read', async () => {
    mockedDivisions.mockRejectedValue(apiError(500, { message: 'Database unavailable.' }));
    renderPage();
    expect(await screen.findByRole('alert')).toHaveTextContent('Database unavailable.');
  });

  it('shows the empty state when no divisions exist', async () => {
    mockedDivisions.mockResolvedValue([]);
    renderPage();
    expect(await screen.findByText('No divisions set up')).toBeInTheDocument();
  });

  it('loads the first division by default and shows its fields', async () => {
    mockedDivisions.mockResolvedValue(divisions);
    mockedFields.mockResolvedValue([makeField(), makeField({ id: 5, name: 'Upper paddy' })]);
    renderPage();

    expect(await screen.findByText('Upper paddy')).toBeInTheDocument();
    expect(screen.getByText('Lower paddy')).toBeInTheDocument();
    expect(mockedFields).toHaveBeenCalledWith(1);
  });

  it('a division with no fields shows its empty state', async () => {
    mockedDivisions.mockResolvedValue(divisions);
    mockedFields.mockResolvedValue([]);
    renderPage();
    expect(await screen.findByText('No fields in this division yet')).toBeInTheDocument();
  });

  it('picking another division loads its fields', async () => {
    mockedDivisions.mockResolvedValue(divisions);
    mockedFields.mockResolvedValue([]);
    const user = renderPage();
    await screen.findByText('No fields in this division yet');

    mockedFields.mockResolvedValue([makeField({ id: 7, name: 'Ampara tract', divisionId: 2 })]);
    await user.selectOptions(screen.getByLabelText('Division'), '2');

    expect(await screen.findByText('Ampara tract')).toBeInTheDocument();
    await waitFor(() => expect(mockedFields).toHaveBeenLastCalledWith(2));
  });

  it('a fields error shows the server message', async () => {
    mockedDivisions.mockResolvedValue(divisions);
    mockedFields.mockRejectedValue(apiError(403, { message: 'You do not have access to this division.' }));
    renderPage();
    expect(await screen.findByRole('alert')).toHaveTextContent('You do not have access to this division.');
  });
});
