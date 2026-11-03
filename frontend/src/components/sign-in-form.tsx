import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Text } from '@/components/ui/text';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertCircleIcon } from 'lucide-react-native';
import { router } from 'expo-router';
import { useState, useRef } from 'react';
import { useUpdateEffect } from '@/hooks/UseUpdateEffect';
import {
  ActivityIndicator,
  Keyboard,
  Platform,
  Pressable,
  TouchableWithoutFeedback,
  type TextInput,
  View,
} from 'react-native';
import { useAuth, useSignIn } from '@clerk/expo';
import { clerkErrorsToPageErrors, validateEmail } from '@/lib/validation';

export function SignInForm() {
  const { isLoaded } = useAuth();
  const { signIn, errors, fetchStatus } = useSignIn()
  
  const [emailAddress, setEmailAddress] = useState('');
  const [password, setPassword] = useState('');
  const [pageErrors, setPageErrors] = useState<Record<string, string>>({});

  const passwordInputRef = useRef<TextInput>(null);

  function onEmailSubmitEditing() {
    passwordInputRef.current?.focus();
  }

  useUpdateEffect(() => {
    setPageErrors(clerkErrorsToPageErrors(errors));
  }, [errors]);

  async function onSubmit() {
    setPageErrors({});

    const emailError = validateEmail(emailAddress);
    if (emailError) {
      setPageErrors({ identifier: emailError });
      return;
    }

    const { error } = await signIn.password({ emailAddress, password });
    if (error) {
      console.error(JSON.stringify(error, null, 2));
      console.error(JSON.stringify(errors, null, 2));
      return;
    }

    if(signIn.status === 'complete'){
      await signIn.finalize({
        navigate: () => {
          router.replace('/(app)/(tabs)/movies');
        },
      })

    }
     else {
      console.error('Sign-in attempt not complete:', signIn);
    }
  }

  if (!isLoaded) {
    return (
      <View className="items-center gap-3">
        <ActivityIndicator />
      </View>
    );
  }

  const form = (
    <View className="gap-6">
      <Card className="border-border/0 sm:border-border shadow-none sm:shadow-sm sm:shadow-black/5">
        <CardHeader>
          <CardTitle className="text-center text-xl sm:text-left">Palette Sign In</CardTitle>
          <CardDescription className="text-center sm:text-left">
            Welcome back! Please Sign In to continue
          </CardDescription>
          
          {pageErrors?.global &&
            <Alert variant="destructive" icon={AlertCircleIcon}>
              <AlertTitle>Unable to Sign In</AlertTitle>
              <AlertDescription>
                {pageErrors?.global}
              </AlertDescription>
            </Alert>
          }
        </CardHeader>

        <CardContent className="gap-6">
          <View className="gap-6">
            <View className="gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                placeholder="m@example.com"
                keyboardType="email-address"
                autoComplete="email"
                autoCapitalize="none"
                onSubmitEditing={onEmailSubmitEditing}
                returnKeyType="next"
                submitBehavior="submit"
                onChangeText={setEmailAddress}
              />

              {pageErrors?.identifier && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Sign In</AlertTitle>
                  <AlertDescription>
                    {pageErrors?.identifier}
                  </AlertDescription>
                </Alert>
              }
            </View>
            <View className="gap-1.5">
              <View className="flex-row items-center">
                <Label htmlFor="password">Password</Label>
                <Button
                  variant="link"
                  size="sm"
                  className="web:h-fit ml-auto h-4 px-1 py-0 sm:h-4"
                  onPress={() => {
                    // TODO: Navigate to forgot password screen
                  }}>
                  <Text className="font-normal leading-4">Forgot your password?</Text>
                </Button>
              </View>
              <Input
                ref={passwordInputRef}
                id="password"
                secureTextEntry
                returnKeyType="send"
                onSubmitEditing={onSubmit}
                onChangeText={setPassword}
              />

              {pageErrors?.password && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Sign In</AlertTitle>
                  <AlertDescription>
                    {pageErrors?.password}
                  </AlertDescription>
                </Alert>
              }
            </View>
            <Button className="w-full" onPress={onSubmit} disabled={!emailAddress || !password || fetchStatus === 'fetching'}>
              <Text>Continue</Text>
            </Button>
          </View>
          <View className="flex-row items-center justify-center">
            <Text className="text-sm">Don&apos;t have an account? </Text>
            <Pressable
              onPress={() => {
                router.replace('/signup');
              }}>
              <Text className="text-sm underline underline-offset-4">Sign up</Text>
            </Pressable>
          </View>
        </CardContent>
      </Card>
    </View>
  );

  if (Platform.OS === 'web') {
    return form;
  }

  return (
    <TouchableWithoutFeedback onPress={() => Keyboard.dismiss()} accessible={false}>
      {form}
    </TouchableWithoutFeedback>
  );
}
