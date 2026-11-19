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
import { useSignIn } from '@clerk/expo';
import { useState } from 'react';
import { Keyboard, Platform, TouchableWithoutFeedback, View } from 'react-native';
import { router } from 'expo-router';
import { clerkErrorsToPageErrors, validateEmail } from '@/lib/validation';
import { useUpdateEffect } from '@/hooks/UseUpdateEffect';

export function ForgotPasswordForm() {
  const [emailAddress, setEmailAddress] = useState('');
  const { signIn, errors, fetchStatus } = useSignIn();
  const [pageErrors, setPageErrors] = useState<Record<string, string>>({});

  useUpdateEffect(() => {
    setPageErrors(clerkErrorsToPageErrors(errors));
  }, [errors]);

  const onSubmit = async () => {
    setPageErrors({});

    const emailError = validateEmail(emailAddress);
    if (emailError) {
      setPageErrors({ identifier: emailError });
      return;
    }

    try {
      const { error: createError } = await signIn.create({
        identifier: emailAddress,
      });

      if (createError) {
        console.error(createError);
        return;
      }

      const { error: sendCodeError } = await signIn.resetPasswordEmailCode.sendCode();

      if (sendCodeError) {
        console.error(sendCodeError);
        return;
      }

      router.replace('/reset-password');
    } catch (err) {
      console.error(err);
    }
  };

  const form = (
    <View className="gap-6">
      <Card className="border-border/0 shadow-none sm:border-border sm:shadow-sm sm:shadow-black/5">
        <CardHeader>
          <CardTitle className="text-center text-xl sm:text-left">Forgot password?</CardTitle>
          <CardDescription className="text-center sm:text-left">
            Enter your email to reset your password
          </CardDescription>
                    
          {pageErrors?.global &&
            <Alert variant="destructive" icon={AlertCircleIcon}>
              <AlertTitle>Unable to Reset Password</AlertTitle>
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
                defaultValue={emailAddress}
                placeholder="m@example.com"
                keyboardType="email-address"
                autoComplete="email"
                autoCapitalize="none"
                onChangeText={setEmailAddress}
                onSubmitEditing={onSubmit}
                returnKeyType="send"
              />
              {pageErrors?.identifier && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Reset Password</AlertTitle>
                  <AlertDescription>
                    {pageErrors?.identifier}
                  </AlertDescription>
                </Alert>
              }
            </View>
            <Button className={"w-full"} onPress={onSubmit} disabled={!emailAddress || fetchStatus === 'fetching'} >
              <Text>Reset your password</Text>
            </Button>
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
