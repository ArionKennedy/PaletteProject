import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import { useAuth, useSignIn } from '@clerk/expo';
import { SignInForm } from '@/components/sign-in-form';

// --- Mocks ---

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  router: { replace: mockRouterReplace },
}));

jest.mock('@clerk/expo', () => ({
  useAuth: jest.fn(),
  useSignIn: jest.fn(),
}));

// --- Helpers ---

// Default mock for a ready, idle Clerk sign-in session
function mockSignInReady() {
  (useAuth as jest.Mock).mockReturnValue({ isLoaded: true });
  (useSignIn as jest.Mock).mockReturnValue({
    signIn: {
      password: jest.fn().mockResolvedValue({ error: null }),
      finalize: jest.fn().mockImplementation(({ navigate }) => navigate()),
      status: 'complete',
    },
    errors: {},
    fetchStatus: 'idle',
  });
}

// --- Tests ---

describe('<SignInForm />', () => {
  beforeEach(() => {
    mockSignInReady();
  });

  test('renders the form title, inputs, and Continue button', () => {
    render(<SignInForm />);

    expect(screen.getByText('Palette Sign In')).toBeTruthy();
    expect(screen.getByPlaceholderText('m@example.com')).toBeTruthy();
    expect(screen.getByText('Continue')).toBeTruthy();
  });

  test('shows a loading spinner while Clerk is initialising', () => {
    (useAuth as jest.Mock).mockReturnValue({ isLoaded: false });

    render(<SignInForm />);

    // The form itself should not appear
    expect(screen.queryByText('Palette Sign In')).toBeNull();
  });

  test('Continue button is disabled when fields are empty', () => {
    render(<SignInForm />);

    const button = screen.getByText('Continue');
    expect(button).toBeDisabled();
  });

  test('shows an email validation error when the email is invalid', async () => {
    render(<SignInForm />);
    

    fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'notanemail');

    // Find the password input (second TextInput) and type something so the button enables
    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[1], 'password123');

    fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(screen.getByText(/valid email/i)).toBeTruthy();
    });
  });

  test('calls signIn.password with the entered credentials on submit', async () => {
    const mockPassword = jest.fn().mockResolvedValue({ error: null });
    const mockFinalize = jest.fn().mockImplementation(({ navigate }) => navigate());
    (useSignIn as jest.Mock).mockReturnValue({
      signIn: { password: mockPassword, finalize: mockFinalize, status: 'complete' },
      errors: {},
      fetchStatus: 'idle',
    });

    render(<SignInForm />);

    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[0], 'user@example.com');
    fireEvent.changeText(inputs[1], 'password123');
    fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(mockPassword).toHaveBeenCalledWith({
        emailAddress: 'user@example.com',
        password: 'password123',
      });
    });
  });

  test('navigates to movies after a successful sign in', async () => {
    const mockFinalize = jest.fn().mockImplementation(({ navigate }) => navigate());
    (useSignIn as jest.Mock).mockReturnValue({
      signIn: {
        password: jest.fn().mockResolvedValue({ error: null }),
        finalize: mockFinalize,
        status: 'complete',
      },
      errors: {},
      fetchStatus: 'idle',
    });

    render(<SignInForm />);

    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[0], 'user@example.com');
    fireEvent.changeText(inputs[1], 'password123');
    fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(mockRouterReplace).toHaveBeenCalledWith('/(app)/(tabs)/movies');
    });
  });

  test('pressing "Sign up" navigates to the signup screen', () => {
    render(<SignInForm />);

    fireEvent.press(screen.getByText('Sign up'));

    expect(mockRouterReplace).toHaveBeenCalledWith('/signup');
  });
});
