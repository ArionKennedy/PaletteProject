import React from 'react';
import { render, screen } from '@testing-library/react-native';
import Movie from '@/components/movie';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

// useTheme accesses colours that don't exist in the Jest environment
jest.mock('@/hooks/use-theme', () => ({
  useTheme: () => ({
    background: '#000000',
    text: '#ffffff',
    textSecondary: '#888888',
    backgroundElement: '#111111',
  }),
}));

// --- Fixtures ---

const baseMovie: MediaSearchResult = {
  externalId: '1',
  mediaType: 'movie',
  title: 'Inception',
  releaseDate: '2010-07-16',
  posterUrl: 'https://example.com/inception.jpg',
};

// --- Tests ---

describe('<Movie />', () => {


  test('renders the title, media type, and release date', async () => {
    await render(<Movie movie={baseMovie} />);

    expect(screen.getByText('Inception')).toBeTruthy();
    expect(screen.getByText('movie')).toBeTruthy();
    expect(screen.getByText('2010-07-16')).toBeTruthy();
  });



  test('shows "Release date unknown" when releaseDate is null', async () => {
    await render(<Movie movie={{ ...baseMovie, releaseDate: null }} />);

    expect(screen.getByText('Release date unknown')).toBeTruthy();
  });



  test('shows a "No poster" placeholder when posterUrl is null', async () => {
    await render(<Movie movie={{ ...baseMovie, posterUrl: null }} />);

    expect(screen.getByText('No poster')).toBeTruthy();
  });



  test('does not show "No poster" when a posterUrl is provided', async () => {
    await render(<Movie movie={baseMovie} />);

    expect(screen.queryByText('No poster')).toBeNull();
  });
});
