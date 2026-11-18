import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import { useAuth, useSignUp } from '@clerk/expo';
import { SignUpForm } from '@/components/sign-up-form';

// --- Mocks ---

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  router: { replace: mockRouterReplace },
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

  test('renders the form title, inputs, and Continue button', () => {
    render(<SignUpForm />);

    expect(screen.getByText('Create Your Palette Account')).toBeTruthy();
    expect(screen.getByPlaceholderText('m@example.com')).toBeTruthy();
    expect(screen.getByText('Continue')).toBeTruthy();
  });

  test('shows a loading spinner while Clerk is initialising', () => {
    (useAuth as jest.Mock).mockReturnValue({ isLoaded: false });

    render(<SignUpForm />);

    expect(screen.queryByText('Create Your Palette Account')).toBeNull();
  });

  test('Continue button is disabled when fields are empty', () => {
    render(<SignUpForm />);

    expect(screen.getByText('Continue')).toBeDisabled();
  });

  test('shows an email validation error when the email is invalid', async () => {
    render(<SignUpForm />);

    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[0], 'notanemail');   // email
    fireEvent.changeText(inputs[1], 'myusername');   // username
    fireEvent.changeText(inputs[2], 'password123');  // password

    fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(screen.getByText(/valid email/i)).toBeTruthy();
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

    render(<SignUpForm />);

    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[0], 'user@example.com');
    fireEvent.changeText(inputs[1], 'myusername');
    fireEvent.changeText(inputs[2], 'password123');
    fireEvent.press(screen.getByText('Continue'));

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

    render(<SignUpForm />);

    const { TextInput } = require('react-native');
    const inputs = screen.UNSAFE_getAllByType(TextInput);
    fireEvent.changeText(inputs[0], 'user@example.com');
    fireEvent.changeText(inputs[1], 'myusername');
    fireEvent.changeText(inputs[2], 'password123');
    fireEvent.press(screen.getByText('Continue'));

    await waitFor(() => {
      expect(mockRouterReplace).toHaveBeenCalledWith('/(app)/(tabs)/movies');
    });
  });

  test('pressing "Sign in" navigates to the login screen', () => {
    render(<SignUpForm />);

    fireEvent.press(screen.getByText('Sign in'));

    expect(mockRouterReplace).toHaveBeenCalledWith('/login');
  });
});
