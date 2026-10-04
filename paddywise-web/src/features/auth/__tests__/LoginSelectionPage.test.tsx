import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import LoginSelectionPage from '../pages/LoginSelectionPage';

describe('LoginSelectionPage Component Tests', () => {
  it('renders portal title and description for Agricultural Officer and System Administrator', () => {
    render(
      <MemoryRouter>
        <LoginSelectionPage />
      </MemoryRouter>
    );

    // Verify main portal heading and subtitle
    expect(screen.getByText('Choose Portal')).toBeInTheDocument();
    expect(
      screen.getByText('Select your role to access your dedicated workspace.')
    ).toBeInTheDocument();

    // Verify Agricultural Officer selection option
    expect(screen.getByText('Agricultural Officer')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Field evaluations, cycle plan approvals, crop recommendations, and pest surveillance.'
      )
    ).toBeInTheDocument();

    // Verify System Administrator selection option
    expect(screen.getByText('System Administrator')).toBeInTheDocument();
    expect(
      screen.getByText(
        'Officer approval verification, access governance, user management, and audit inspection.'
      )
    ).toBeInTheDocument();
  });

  it('renders links and portal cards for both Officer and Admin logins', () => {
    render(
      <MemoryRouter>
        <LoginSelectionPage />
      </MemoryRouter>
    );

    // Verify links to respective login portals exist
    const officerLoginButton = screen.getByText('Agricultural Officer Sign in');
    expect(officerLoginButton).toBeInTheDocument();

    const adminLoginButton = screen.getByText('Administrator Sign in');
    expect(adminLoginButton).toBeInTheDocument();

    // Mobile app callout banner
    expect(screen.getByText('Are you a Registered Paddy Farmer?')).toBeInTheDocument();
    expect(
      screen.getByText('Download Kumburu for Android')
    ).toBeInTheDocument();
  });
});
