import React from 'react';
import { render, screen } from '@testing-library/react-native';
import MediaDetailsPage from '@/components/media-details-page';
import type { MediaDetail } from '@/types/mediaDetail';

// Stack.Screen renders nothing meaningful in tests
jest.mock('expo-router', () => ({
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

// --- Fixtures ---

const mockMedia: MediaDetail = {
  externalId: '27205',
  title: 'Inception',
  posterUrl: 'https://example.com/inception.jpg',
  releaseDate: '2010-07-16',
  description: 'A thief who steals corporate secrets.',
};

// --- Tests ---

describe('<MediaDetailsPage />', () => {


  test('shows a loading spinner when media has not loaded yet', async () => {
    await render(
      <MediaDetailsPage media={null} error={null} metaText="" errorTitle="Movie" type="movie" />
    );

    // Neither content nor error should appear while loading
    expect(screen.queryByText('No description available.')).toBeNull();
    expect(screen.queryByText('No poster')).toBeNull();
  });



  test('shows an error message when there is an error', async () => {
    await render(
      <MediaDetailsPage
        media={null}
        error="Something went wrong"
        metaText=""
        errorTitle="Movie"
        type="movie"
      />
    );

    expect(screen.getByText('Something went wrong')).toBeTruthy();
  });



  test('renders the media title and description when loaded', async () => {
    await render(
      <MediaDetailsPage media={mockMedia} error={null} metaText="2010 · 2h 28m" errorTitle="Movie" type="movie" />
    );

    expect(screen.getByText('Inception')).toBeTruthy();
    expect(screen.getByText('A thief who steals corporate secrets.')).toBeTruthy();
    expect(screen.getByText('2010 · 2h 28m')).toBeTruthy();
  });



  test('shows "No description available." when description is null', async () => {
    await render(
      <MediaDetailsPage
        media={{ ...mockMedia, description: null }}
        error={null}
        metaText=""
        errorTitle="Movie"
        type="movie"
      />
    );

    expect(screen.getByText('No description available.')).toBeTruthy();
  });



  test('shows a "No poster" placeholder when posterUrl is null', async () => {
    await render(
      <MediaDetailsPage
        media={{ ...mockMedia, posterUrl: null }}
        error={null}
        metaText=""
        errorTitle="Movie"
        type="movie"
      />
    );

    expect(screen.getByText('No poster')).toBeTruthy();
  });
});
