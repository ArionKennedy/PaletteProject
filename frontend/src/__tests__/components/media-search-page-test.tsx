import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react-native';
import { mediaSearch } from '@/services/search';
import MediaSearchPage from '@/components/media-search-page';
import type { MediaSearchResult } from '@/types/mediaSearchResult';

// --- Mocks ---

// Replace the real API call so tests never hit the network
jest.mock('@/services/search', () => ({
  mediaSearch: jest.fn(),
}));

// Link wraps results in a Pressable — render children directly for testing
jest.mock('expo-router', () => ({
  Link: ({ children }: { children: React.ReactNode }) => children,
}));

jest.mock('@/hooks/use-theme', () => ({
  useTheme: () => ({
    background: '#000000',
    text: '#ffffff',
    textSecondary: '#888888',
    backgroundElement: '#111111',
  }),
}));

// PageHeader has its own dependencies; not under test here
jest.mock('@/components/page-header', () => ({
  PageHeader: () => null,
}));

// Test data

const mockResults: MediaSearchResult[] = [
  {
    externalId: '1',
    mediaType: 'movie',
    title: 'Zapps trap',
    releaseDate: '2020-03-13',
    posterUrl: null,
  },
  {
    externalId: '2',
    mediaType: 'movie',
    title: 'The lowly CS major',
    releaseDate: '2026-12-19',
    posterUrl: null,
  },
];

// Helper to render the page and get the search input
function renderPage() {
  render(
    <MediaSearchPage
      mediaType="movie"
      title="Movies"
      getDetailHref={(id) => ({ pathname: '/movies/[id]', params: { id } })}
    />
  );
  return screen.getByPlaceholderText('Search...');
}

// --- Tests ---

describe('<MediaSearchPage />', () => {
  test('renders the search bar', () => {
    (mediaSearch as jest.Mock).mockResolvedValue([]);

    const input = renderPage();

    expect(input).toBeTruthy();
  });



  test('does not show results while the search is in progress', async () => {
    // Never resolves so the loading state stays visible
    (mediaSearch as jest.Mock).mockImplementation(() => new Promise(() => {}));

    const input = renderPage();
    fireEvent.changeText(input, 'batman');
    fireEvent(input, 'submitEditing');

    expect(screen.queryByText('Zapps trap')).toBeNull();
    expect(screen.queryByText('The lowly CS major')).toBeNull();
  });



  test('displays results after a successful search', async () => {
    (mediaSearch as jest.Mock).mockResolvedValue(mockResults);

    const input = renderPage();
    fireEvent.changeText(input, 'batman');
    fireEvent(input, 'submitEditing');

    await waitFor(() => {
      expect(screen.getByText('Batman Begins')).toBeTruthy();
      expect(screen.getByText('The Dark Knight')).toBeTruthy();
    });
  });



  test('shows an error message when the API call fails', async () => {
    (mediaSearch as jest.Mock).mockRejectedValue(new Error('Network error'));

    const input = renderPage();
    fireEvent.changeText(input, 'batman');
    fireEvent(input, 'submitEditing');

    await waitFor(() => {
      expect(screen.getByText('Network error')).toBeTruthy();
    });
  });



  test('shows "No results found." when the API returns an empty array', async () => {
    (mediaSearch as jest.Mock).mockResolvedValue([]);

    const input = renderPage();
    fireEvent.changeText(input, 'xyzzy');
    fireEvent(input, 'submitEditing');

    await waitFor(() => {
      expect(screen.getByText('No results found.')).toBeTruthy();
    });
  });


  
  test('calls mediaSearch with the correct mediaType and query', async () => {
    (mediaSearch as jest.Mock).mockResolvedValue([]);

    const input = renderPage();
    fireEvent.changeText(input, 'batman');
    fireEvent(input, 'submitEditing');

    await waitFor(() => {
      expect(mediaSearch).toHaveBeenCalledWith('movie', 'batman', expect.any(AbortSignal));
    });
  });
});
