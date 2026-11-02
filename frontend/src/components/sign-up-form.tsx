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
import { useState, useRef } from 'react'
import {
  ActivityIndicator,
  Keyboard,
  Platform,
  Pressable,
  TextInput,
  TouchableWithoutFeedback,
  View,
} from 'react-native';
import { useAuth, useSignUp } from '@clerk/expo'

export function SignUpForm() {
  const { isLoaded } = useAuth();
  const { signUp, errors, fetchStatus } = useSignUp();

  const [emailAddress, setEmailAddress] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  const usernameInputRef = useRef<TextInput>(null);
  const passwordInputRef = useRef<TextInput>(null);

  function onEmailSubmitEditing() {
    usernameInputRef.current?.focus();
  }

  function onUsernameSubmitEditing() {
    passwordInputRef.current?.focus();  
  }

  async function onSubmit() {
    const { error } = await signUp.password({ emailAddress, password, username });
    if (error) {
      console.error(JSON.stringify(error, null, 2));
      console.error(JSON.stringify(errors, null, 2));
      return;
    }

    if(signUp.status === 'complete'){
      await signUp.finalize({
        navigate: () => {
          router.replace('/(app)/(tabs)/movies');
        },
      })

    }
     else {
      console.error('Sign-up attempt not complete:', signUp);
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
          <CardTitle className="text-center text-xl sm:text-left">Create your Palette account</CardTitle>
          <CardDescription className="text-center sm:text-left">
            Welcome! Please fill in the details below to get started.
          </CardDescription>
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

              {errors.fields.emailAddress && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Sign Up</AlertTitle>
                  <AlertDescription>
                    {errors?.fields?.emailAddress?.longMessage}
                  </AlertDescription>
                </Alert>
              }
            </View>

            <View className="gap-1.5">
              <View className="flex-row items-center">
                <Label htmlFor="username">Username</Label>
              </View>
              <Input
                ref={usernameInputRef}
                id="username"
                placeholder=""
                autoComplete="username"
                onSubmitEditing={onUsernameSubmitEditing}
                returnKeyType="next"
                submitBehavior="submit"
                onChangeText={setUsername}
              />

              {errors.fields.username && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Sign Up</AlertTitle>
                  <AlertDescription>
                    {errors?.fields?.username?.longMessage}
                  </AlertDescription>
                </Alert>
              }
            </View>

            <View className="gap-1.5">
              <View className="flex-row items-center">
                <Label htmlFor="password">Password</Label>
              </View>
              <Input
                ref={passwordInputRef}
                id="password"
                secureTextEntry
                returnKeyType="send"
                onSubmitEditing={onSubmit}
                onChangeText={setPassword}
              />

              {errors.fields.password && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Sign Up</AlertTitle>
                  <AlertDescription>
                    {errors?.fields?.password?.longMessage}
                  </AlertDescription>
                </Alert>
              }
            </View>
            <Button className="w-full" onPress={onSubmit} disabled={!emailAddress || !password || fetchStatus === 'fetching'}>
              <Text>Continue</Text>
            </Button>
          </View>
          <View className="flex-row items-center justify-center">
            <Text className="text-sm">Already have an account? </Text>
            <Pressable
              onPress={() => {
                router.replace('/login');
              }}>
              <Text className="text-sm underline underline-offset-4">Sign in</Text>
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
