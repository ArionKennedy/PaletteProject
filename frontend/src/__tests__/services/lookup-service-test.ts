import { movieLookup, tvLookup } from '@/services/lookup';

process.env.EXPO_PUBLIC_API_URL = 'http://localhost:3000';

beforeEach(() => {
  globalThis.fetch = jest.fn();
});


// Test data

const mockMovie = {
  externalId: '1738',
  mediaType: 'movie',
  title: 'Rise of Guiderian',
  releaseDate: '2026-07-16',
  posterUrl: null,
  description: 'A man with many internet friends.',
  runtime: 420,
};

const mockTv = {
  externalId: '1876',
  mediaType: 'tv',
  title: 'Terrence and Philip',
  releaseDate: '1999-01-20',
  posterUrl: null,
  description: 'Canadians.', // A canadian wrote this
  numberOfSeasons: 5,
};

// Movie lookup tests

describe('movieLookup()', () => {


  test('returns the movie object on success', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => mockMovie,
    });

    const result = await movieLookup('27205');

    expect(result).toEqual(mockMovie);
  });



  test('calls the correct URL', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => mockMovie,
    });

    await movieLookup('27205');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      'http://localhost:3000/api/lookup/movie/27205',
      expect.any(Object),
    );
  });


  
  test('throws an error message on a failed response', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 404,
      json: async () => ({ detail: 'Movie not found' }),
    });

    await expect(movieLookup('99999')).rejects.toThrow('Movie not found');
  });


});

// TV lookup tests

describe('tvLookup()', () => {


  test('returns the TV show object on success', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => mockTv,
    });

    const result = await tvLookup('1396');

    expect(result).toEqual(mockTv);
  });



  test('calls the correct URL', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      json: async () => mockTv,
    });

    await tvLookup('1396');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      'http://localhost:3000/api/lookup/tv/1396',
      expect.any(Object),
    );
  });



  test('throws an error message on a failed response', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 404,
      json: async () => ({ detail: 'TV show not found' }),
    });

    await expect(tvLookup('99999')).rejects.toThrow('TV show not found');
  });

  
});
