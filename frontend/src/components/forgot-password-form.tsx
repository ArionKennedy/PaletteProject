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
import { useRef, useState } from 'react';
import { Keyboard, Platform, TouchableWithoutFeedback, View } from 'react-native';
import { router } from 'expo-router';
import { useAuthFormErrors } from '@/hooks/use-auth-form-errors';
import { getVerificationCodeWaitSeconds, useVerificationCodeCooldown } from '@/hooks/use-verification-code-cooldown';
import { canResumePasswordReset, clerkRequestToPageErrors, getVerificationCodeRetryAfter } from '@/lib/clerk-sign-in';
import { validateEmail } from '@/lib/validation';

export function ForgotPasswordForm() {
  const [emailAddress, setEmailAddress] = useState('');
  const { signIn, fetchStatus } = useSignIn();
  const { pageErrors, setPageErrors, isFocused } = useAuthFormErrors();
  const requestInProgress = useRef(false);
  const { secondsLeft, startCooldown } = useVerificationCodeCooldown(emailAddress);
  const waitingToSend = secondsLeft > 0 && !canResumePasswordReset(signIn, emailAddress.trim());

  const onSubmit = async () => {
    if (requestInProgress.current || fetchStatus === 'fetching') return;
    setPageErrors({});

    const identifier = emailAddress.trim();
    const emailError = validateEmail(identifier);
    if (emailError) {
      setPageErrors({ identifier: emailError });
      return;
    }

    if (canResumePasswordReset(signIn, identifier)) {
      router.replace('/reset-password');
      return;
    }

    if (getVerificationCodeWaitSeconds(identifier) > 0) return;

    requestInProgress.current = true;
    try {
      const { error: createError } = await signIn.create({ identifier });

      if (createError) {
        setPageErrors(clerkRequestToPageErrors(createError, 'Could not start password reset. Please try again.'));
        return;
      }

      const { error: sendCodeError } = await signIn.resetPasswordEmailCode.sendCode();

      if (sendCodeError) {
        const retryAfter = getVerificationCodeRetryAfter(sendCodeError);
        if (retryAfter !== null) startCooldown(identifier, retryAfter);
        setPageErrors({
          ...clerkRequestToPageErrors(sendCodeError, 'Could not send the verification code. Please try again.'),
          globalTitle: 'Unable to send verification code',
        });
        return;
      }

      startCooldown(identifier);
      if (isFocused()) router.replace('/reset-password');
    } catch {
      setPageErrors({
        global: 'Could not send the verification code. Please try again.',
        globalTitle: 'Unable to send verification code',
      });
    } finally {
      requestInProgress.current = false;
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
              <AlertTitle>{pageErrors.globalTitle || 'Unable to Reset Password'}</AlertTitle>
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
                value={emailAddress}
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
            <Button
              className="w-full"
              onPress={onSubmit}
              disabled={!emailAddress.trim() || waitingToSend || fetchStatus === 'fetching'}
            >
              <Text>{waitingToSend ? `Request a new code in ${secondsLeft}s` : 'Reset your password'}</Text>
            </Button>
            <Button
              variant="link"
              size="sm"
              className="self-center h-fit px-1 py-0"
              onPress={() => router.replace('/login')}
            >
              <Text>Back to Sign in</Text>
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
