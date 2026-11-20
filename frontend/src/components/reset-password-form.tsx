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
import { useState, useRef } from 'react';
import { ActivityIndicator, Keyboard, Platform, TouchableWithoutFeedback, View, type TextInputInstance } from 'react-native';
import { router } from 'expo-router';
import { useAuthFormErrors } from '@/hooks/use-auth-form-errors';
import { getVerificationCodeWaitSeconds, useVerificationCodeCooldown } from '@/hooks/use-verification-code-cooldown';
import {
  clerkRequestToPageErrors,
  getVerificationCodeRetryAfter,
  hasClerkErrorCode,
  type ClerkSignInRequestError,
} from '@/lib/clerk-sign-in';

export function ResetPasswordForm() {
  const { signIn, fetchStatus } = useSignIn();
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const codeInputRef = useRef<TextInputInstance>(null);
  const requestInProgress = useRef(false);
  const { pageErrors, setPageErrors, isFocused } = useAuthFormErrors();
  const { secondsLeft, startCooldown } = useVerificationCodeCooldown(signIn.identifier);
  const [isReSendingCode, setIsReSendingCode] = useState(false);
  const codeVerified = signIn.status === 'needs_new_password';

  function showResetError(error: ClerkSignInRequestError, fallback: string, globalTitle?: string) {
    if (!isFocused()) return;

    if (!signIn.id || hasClerkErrorCode(error, 'form_conditional_param_missing', 'client_state_invalid')) {
      router.replace('/forgot-password');
      return;
    }
    if (hasClerkErrorCode(error, 'verification_expired')) {
      if (signIn.status === 'needs_new_password') {
        router.replace('/forgot-password');
      } else {
        setPageErrors({ global: 'This verification code has expired. Request a new code and try again.' });
      }
      return;
    }
    const errors = clerkRequestToPageErrors(error, fallback);
    setPageErrors(globalTitle ? { ...errors, globalTitle } : errors);
  }

  function onPasswordSubmitEditing() {
    codeInputRef.current?.focus();
  }
  
  async function onSubmit() {
    if (!password || (!code && !codeVerified) || requestInProgress.current) {
      return;
    }
    if (!signIn.id) {
      router.replace('/forgot-password');
      return;
    }

    requestInProgress.current = true;
    setPageErrors({});

    try {
      if (!codeVerified) {
        const { error: verifyCodeError } = await signIn.resetPasswordEmailCode.verifyCode({ code });
        if (verifyCodeError) {
          showResetError(verifyCodeError, 'Could not verify the code. Please try again.');
          return;
        }
      }

      const { error: submitPasswordError } = await signIn.resetPasswordEmailCode.submitPassword({
        password,
      });

      if (submitPasswordError) {
        showResetError(submitPasswordError, 'Could not reset your password. Please try again.');
        return;
      }

      if (signIn.status === 'complete' && isFocused()) {
        // Set the active session to
        // the newly created session (user is now signed in)
        await signIn.finalize({
          navigate: () => {
            router.replace('/(app)/(tabs)/movies');
          },
        })
      }
    } catch (err) {
      console.error(err);
      setPageErrors({ global: 'Could not reset your password. Please try again.' });
    } finally {
      requestInProgress.current = false;
    }
  }

  async function onResendCode() {
    if (codeVerified || requestInProgress.current || fetchStatus === 'fetching') {
      return;
    }
    const identifier = signIn.identifier;
    if (!signIn.id || !identifier) {
      router.replace('/forgot-password');
      return;
    }
    if (getVerificationCodeWaitSeconds(identifier) > 0) return;

    requestInProgress.current = true;
    setIsReSendingCode(true);
    setPageErrors({});
    const resendErrorMessage = 'Could not resend the verification code. Please try again.';

    try {
      const { error: sendCodeError } = await signIn.resetPasswordEmailCode.sendCode();
      if (sendCodeError) {
        const retryAfter = getVerificationCodeRetryAfter(sendCodeError);
        if (retryAfter !== null) startCooldown(identifier, retryAfter);
        showResetError(sendCodeError, resendErrorMessage, 'Unable to send verification code');
        return;
      }
      setCode('');
      startCooldown(identifier);
    } catch (err) {
      console.error(err);
      if (!signIn.id) {
        if (isFocused()) router.replace('/forgot-password');
      } else {
        setPageErrors({ global: resendErrorMessage, globalTitle: 'Unable to send verification code' });
      }
    } finally {
      requestInProgress.current = false;
      setIsReSendingCode(false);
    }
  }


  const form = (
    <View className="gap-6">
      <Card className="border-border/0 shadow-none sm:border-border sm:shadow-sm sm:shadow-black/5">
        <CardHeader>
          <CardTitle className="text-center text-xl sm:text-left">Reset password</CardTitle>
          <CardDescription className="text-center sm:text-left">
            {codeVerified ? 'Code verified. Set a new password.' : 'Enter the code sent to your email and set a new password'}
          </CardDescription>

          {pageErrors.global &&
            <Alert variant="destructive" icon={AlertCircleIcon}>
              <AlertTitle>{pageErrors.globalTitle || 'Unable to Reset Password'}</AlertTitle>
              <AlertDescription>
                {pageErrors.global}
              </AlertDescription>
            </Alert>
          }
        </CardHeader>

        <CardContent className="gap-6">
          <View className="gap-6">
            <View className="gap-1.5">
              <View className="flex-row items-center">
                <Label htmlFor="password">New password</Label>
              </View>
              <Input
                id="password"
                secureTextEntry
                onChangeText={setPassword}
                returnKeyType={codeVerified ? 'send' : 'next'}
                submitBehavior="submit"
                onSubmitEditing={codeVerified ? onSubmit : onPasswordSubmitEditing}
              />
              
              {pageErrors?.password && 
                <Alert variant="destructive" icon={AlertCircleIcon}>
                  <AlertTitle>Unable to Reset Password</AlertTitle>
                  <AlertDescription>
                    {pageErrors?.password}
                  </AlertDescription>
                </Alert>
              }
            </View>

            {!codeVerified && (
              <View>
                <View className="gap-1.5">
                  <Label htmlFor="code">Verification code</Label>
                  <Input
                    ref={codeInputRef}
                    id="code"
                    value={code}
                    autoCapitalize="none"
                    onChangeText={setCode}
                    returnKeyType="send"
                    keyboardType="numeric"
                    autoComplete="sms-otp"
                    textContentType="oneTimeCode"
                    onSubmitEditing={onSubmit}
                    maxLength={6}
                  />

                  {pageErrors?.code &&
                    <Alert variant="destructive" icon={AlertCircleIcon}>
                      <AlertTitle>Unable to Reset Password</AlertTitle>
                      <AlertDescription>
                        {pageErrors?.code}
                      </AlertDescription>
                    </Alert>
                  }
                </View>

                <View
                  className="flex-row items-center justify-start"
                  style={{ marginTop: 8 }}
                >
                  <Text className="text-muted-foreground text-sm">Didn&apos;t get the code?</Text>
                  <Button
                    variant="link"
                    size="sm"
                    className="h-fit px-1 py-0"
                    onPress={onResendCode}
                    disabled={secondsLeft > 0 || isReSendingCode || fetchStatus === 'fetching'}
                  >
                    {isReSendingCode ? (
                      <ActivityIndicator size={16} color="black" />
                    ) : (
                      <Text className="text-sm">
                        {secondsLeft > 0 ? `Resend in ${secondsLeft}s` : 'Resend code'}
                      </Text>
                    )}
                  </Button>
                </View>
              </View>
            )}
            
            <Button
              className="w-full"
              onPress={onSubmit}
              disabled={!password || (!codeVerified && !code) || fetchStatus === 'fetching' || isReSendingCode}>
              <Text>Reset Password</Text>
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
