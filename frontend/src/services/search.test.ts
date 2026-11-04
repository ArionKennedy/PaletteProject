import { mediaSearch } from '@/services/search';
import { getJson } from '@/services/client';

jest.mock('@/services/client', () => ({
  getJson: jest.fn(),
}));

const mockedGetJson = getJson as jest.MockedFunction<typeof getJson>;

describe('mediaSearch', () => {
  beforeEach(() => {
    mockedGetJson.mockReset();
  });

  it('builds an encoded search path and returns results', async () => {
    const results = [
      {
        externalId: '123',
        mediaType: 'movie',
        title: 'Test Movie',
        releaseDate: '2026-01-01',
        posterUrl: null,
      },
    ];
    mockedGetJson.mockResolvedValue({ results });

    await expect(mediaSearch('movie & tv', 'Dune Part 2')).resolves.toEqual(results);
    expect(mockedGetJson).toHaveBeenCalledWith(
      '/api/search/movie%20%26%20tv?q=Dune%20Part%202',
      undefined,
    );
  });

  it('passes the abort signal to the API client', async () => {
    const controller = new AbortController();
    mockedGetJson.mockResolvedValue({ results: [] });

    await mediaSearch('movie', 'Dune', controller.signal);

    expect(mockedGetJson).toHaveBeenCalledWith(
      '/api/search/movie?q=Dune',
      controller.signal,
    );
  });

  it('does not call the API for empty, whitespace-only, or null runtime values', async () => {
    await expect(mediaSearch('', 'Dune')).resolves.toEqual([]);
    await expect(mediaSearch('movie', '   ')).resolves.toEqual([]);
    await expect(
      mediaSearch(null as unknown as string, null as unknown as string),
    ).resolves.toEqual([]);
    expect(mockedGetJson).not.toHaveBeenCalled();
  });

  it('handles Unicode and reserved characters in the search path', async () => {
    mockedGetJson.mockResolvedValue({ results: [] });

    await mediaSearch('電影/TV & more', 'Amélie #1?');

    expect(mockedGetJson).toHaveBeenCalledWith(
      '/api/search/%E9%9B%BB%E5%BD%B1%2FTV%20%26%20more?q=Am%C3%A9lie%20%231%3F',
      undefined,
    );
  });

  it('returns an empty array when the API returns null results data', async () => {
    mockedGetJson.mockResolvedValue(null as never);

    await expect(mediaSearch('movie', 'Dune')).resolves.toEqual([]);
  });

  it('propagates API exceptions', async () => {
    mockedGetJson.mockRejectedValue(new Error('Backend unavailable'));

    await expect(mediaSearch('movie', 'Dune')).rejects.toThrow('Backend unavailable');
  });
});
