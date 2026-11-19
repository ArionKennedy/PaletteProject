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
import { clerkErrorsToPageErrors } from '@/lib/validation';
import { useUpdateEffect } from '@/hooks/UseUpdateEffect';

export function ResetPasswordForm() {
  const { signIn, errors, fetchStatus } = useSignIn();
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const codeInputRef = useRef<TextInputInstance>(null);
  const [pageErrors, setPageErrors] = useState<Record<string, string>>({});
  const [verificationError, setVerificationError] = useState('');
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [isReSendingCode, setIsReSendingCode] = useState(false);

  useUpdateEffect(() => {
    setPageErrors(clerkErrorsToPageErrors(errors));

    const globalError = errors?.global?.[0];
    if(globalError?.code === 'verification_expired'){
      setVerificationError('This verification code has expired. Request a new code and try again.');
    }
    else if(errors?.fields?.identifier?.code === `form_conditional_param_missing`){
      router.replace('/forgot-password');
    }
    else if(globalError?.isClerkAPIResponseError()) {
      if(globalError?.errors?.[0]?.code === 'client_state_invalid'){
        router.replace('/forgot-password');
      }
    }
  }, [errors]);

  // Handle the countdown timer
  useUpdateEffect(() => {
    if (secondsLeft <= 0) return;

    const timer = setInterval(() => {
      setSecondsLeft((prev) => prev - 1);
    }, 1000);

    return () => clearInterval(timer);
  }, [secondsLeft]);
  
  function onPasswordSubmitEditing() {
    codeInputRef.current?.focus();
  }
  
  async function onSubmit() {
    setPageErrors({});
    setVerificationError('');

    try {
      const { error: verifyCodeError } = await signIn.resetPasswordEmailCode.verifyCode({
        code,
      });

      if (verifyCodeError) {
        console.error(verifyCodeError);
        return;
      }

      const { error: submitPasswordError } = await signIn.resetPasswordEmailCode.submitPassword({
        password,
      });

      if (submitPasswordError) {
        console.error(submitPasswordError);
        return;
      }

      if (signIn.status === 'complete') {
        // Set the active session to
        // the newly created session (user is now signed in)
        await signIn.finalize({
          navigate: () => {
            router.replace('/(app)/(tabs)/movies');
          },
        })
      }
      else{
        console.error('reset password attempt not complete:', signIn);
      }
    } catch (err) {
      console.error(err);
    }
  }

  async function onResendCode() {
    if (secondsLeft > 0 || isReSendingCode) {
      return;
    }

    setIsReSendingCode(true);

    const { error: sendCodeError } = await signIn.resetPasswordEmailCode.sendCode();

    if (sendCodeError) {
      setIsReSendingCode(false);
      return;
    }
    setSecondsLeft(30);
    setIsReSendingCode(false);
  }


  const form = (
    <View className="gap-6">
      <Card className="border-border/0 shadow-none sm:border-border sm:shadow-sm sm:shadow-black/5">
        <CardHeader>
          <CardTitle className="text-center text-xl sm:text-left">Reset password</CardTitle>
          <CardDescription className="text-center sm:text-left">
            Enter the code sent to your email and set a new password
          </CardDescription>

          {(verificationError || pageErrors?.global) &&
            <Alert variant="destructive" icon={AlertCircleIcon}>
              <AlertTitle>Unable to Reset Password</AlertTitle>
              <AlertDescription>
                {verificationError || pageErrors?.global}
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
                returnKeyType="next"
                submitBehavior="submit"
                onSubmitEditing={onPasswordSubmitEditing}
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

            <View>
              <View className="gap-1.5">
                <Label htmlFor="code">Verification code</Label>
                <Input
                  ref={codeInputRef}
                  id="code"
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
                {secondsLeft > 0 ? (
                  <Text>
                    {`You can resend the code again in ${secondsLeft}s`}
                  </Text>
                ) : (
                  <>
                    <Text className="text-muted-foreground text-sm">
                      {`Didn't get the code? `}
                    </Text>
                    <Button
                      variant="link"
                      size="sm"
                      className="h-fit px-1 py-0"
                      onPress={onResendCode}
                      disabled={isReSendingCode}
                    >
                      <>
                        <Text
                          className="text-base text-sm"
                          style={isReSendingCode ? { visibility: 'hidden' } : undefined}
                        >
                          Resend code
                        </Text>
                        {isReSendingCode && (
                          <View
                            style={{
                              position: 'absolute',
                              top: 0,
                              right: 0,
                              bottom: 0,
                              left: 0,
                              alignItems: 'center',
                              justifyContent: 'center',
                            }}
                          >
                            <ActivityIndicator size={16} color="black" />
                          </View>
                        )}
                      </>
                    </Button>
                  </>
                )}
              </View>
            </View>
            
            <Button className={"w-full"} onPress={onSubmit} disabled={!code || !password || fetchStatus === 'fetching'}>
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
