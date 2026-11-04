import { getJson } from '@/services/client';

describe('getJson', () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    globalThis.fetch = jest.fn();
  });

  afterAll(() => {
    globalThis.fetch = originalFetch;
  });

  it('returns JSON from a successful response', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      status: 200,
      json: jest.fn().mockResolvedValue({ value: 'ok' }),
    });

    await expect(getJson<{ value: string }>('/api/value')).resolves.toEqual({ value: 'ok' });
    expect(globalThis.fetch).toHaveBeenCalledWith('https://api.example.test/api/value', { signal: undefined });
  });

  it('uses ProblemDetails detail for failed requests', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 404,
      json: jest.fn().mockResolvedValue({ detail: 'Resource not found' }),
    });

    await expect(getJson('/api/missing')).rejects.toThrow('Resource not found');
  });

  it('uses the status fallback when ProblemDetails has no detail', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 500,
      json: jest.fn().mockResolvedValue({}),
    });

    await expect(getJson('/api/failure')).rejects.toThrow('API request failed: 500');
  });

  it('propagates network exceptions', async () => {
    (globalThis.fetch as jest.Mock).mockRejectedValue(new TypeError('Network unavailable'));

    await expect(getJson('/api/value')).rejects.toThrow('Network unavailable');
  });

  it('propagates malformed JSON exceptions from successful responses', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: true,
      status: 200,
      json: jest.fn().mockRejectedValue(new SyntaxError('Unexpected token')),
    });

    await expect(getJson('/api/value')).rejects.toThrow('Unexpected token');
  });

  it('uses the status fallback when the ProblemDetails body is malformed', async () => {
    (globalThis.fetch as jest.Mock).mockResolvedValue({
      ok: false,
      status: 502,
      json: jest.fn().mockRejectedValue(new SyntaxError('Invalid JSON')),
    });

    await expect(getJson('/api/failure')).rejects.toThrow('API request failed: 502');
  });

  it('throws a clear exception when the API URL is missing', async () => {
    const originalApiUrl = process.env.EXPO_PUBLIC_API_URL;
    delete process.env.EXPO_PUBLIC_API_URL;
    jest.resetModules();
    const { getJson: getJsonWithoutConfig } = require('@/services/client') as typeof import('@/services/client');

    await expect(getJsonWithoutConfig('/api/value')).rejects.toThrow('API URL is not configured');

    if (originalApiUrl === undefined) {
      delete process.env.EXPO_PUBLIC_API_URL;
    } else {
      process.env.EXPO_PUBLIC_API_URL = originalApiUrl;
    }
  });
});
