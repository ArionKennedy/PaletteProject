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
import { useAuthFormErrors } from '@/hooks/use-auth-form-errors';
import {
  ActivityIndicator,
  Keyboard,
  Platform,
  Pressable,
  TouchableWithoutFeedback,
  type TextInputInstance,
  View,
} from 'react-native';
import { useAuth, useSignIn } from '@clerk/expo';
import { useVerificationCodeCooldown } from '@/hooks/use-verification-code-cooldown';
import { clerkRequestToPageErrors, getVerificationCodeRetryAfter } from '@/lib/clerk-sign-in';
import { validateEmail } from '@/lib/validation';

export function SignInForm() {
  const { isLoaded } = useAuth();
  const { signIn, fetchStatus } = useSignIn();

  const [emailAddress, setEmailAddress] = useState('');
  const [password, setPassword] = useState('');
  const [factor, setFactor] = useState<'email_code' | 'phone_code' | null>(null);
  const [code, setCode] = useState('');
  const [codeSent, setCodeSent] = useState(false);
  const cooldownKey = `sign-in:${emailAddress}`;
  const { secondsLeft, startCooldown } = useVerificationCodeCooldown(cooldownKey);
  const { pageErrors, setPageErrors, isFocused } = useAuthFormErrors();
  const requestInProgress = useRef(false);
  const passwordInputRef = useRef<TextInputInstance>(null);

  function onEmailSubmitEditing() {
    passwordInputRef.current?.focus();
  }

  async function onSubmit() {
    if (requestInProgress.current || fetchStatus === 'fetching' || !password) return;
    setPageErrors({});

    const emailError = validateEmail(emailAddress);
    if (emailError) {
      setPageErrors({ identifier: emailError });
      return;
    }

    requestInProgress.current = true;
    const failureMessage = 'Could not sign in. Please try again.';
    try {
      const { error } = await signIn.password({ emailAddress, password });
      if (error) {
        setPageErrors(clerkRequestToPageErrors(error, failureMessage));
        return;
      }

      if (!isFocused()) return;

      if (signIn.status === 'complete') {
        await finalizeSignIn();
        return;
      }

      if (signIn.status === 'needs_client_trust') {
        const factors = signIn.supportedSecondFactors;
        if (factors?.some(({ strategy }) => strategy === 'email_code')) {
          setFactor('email_code');
          return;
        }
        if (factors?.some(({ strategy }) => strategy === 'phone_code')) {
          setFactor('phone_code');
          return;
        }
      }

      setPageErrors({ global: 'This sign-in requires an additional verification method.' });
    } catch {
      setPageErrors({ global: failureMessage });
    } finally {
      requestInProgress.current = false;
    }
  }

  async function finalizeSignIn() {
    const { error } = await signIn.finalize({
      navigate: () => router.replace('/(app)/(tabs)/movies'),
    });
    if (error) {
      setPageErrors(clerkRequestToPageErrors(error, 'Could not sign in. Please try again.'));
    }
  }

  async function sendCode() {
    if (!factor || requestInProgress.current || fetchStatus === 'fetching' || secondsLeft > 0) return;

    requestInProgress.current = true;
    setPageErrors({});
    const fallback = 'Could not send the verification code. Please try again.';
    try {
      const { error } = factor === 'email_code'
        ? await signIn.mfa.sendEmailCode()
        : await signIn.mfa.sendPhoneCode();

      if (error) {
        const retryAfter = getVerificationCodeRetryAfter(error);
        if (retryAfter !== null) startCooldown(cooldownKey, retryAfter);
        const errors = clerkRequestToPageErrors(error, fallback);
        setPageErrors({ ...errors, global: errors.global || fallback });
        return;
      }

      startCooldown(cooldownKey);
      setCode('');
      setCodeSent(true);
    } catch {
      setPageErrors({ global: fallback });
    } finally {
      requestInProgress.current = false;
    }
  }

  async function verifyCode() {
    if (!factor || requestInProgress.current || fetchStatus === 'fetching' || !code.trim()) return;

    requestInProgress.current = true;
    setPageErrors({});
    const fallback = 'Could not verify the code. Please try again.';
    try {
      if (signIn.status !== 'complete') {
        const { error } = factor === 'email_code'
          ? await signIn.mfa.verifyEmailCode({ code: code.trim() })
          : await signIn.mfa.verifyPhoneCode({ code: code.trim() });

        if (error) {
          const errors = clerkRequestToPageErrors(error, fallback);
          setPageErrors({ ...errors, global: errors.global || errors.code || fallback });
          return;
        }
      }

      if (!isFocused()) return;

      if (signIn.status === 'complete') {
        await finalizeSignIn();
      } else {
        setPageErrors({ global: 'This sign-in requires an additional verification method.' });
      }
    } catch {
      setPageErrors({ global: fallback });
    } finally {
      requestInProgress.current = false;
    }
  }

  function backToSignIn() {
    if (requestInProgress.current) return;
    setFactor(null);
    setCode('');
    setCodeSent(false);
    setPassword('');
    setPageErrors({});
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
            {factor ? 'Verify this device to continue' : 'Welcome back! Please Sign In to continue'}
          </CardDescription>

          {pageErrors?.global && (
            <Alert variant="destructive" icon={AlertCircleIcon}>
              <AlertTitle>{pageErrors.globalTitle || 'Unable to Sign In'}</AlertTitle>
              <AlertDescription>{pageErrors.global}</AlertDescription>
            </Alert>
          )}
        </CardHeader>

        {factor ? (
          <CardContent className="gap-3">
            <Text>
              {codeSent ? 'Code sent. Enter it below to sign in.' : factor === 'email_code'
                ? 'Send a verification code to your email.'
                : 'Send a verification code to your phone.'}
            </Text>
            <Button
              variant="outline"
              onPress={sendCode}
              disabled={fetchStatus === 'fetching' || secondsLeft > 0}>
              <Text>
                {secondsLeft > 0 ? `Send again in ${secondsLeft}s` : codeSent ? 'Resend code' : 'Send code'}
              </Text>
            </Button>
            <Label htmlFor="sign-in-code">Verification code</Label>
            <Input
              id="sign-in-code"
              value={code}
              onChangeText={setCode}
              keyboardType="number-pad"
              autoComplete="one-time-code"
              onSubmitEditing={verifyCode}
            />
            <Button onPress={verifyCode} disabled={!code.trim() || fetchStatus === 'fetching'}>
              <Text>Verify and sign in</Text>
            </Button>
            <Button variant="link" onPress={backToSignIn} disabled={fetchStatus === 'fetching'}>
              <Text>Back to sign in</Text>
            </Button>
          </CardContent>
        ) : (
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
                  value={emailAddress}
                  onChangeText={setEmailAddress}
                />
                {pageErrors?.identifier && (
                  <Alert variant="destructive" icon={AlertCircleIcon}>
                    <AlertTitle>Unable to Sign In</AlertTitle>
                    <AlertDescription>{pageErrors.identifier}</AlertDescription>
                  </Alert>
                )}
              </View>

              <View className="gap-1.5">
                <View className="flex-row items-center">
                  <Label htmlFor="password">Password</Label>
                  <Button
                    variant="link"
                    size="sm"
                    className="web:h-fit ml-auto h-4 px-1 py-0 sm:h-4"
                    onPress={() => router.replace('/forgot-password')}>
                    <Text className="font-normal leading-4">Forgot your password?</Text>
                  </Button>
                </View>
                <Input
                  ref={passwordInputRef}
                  id="password"
                  secureTextEntry
                  returnKeyType="send"
                  onSubmitEditing={onSubmit}
                  value={password}
                  onChangeText={setPassword}
                />
                {pageErrors?.password && (
                  <Alert variant="destructive" icon={AlertCircleIcon}>
                    <AlertTitle>Unable to Sign In</AlertTitle>
                    <AlertDescription>{pageErrors.password}</AlertDescription>
                  </Alert>
                )}
              </View>

              <Button
                className="w-full"
                onPress={onSubmit}
                disabled={!emailAddress || !password || fetchStatus === 'fetching'}>
                <Text>Continue</Text>
              </Button>
            </View>

            <View className="flex-row items-center justify-center">
              <Text className="text-sm">Don&apos;t have an account? </Text>
              <Pressable onPress={() => router.replace('/signup')}>
                <Text className="text-sm underline underline-offset-4">Sign up</Text>
              </Pressable>
            </View>
          </CardContent>
        )}
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
