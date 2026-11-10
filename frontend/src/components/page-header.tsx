import { UserMenu } from '@/components/user-menu';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/lib/theme';
import * as React from 'react';
import {StyleSheet, Text, View } from 'react-native';

type PageHeaderProps = {
  title: string;
};

export function PageHeader({ title }: PageHeaderProps) {
  const theme = useTheme();

  return (
    <View style={styles.container}>
      <Text accessibilityRole="header" style={[styles.title, { color: theme.text }]}>
        {title}
      </Text>
      <UserMenu />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: '100%',
    alignSelf: 'stretch',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  title: {
    fontSize: 34,
    fontWeight: '700',
  },
});
