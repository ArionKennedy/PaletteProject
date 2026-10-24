import { SplashScreen } from 'expo-router';
import { useAuth } from '@clerk/expo'

SplashScreen.preventAutoHideAsync();

export function SplashScreenController() {
  const { isLoaded } = useAuth();

  if (isLoaded) {
    SplashScreen.hide();
  }

  return null;
}
