import { Button } from '@/components/ui/button';
import { Icon } from '@/components/ui/icon';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Text } from '@/components/ui/text';
import { cn } from '@/lib/utils';
import { CheckIcon, CircleIcon, EyeIcon, EyeOffIcon } from 'lucide-react-native';
import { useState, type ComponentProps } from 'react';
import { View } from 'react-native';
import { passwordRules } from '@/lib/password-rules';

type NewPasswordFieldProps = Omit<
  ComponentProps<typeof Input>,
  'secureTextEntry' | 'value'
> & {
  id: string;
  label: string;
  value: string;
};

export function NewPasswordField({ id, label, value, className, ...inputProps }: NewPasswordFieldProps) {
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);

  return (
    <View className="gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <View className="flex-row items-center gap-2">
        <Input
          autoComplete="new-password"
          autoCapitalize="none"
          autoCorrect={false}
          {...inputProps}
          className={cn('w-auto flex-1', className)}
          id={id}
          value={value}
          secureTextEntry={!isPasswordVisible}
        />
        <Button
          variant="outline"
          size="icon"
          accessibilityLabel={isPasswordVisible ? 'Hide password' : 'Show password'}
          onPress={() => setIsPasswordVisible(visible => !visible)}
        >
          <Icon as={isPasswordVisible ? EyeOffIcon : EyeIcon} size={18} />
        </Button>
      </View>
      {passwordRules.map(({label, test}) => {
        const isMet = test(value);

        return (
          <View key={label} className="flex-row items-center gap-1.5">
            <Icon 
              as={isMet ? CheckIcon : CircleIcon} 
              className="text-muted-foreground" 
            />
            <Text
              className="text-sm text-muted-foreground"
              accessibilityLabel={`${label}, ${isMet ? 'met' : 'not met'}`}
            >
              {label}
            </Text>
          </View>
        );
      })}
    </View>
  );
}
