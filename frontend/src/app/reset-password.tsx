import { ResetPasswordForm } from '@/components/reset-password-form';
import { View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
 
export default function ForgotPasswordScreen() {
  return (
    <SafeAreaView className="flex-1 bg-background">
      <View className="flex-1 items-center justify-center p-4">
        <View className="w-full max-w-sm">
          <ResetPasswordForm />
        </View>
      </View>
    </SafeAreaView>
  );
}