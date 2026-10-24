import '../global.css';
import { Stack } from 'expo-router';

import { SplashScreenController } from '@/splashScreen';
import { PortalHost } from '@rn-primitives/portal';

import { ClerkProvider, useAuth } from '@clerk/expo'
import { tokenCache } from '@clerk/expo/token-cache'

const publishableKey = process.env.EXPO_PUBLIC_CLERK_PUBLISHABLE_KEY!

if (!publishableKey) {
  throw new Error('Add your Clerk Publishable Key to the .env file')
}

export default function Root() {
  return (
    <ClerkProvider publishableKey={publishableKey} tokenCache={tokenCache}>
      <SplashScreenController />
      <RootNavigator />
      <PortalHost />
    </ClerkProvider>
  );
}

function RootNavigator() {
  const { isLoaded, isSignedIn } = useAuth();

  if (!isLoaded) {
    return null;
  }

  return (
    <Stack screenOptions={{ headerShown: false, animation: 'none', contentStyle: { backgroundColor: '#000000' }}}>
      <Stack.Protected guard={!!isSignedIn}>
        <Stack.Screen name="(app)"/>
      </Stack.Protected>

      <Stack.Protected guard={!isSignedIn}>
        <Stack.Screen name="login"/>
        <Stack.Screen name="signup"/>
      </Stack.Protected>
    </Stack>
  );
}
