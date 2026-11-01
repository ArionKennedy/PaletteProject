import { Stack } from 'expo-router';
import { useAuth, useUser } from '@clerk/expo';
import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, View } from 'react-native';

import { Button } from '@/components/ui/button';
import { Text } from '@/components/ui/text';
import { syncCurrentUser } from '@/services/api';

export default function AppLayout() {
  const { isLoaded: authLoaded, getToken } = useAuth();
  const { isLoaded: userLoaded, user } = useUser();
  const getTokenRef = useRef(getToken);
  const [retry, setRetry] = useState(0);
  const [confirmed, setConfirmed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const userId = user?.id;
  const email = user?.primaryEmailAddress?.emailAddress;
  const username = user?.username || user?.firstName || email?.split('@')[0];

  useEffect(() => {
    getTokenRef.current = getToken;
  }, [getToken]);

  useEffect(() => {
    if (!authLoaded || !userLoaded || !userId) {
      return;
    }

    let isActive = true;

    async function confirmDatabaseUser() {
      setConfirmed(false);
      setError(null);

      try {
        if (!email || !username) {
          throw new Error('Your Clerk account needs a primary email and username to sync.');
        }

        const token = await getTokenRef.current();
        if (!token) {
          throw new Error('Could not obtain an authentication token from Clerk.');
        }

        await syncCurrentUser(token, { email, username });
        if (isActive) {
          setConfirmed(true);
        }
      } catch (syncError) {
        if (isActive) {
          setError(syncError instanceof Error ? syncError.message : 'Unable to confirm your account.');
        }
      }
    }

    void confirmDatabaseUser();

    return () => {
      isActive = false;
    };
  }, [authLoaded, email, retry, userId, userLoaded, username]);

  if (!authLoaded || !userLoaded || (!confirmed && !error)) {
    return (
      <View className="flex-1 items-center justify-center gap-3 bg-background">
        <ActivityIndicator />
        <Text>Confirming your account...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View className="flex-1 items-center justify-center gap-4 bg-background p-6">
        <Text className="text-center">{error}</Text>
        <Button onPress={() => setRetry((value) => value + 1)}>
          <Text>Retry</Text>
        </Button>
      </View>
    );
  }

  return (
    <Stack
      screenOptions={{
        headerShown: false,
        animation: 'none',
        contentStyle: { backgroundColor: '#000000' },
      }}>
      <Stack.Screen name="(tabs)" />
    </Stack>
  );
}