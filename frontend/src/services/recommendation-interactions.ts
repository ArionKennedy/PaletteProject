import { fetchWithAuth } from '@/services/api';

export async function recordSearchSelection(
  mediaType: string,
  externalId: string,
): Promise<void> {
  const response = await fetchWithAuth(
    '/api/recommendations/interactions/selection',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        mediaType,
        externalId,
      }),
    },
  );

  if (!response.ok) {
    throw new Error(
      `Failed to record search selection: ${response.status}`,
    );
  }
}