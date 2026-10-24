import { Stack } from 'expo-router';

export default function AppLayout() {
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