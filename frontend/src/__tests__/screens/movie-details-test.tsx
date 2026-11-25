import React from 'react';
import { render, screen, waitFor } from '@testing-library/react-native';
import { movieLookup } from '@/services/lookup';
import MovieDetailsPage from '@/app/(app)/(tabs)/movies/[id]';
import type { MovieLookupResult } from '@/types/movieLookupResult';

// Mocks

jest.mock('@/services/lookup', () => ({
  movieLookup: jest.fn()
}));

// useLocalSearchParams provides the route param (:id) to the component
jest.mock('expo-router', () => ({
  useLocalSearchParams: jest.fn(() => ({ id: '27205' })),
  Stack: { Screen: () => null },
  useFocusEffect: jest.fn(),
}));

// LibraryAction brings in the Clerk/API dependency chain — not under test here
jest.mock('@/components/library-action', () => ({
  LibraryAction: () => null,
}));

jest.mock('@/hooks/use-theme', () => ({
  useTheme: () => ({
    background: '#000000',
    text: '#ffffff',
    textSecondary: '#888888',
    backgroundElement: '#111111',
  }),
}));

// Fixtures

const mockMovie: MovieLookupResult = {
  externalId: '27205',
  mediaType: 'movie',
  title: 'Inception',
  releaseDate: '2010-07-16',
  posterUrl: null,
  description: 'A thief who steals corporate secrets.',
  runtime: 148,
};

// Tests

describe('<MovieDetailsPage />', () => {

  test('does not show content before the movie data arrives', async () => {
    (movieLookup as jest.Mock).mockImplementation(() => new Promise(() => {}));

    await render(<MovieDetailsPage />);

    expect(screen.queryByText('Inception')).toBeNull();
  });



  test('renders the movie title, release year, runtime and description after the data loads', async () => {
    (movieLookup as jest.Mock).mockResolvedValue(mockMovie);

    await render(<MovieDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('Inception')).toBeTruthy();
      expect(screen.getByText('A thief who steals corporate secrets.')).toBeTruthy();
      expect(screen.getByText('2010 · 2h 28m')).toBeTruthy();
    });
  });



  test('shows an error message when the lookup fails', async () => {
    (movieLookup as jest.Mock).mockRejectedValue(new Error('Movie not found'));

    await render(<MovieDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('Movie not found')).toBeTruthy();
    });
  });



  test('shows "No poster" placeholder when posterUrl is null', async () => {
    (movieLookup as jest.Mock).mockResolvedValue({ ...mockMovie, posterUrl: null });

    await render(<MovieDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('No poster')).toBeTruthy();
    });
  });


});
