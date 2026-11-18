import React from 'react';
import { render, screen, waitFor } from '@testing-library/react-native';
import { tvLookup } from '@/services/lookup';
import TvDetailsPage from '@/app/(app)/(tabs)/tv/[id]';
import type { TvLookupResult } from '@/types/tvLookupResult';

// --- Mocks ---

jest.mock('@/services/lookup', () => ({
  movieLookup: jest.fn(),
  tvLookup: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: jest.fn(() => ({ id: '1396' })),
  Stack: { Screen: () => null },
}));

jest.mock('@/hooks/use-theme', () => ({
  useTheme: () => ({
    background: '#000000',
    text: '#ffffff',
    textSecondary: '#888888',
    backgroundElement: '#111111',
  }),
}));

// --- Fixtures ---

const mockTv: TvLookupResult = {
  externalId: '1396',
  mediaType: 'tv',
  title: 'Breaking Bad',
  releaseDate: '2008-01-20',
  posterUrl: null,
  description: 'A high school chemistry teacher turned drug kingpin.',
  numberOfSeasons: 5,
};

// --- Tests ---

describe('<TvDetailsPage />', () => {


  test('does not show content before the TV show data arrives', async () => {
    (tvLookup as jest.Mock).mockImplementation(() => new Promise(() => {}));

    await render(<TvDetailsPage />);

    expect(screen.queryByText('Breaking Bad')).toBeNull();
  });



  test('renders the TV show title and description after the data loads', async () => {
    (tvLookup as jest.Mock).mockResolvedValue(mockTv);

    render(<TvDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('Breaking Bad')).toBeTruthy();
      expect(screen.getByText('A high school chemistry teacher turned drug kingpin.')).toBeTruthy();
    });
  });



  test('renders the first air year and season count in the meta line', async () => {
    (tvLookup as jest.Mock).mockResolvedValue(mockTv);

    render(<TvDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('First Air: 2008 · 5 Seasons')).toBeTruthy();
    });
  });



  test('uses singular "Season" when numberOfSeasons is 1', async () => {
    (tvLookup as jest.Mock).mockResolvedValue({ ...mockTv, numberOfSeasons: 1 });

    render(<TvDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('First Air: 2008 · 1 Season')).toBeTruthy();
    });
  });



  test('shows an error message when the lookup fails', async () => {
    (tvLookup as jest.Mock).mockRejectedValue(new Error('TV show not found'));

    render(<TvDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('TV show not found')).toBeTruthy();
    });
  });



  test('shows "No poster" placeholder when posterUrl is null', async () => {
    (tvLookup as jest.Mock).mockResolvedValue({ ...mockTv, posterUrl: null });

    render(<TvDetailsPage />);

    await waitFor(() => {
      expect(screen.getByText('No poster')).toBeTruthy();
    });
  });

  
});
