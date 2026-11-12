import { getClerkInstance } from '@clerk/expo';

const API_URL = process.env.EXPO_PUBLIC_API_URL;

export async function fetchWithAuth(path: string, options: RequestInit = {}): Promise<Response>  {
  if (!API_URL) {
    throw new Error('API URL is not configured');
  }

  const clerkInstance = getClerkInstance();
  const token = await clerkInstance.session?.getToken();

  const headers = new Headers(options.headers || {});
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  return fetch(`${API_URL}${path}`, {
    ...options,
    headers,
  });
}

export async function testConnection(): Promise<string> {
  if (!API_URL) {
    throw new Error('API URL is not configured');
  }

  const response = await fetch(`${API_URL}/api/test`);

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status}`);
  }

  const data: { message: string } = await response.json();

  return data.message;
}
