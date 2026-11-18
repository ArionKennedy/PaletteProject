import React from 'react';
import { render } from '@testing-library/react-native';
import { useAuth } from '@clerk/expo';
import IndexRoute from '@/app/index';

// Replace Clerk with a controllable fake — no real session needed
jest.mock('@clerk/expo', () => ({
  useAuth: jest.fn(),
}));

// Capture the href passed to <Redirect> so we can assert on it
const mockRedirectHref = jest.fn();
jest.mock('expo-router', () => ({
  Redirect: ({ href }: { href: string }) => {
    mockRedirectHref(href);
    return null;
  },
}));

describe('<IndexRoute />', () => {
  test('redirects to movies when the user is signed in', () => {
    (useAuth as jest.Mock).mockReturnValue({ isSignedIn: true });

    render(<IndexRoute />);

    expect(mockRedirectHref).toHaveBeenCalledWith('/(app)/(tabs)/movies');
  });

  test('redirects to login when the user is not signed in', () => {
    (useAuth as jest.Mock).mockReturnValue({ isSignedIn: false });

    render(<IndexRoute />);

    expect(mockRedirectHref).toHaveBeenCalledWith('/login');
  });
});
