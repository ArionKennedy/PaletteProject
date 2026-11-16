import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { Icon } from '@/components/ui/icon';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Text } from '@/components/ui/text';
import { cn } from '@/lib/utils';
import type { TriggerRef } from '@rn-primitives/popover';
import { LogOutIcon, SettingsIcon } from 'lucide-react-native';
import * as React from 'react';
import { View } from 'react-native';
import { useAuth, useUser } from '@clerk/expo';

export function UserMenu() {
  const { signOut } = useAuth();
  const { user } = useUser();
  const popoverTriggerRef = React.useRef<TriggerRef>(null);

  async function onSignOut() {
    popoverTriggerRef.current?.close();
    await signOut();
  }

  return (
    <Popover>
      <PopoverTrigger asChild ref={popoverTriggerRef}>
        <Button variant="ghost" size="icon" className="size-8 rounded-full">
          <UserAvatar user={user} />
        </Button>
      </PopoverTrigger>
      <PopoverContent align="center" side="bottom" className="w-80 p-0">
        <View className="border-border gap-3 border-b p-3">
          <View className="flex-row items-center gap-3">
            <UserAvatar user={user} className="size-10" />
            <View className="flex-1">
              <Text className="font-medium leading-5">{user?.username}</Text>
              {user?.primaryEmailAddress?.emailAddress ? (
                <Text className="text-muted-foreground text-sm font-normal leading-4">
                  {user?.primaryEmailAddress?.emailAddress}
                </Text>
              ) : null}
            </View>
          </View>
          <View className="flex-row flex-wrap gap-3 py-0.5">
            {/* <Button
              variant="outline"
              size="sm"
              onPress={() => {
                // TODO: Navigate to account settings screen
              }}>
              <Icon as={SettingsIcon} className="size-4" />
              <Text>Manage Account</Text>
            </Button> */}
            <Button variant="outline" size="sm" className="flex-1" onPress={onSignOut}>
              <Icon as={LogOutIcon} className="size-4" />
              <Text>Sign Out</Text>
            </Button>
          </View>
        </View>
      </PopoverContent>
    </Popover>
  );
}

function UserAvatar({ user, className, ...props }: Omit<React.ComponentProps<typeof Avatar>, 'alt'> & { user: any }) {
  return (
    <Avatar alt={`${user?.username}'s avatar`} className={cn('size-8', className)} {...props}>
      {user?.imageUrl ? <AvatarImage source={{ uri: user?.imageUrl }} /> : null}
      <AvatarFallback>
        <Text>{user?.username?.charAt(0)?.toUpperCase()}</Text>
      </AvatarFallback>
    </Avatar>
  );
}
