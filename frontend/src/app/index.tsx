import { Redirect } from 'expo-router';
import { useAuth } from '@clerk/expo'

export default function IndexRoute() {
  const { isSignedIn } = useAuth();
  if ( isSignedIn ){
    return <Redirect href="/(app)/(tabs)/movies" />;
  }
  else{
    return <Redirect href="/login" />;
  }
}
