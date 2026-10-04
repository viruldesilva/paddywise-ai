import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import OfficerApprovalPage from '../OfficerApprovalPage';
import { adminService } from '../../services/adminService';
import { AuthContext } from '../../context/AuthContext';
import type { AuthContextType } from '../../context/AuthContext';
import type { OfficerRequestDto } from '../../types/auth';

describe('OfficerApprovalPage Component & Integration Tests', () => {
  const mockAdminUser = {
    id: 1,
    name: 'Admin Virul',
    email: 'admin@paddywise.lk',
    role: 'Admin' as const,
  };

  const mockAuthContext: AuthContextType = {
    user: mockAdminUser,
    token: 'admin_jwt',
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    clearAuth: vi.fn(),
  };

  const sampleRequests: OfficerRequestDto[] = [
    {
      id: 101,
      name: 'Dr. Nilmini Perera',
      email: 'nilmini.p@gov.lk',
      phone: '+94 77 123 4567',
      role: 'AgriculturalOfficer',
      accountStatus: 'PendingApproval',
      createdAt: '2026-10-01T08:00:00Z',
    },
    {
      id: 102,
      name: 'K. G. Sunil Jayasuriya',
      email: 'sunil.j@gov.lk',
      phone: '+94 71 987 6543',
      role: 'AgriculturalOfficer',
      accountStatus: 'PendingApproval',
      createdAt: '2026-10-02T09:30:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  const renderPage = () => {
    return render(
      <AuthContext.Provider value={mockAuthContext}>
        <MemoryRouter>
          <OfficerApprovalPage />
        </MemoryRouter>
      </AuthContext.Provider>
    );
  };

  describe('Component & Table Rendering', () => {
    it('renders page header and table rows with Approve and Reject buttons per item', async () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockResolvedValueOnce(sampleRequests);

      renderPage();

      // Heading and badge
      expect(screen.getByText('Officer Account Verification')).toBeInTheDocument();

      // Verify each officer's details are rendered
      expect(await screen.findByText('Dr. Nilmini Perera')).toBeInTheDocument();
      expect(screen.getByText('nilmini.p@gov.lk')).toBeInTheDocument();
      expect(screen.getByText('K. G. Sunil Jayasuriya')).toBeInTheDocument();

      // Verify Approve and Reject action buttons for each row
      const approveButtons = screen.getAllByRole('button', { name: /Approve/i });
      const rejectButtons = screen.getAllByRole('button', { name: /Reject/i });

      expect(approveButtons.length).toBeGreaterThanOrEqual(2);
      expect(rejectButtons.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('API Integration: Approve & Reject Endpoints', () => {
    it('clicking Approve calls approveOfficerRequest with the officer id and removes row', async () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockResolvedValueOnce(sampleRequests);
      const approveSpy = vi
        .spyOn(adminService, 'approveOfficerRequest')
        .mockResolvedValueOnce({ message: 'Officer approved' });

      renderPage();

      expect(await screen.findByText('Dr. Nilmini Perera')).toBeInTheDocument();

      // Click Approve on the first officer
      const approveButtons = screen.getAllByRole('button', { name: /Approve/i });
      fireEvent.click(approveButtons[0]);

      await waitFor(() => {
        expect(approveSpy).toHaveBeenCalledWith(101);
      });

      // Officer should be removed from the active pending list
      expect(screen.queryByText('nilmini.p@gov.lk')).not.toBeInTheDocument();
      expect(screen.getByText('K. G. Sunil Jayasuriya')).toBeInTheDocument();
    });

    it('clicking Reject opens confirmation modal, submits rejection with reason, and removes row', async () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockResolvedValueOnce(sampleRequests);
      const rejectSpy = vi
        .spyOn(adminService, 'rejectOfficerRequest')
        .mockResolvedValueOnce({ message: 'Officer rejected' });

      renderPage();

      expect(await screen.findByText('Dr. Nilmini Perera')).toBeInTheDocument();

      // Click Reject to open reject modal
      const rejectButtons = screen.getAllByRole('button', { name: /Reject/i });
      fireEvent.click(rejectButtons[0]);

      // Verify modal opened
      expect(screen.getByText('Reject Officer Application')).toBeInTheDocument();

      // Enter rejection reason
      const reasonInput = screen.getByPlaceholderText(/e\.g\. Unable to verify extension credentials/i);
      fireEvent.change(reasonInput, {
        target: { value: 'Invalid government registration ID provided.' },
      });

      // Confirm rejection
      const confirmButton = screen.getByRole('button', { name: /Confirm Rejection/i });
      fireEvent.click(confirmButton);

      await waitFor(() => {
        expect(rejectSpy).toHaveBeenCalledWith(101, 'Invalid government registration ID provided.');
      });

      // Row removed
      expect(screen.queryByText('nilmini.p@gov.lk')).not.toBeInTheDocument();
    });
  });

  describe('UI-State & Error-State Testing', () => {
    it('shows loading indicator while fetching pending requests', () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockImplementation(
        () => new Promise(() => {}) // Unresolved promise
      );

      renderPage();

      expect(screen.getByText(/Loading pending officer requests.../i)).toBeInTheDocument();
    });

    it('shows empty state when there are zero pending officer requests', async () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockResolvedValueOnce([]);

      renderPage();

      expect(
        await screen.findByText('All clear! No pending officer applications')
      ).toBeInTheDocument();
      expect(
        screen.getByText('There are currently no new Agricultural Officer registrations waiting for verification.')
      ).toBeInTheDocument();
    });

    it('shows error banner when API call fails with network/500 error', async () => {
      vi.spyOn(adminService, 'getPendingOfficerRequests').mockRejectedValueOnce(
        new Error('Network error 500')
      );

      renderPage();

      expect(
        await screen.findByText('Failed to load pending officer requests. Please try again.')
      ).toBeInTheDocument();
    });
  });
});
