import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import { useAuth, useSignUp } from '@clerk/expo';
import { SignUpForm } from '@/components/sign-up-form';
import { router } from 'expo-router';

// --- Mocks ---

jest.mock('expo-router', () => ({
  router: { replace: jest.fn() },
}));

jest.mock('@clerk/expo', () => ({
  useAuth: jest.fn(),
  useSignUp: jest.fn(),
}));

// --- Helpers ---

function mockSignUpReady() {
  (useAuth as jest.Mock).mockReturnValue({ isLoaded: true });
  (useSignUp as jest.Mock).mockReturnValue({
    signUp: {
      password: jest.fn().mockResolvedValue({ error: null }),
      finalize: jest.fn().mockImplementation(({ navigate }) => navigate()),
      status: 'complete',
    },
    errors: {},
    fetchStatus: 'idle',
  });
}

// --- Tests ---

describe('<SignUpForm />', () => {
  beforeEach(() => {
    mockSignUpReady();
  });



  test('renders the form title, inputs, and Continue button', async () => {

    await render(<SignUpForm />);

    expect(screen.getByText('Create Your Palette Account')).toBeTruthy();
    expect(screen.getByPlaceholderText('m@example.com')).toBeTruthy();
    expect(screen.getByText('Continue')).toBeTruthy();
  });



  test('shows a loading spinner while Clerk is initialising', async () => {
    (useAuth as jest.Mock).mockReturnValue({ isLoaded: false });

    await render(<SignUpForm />);

    expect(screen.queryByText('Create Your Palette Account')).toBeNull();
  });



  test('Continue button is disabled when fields are empty', async () => {
    await render(<SignUpForm />);

    expect(screen.getByText('Continue')).toBeDisabled();
  });



  test('shows an email validation error when the email is invalid', async () => {
    await render(<SignUpForm />);

    await fireEvent.changeText(screen.getByTestId('email-input'), 'notanemail');
    await fireEvent.changeText(screen.getByTestId('password-input'), 'password123');

    await fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(screen.getByText('Unable to Sign Up')).toBeTruthy();
    });
  });



  test('calls signUp.password with the entered credentials on submit', async () => {
    const mockPassword = jest.fn().mockResolvedValue({ error: null });
    const mockFinalize = jest.fn().mockImplementation(({ navigate }) => navigate());
    (useSignUp as jest.Mock).mockReturnValue({
      signUp: { password: mockPassword, finalize: mockFinalize, status: 'complete' },
      errors: {},
      fetchStatus: 'idle',
    });

    await render(<SignUpForm />);

    await fireEvent.changeText(screen.getByTestId('email-input'), 'user@example.com');
    await fireEvent.changeText(screen.getByTestId('username-input'), 'myusername');
    await fireEvent.changeText(screen.getByTestId('password-input'), 'password123');
    await fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(mockPassword).toHaveBeenCalledWith({
        emailAddress: 'user@example.com',
        username: 'myusername',
        password: 'password123',
      });
    });
  });



  test('navigates to movies after a successful sign up', async () => {
    const mockFinalize = jest.fn().mockImplementation(({ navigate }) => navigate());
    (useSignUp as jest.Mock).mockReturnValue({
      signUp: {
        password: jest.fn().mockResolvedValue({ error: null }),
        finalize: mockFinalize,
        status: 'complete',
      },
      errors: {},
      fetchStatus: 'idle',
    });

    await render(<SignUpForm />);

    await fireEvent.changeText(screen.getByTestId('email-input'), 'user@example.com');
    await fireEvent.changeText(screen.getByTestId('username-input'), 'myusername');
    await fireEvent.changeText(screen.getByTestId('password-input'), 'password123');
    await fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(router.replace).toHaveBeenCalledWith('/(app)/(tabs)/movies');
    });
  });


  
  test('pressing "Sign in" navigates to the login screen', async () => {
    await render(<SignUpForm />);

    await fireEvent.press(screen.getByText('Sign in'));

    expect(router.replace).toHaveBeenCalledWith('/login');
  });
});
