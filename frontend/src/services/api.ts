const API_URL = process.env.EXPO_PUBLIC_API_URL;

export type CurrentUserProfile = {
  userId: string;
  username: string;
  email: string;
  dateJoined: string;
};

export async function syncCurrentUser(
  token: string,
  profile: { username: string; email: string },
): Promise<CurrentUserProfile> {
  if (!API_URL) {
    throw new Error('API URL is not configured');
  }

  const response = await fetch(`${API_URL.replace(/\/$/, '')}/api/users/me`, {
    method: 'PUT',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(profile),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    const detail =
      typeof errorBody?.detail === 'string'
        ? errorBody.detail
        : typeof errorBody?.title === 'string'
          ? errorBody.title
          : `API request failed: ${response.status}`;
    throw new Error(detail);
  }

  return response.json() as Promise<CurrentUserProfile>;
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
