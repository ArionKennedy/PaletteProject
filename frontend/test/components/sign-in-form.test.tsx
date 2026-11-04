import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react-native';
import { router } from 'expo-router';
import { SignInForm } from '@/components/sign-in-form';

const mockPassword = jest.fn();
const mockFinalize = jest.fn();
const mockUseAuth = jest.fn();
const mockUseSignIn = jest.fn();
const mockSignIn = {
  status: 'complete',
  password: mockPassword,
  finalize: mockFinalize,
};

jest.mock('@clerk/expo', () => ({
  useAuth: () => mockUseAuth(),
  useSignIn: () => mockUseSignIn(),
}));

jest.mock('expo-router', () => ({
  router: {
    replace: jest.fn(),
  },
}));

jest.mock('lucide-react-native', () => ({
  AlertCircleIcon: () => null,
}));

describe('SignInForm', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockSignIn.status = 'complete';
    mockPassword.mockResolvedValue({ error: null });
    mockFinalize.mockImplementation(async (options: { navigate: () => void }) => {
      options.navigate();
    });
    mockUseAuth.mockReturnValue({ isLoaded: true });
    mockUseSignIn.mockReturnValue({
      signIn: mockSignIn,
      errors: { fields: {} },
      fetchStatus: 'idle',
    });
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('shows a loading indicator while Clerk is not loaded', async () => {
    mockUseAuth.mockReturnValue({ isLoaded: false });

    await render(<SignInForm />);

    expect(screen.getByLabelText('Loading sign in')).toBeTruthy();
  });

  it('keeps Continue disabled until email and password are provided', async () => {
    await render(<SignInForm />);
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));
    expect(mockPassword).not.toHaveBeenCalled();

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(mockPassword).toHaveBeenCalledWith({
      emailAddress: 'user@example.com',
      password: 'secret',
    });
  });

  it('keeps Continue disabled for a whitespace-only email', async () => {
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), '   ');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(mockPassword).not.toHaveBeenCalled();
  });

  it('trims the email and completes sign-in before navigating', async () => {
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), '  user@example.com  ');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(mockPassword).toHaveBeenCalledWith({
      emailAddress: 'user@example.com',
      password: 'secret',
    });
    expect(mockFinalize).toHaveBeenCalledTimes(1);
    expect(router.replace).toHaveBeenCalledWith('/(app)/(tabs)/movies');
  });

  it('does not navigate when Clerk returns a sign-in error', async () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => undefined);
    mockPassword.mockResolvedValue({ error: { message: 'Invalid credentials' } });
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'wrong');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(mockFinalize).not.toHaveBeenCalled();
    expect(router.replace).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
  });

  it('does not navigate when sign-in is incomplete', async () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => undefined);
    mockSignIn.status = 'needs_second_factor';
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(mockFinalize).not.toHaveBeenCalled();
    expect(router.replace).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalledWith(
      'Sign-in attempt not complete:',
      expect.objectContaining({ status: 'needs_second_factor' }),
    );
  });

  it('handles thrown Clerk exceptions without throwing out of the component', async () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => undefined);
    mockPassword.mockRejectedValue(new Error('Clerk unavailable'));
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');
    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));

    expect(consoleError).toHaveBeenCalledWith(
      'Sign-in exception:',
      'Clerk unavailable',
    );
    expect(mockFinalize).not.toHaveBeenCalled();
  });

  it('renders Clerk field validation messages', async () => {
    mockUseSignIn.mockReturnValue({
      signIn: mockSignIn,
      errors: {
        fields: {
          identifier: { longMessage: 'Invalid email address' },
          password: { longMessage: 'Password is incorrect' },
        },
      },
      fetchStatus: 'idle',
    });

    await render(<SignInForm />);

    expect(screen.getByText('Invalid email address')).toBeTruthy();
    expect(screen.getByText('Password is incorrect')).toBeTruthy();
  });

  it('disables Continue while Clerk is fetching', async () => {
    mockUseSignIn.mockReturnValue({
      signIn: mockSignIn,
      errors: { fields: {} },
      fetchStatus: 'fetching',
    });
    await render(<SignInForm />);

    await fireEvent.changeText(screen.getByPlaceholderText('m@example.com'), 'user@example.com');
    await fireEvent.changeText(screen.getByPlaceholderText('Password'), 'secret');

    await fireEvent.press(screen.getByRole('button', { name: 'Continue' }));
    expect(mockPassword).not.toHaveBeenCalled();
  });
});
