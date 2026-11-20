import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import { useAuth, useSignIn } from '@clerk/expo';
import { SignInForm } from '@/components/sign-in-form';
import { router } from 'expo-router';

// --- Mocks ---

jest.mock('expo-router', () => ({
  router: { replace: jest.fn() },
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



  
  test('renders the form title, inputs, and Continue button', async () => {
    await render(<SignInForm />);

    expect(screen.getByText('Palette Sign In')).toBeTruthy();
    expect(screen.getByPlaceholderText('m@example.com')).toBeTruthy();
    expect(screen.getByText('Continue')).toBeTruthy();
  });



  test('shows a loading spinner while Clerk is initialising', async () => {
    (useAuth as jest.Mock).mockReturnValue({ isLoaded: false });

    await render(<SignInForm />);

    // The form itself should not appear
    expect(screen.queryByText('Palette Sign In')).toBeNull();
  });



  test('Continue button is disabled when fields are empty', async () => {
    await render(<SignInForm />);

    const button = screen.getByText('Continue');
    expect(button).toBeDisabled();
  });



  test('shows an email validation error when the email is invalid', async () => {
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'notanemail');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'password123');

    await fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(screen.getByText('Unable to Sign In')).toBeTruthy();
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

    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'password123');
    await fireEvent.press(screen.getByText('Continue'));

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

    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'password123');
    await fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(router.replace).toHaveBeenCalledWith('/(app)/(tabs)/movies');
    });
  });



  test('pressing "Sign up" navigates to the signup screen', async () => {
    await render(<SignInForm />);

    await fireEvent.press(screen.getByText('Sign up'));

    expect(router.replace).toHaveBeenCalledWith('/signup');
  });
  
});
