import { mediaSearch } from '@/services/search';

// Bypass Clerk and the API_URL env-var check — just forward to fetch
jest.mock('@/services/api', () => ({
  fetchWithAuth: (path: string, options: RequestInit = {}) =>
    globalThis.fetch(`http://localhost:3000${path}`, options),
}));

// Replace the globalThis fetch so tests never hit a real server
beforeEach(() => {
  globalThis.fetch = jest.fn();
});

describe('mediaSearch()', () => {


  test('returns the results array on a successful response', async () => {
    const mockResults = [
      { externalId: '1', mediaType: 'movie', title: 'Batman Begins', releaseDate: '2005-06-15', posterUrl: null },
    ];

    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => ({ results: mockResults }),
    });

    const results = await mediaSearch('movie', 'batman');

    expect(results).toEqual(mockResults);
  });



  test('returns an empty array when the API returns no results', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => ({ results: [] }),
    });

    const results = await mediaSearch('movie', 'xyzzy');

    expect(results).toEqual([]);
  });



  test('calls the correct URL with encoded query parameters', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => ({ results: [] }),
    });

    await mediaSearch('movie', 'dark knight');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      'http://localhost:3000/api/search/movie?q=dark%20knight',
      expect.any(Object),
    );
  });



  test('throws the backend detail message on a non-ok response', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 404,
      json: async () => ({ detail: 'No movies found' }),
    });

    await expect(mediaSearch('movie', 'batman')).rejects.toThrow('No movies found');
  });



  test('throws a generic message when the error response has no detail', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 500,
      json: async () => null,
    });

    await expect(mediaSearch('movie', 'batman')).rejects.toThrow('API request failed: 500');
  });


});
