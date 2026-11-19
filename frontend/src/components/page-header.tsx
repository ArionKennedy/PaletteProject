import { UserMenu } from '@/components/user-menu';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/lib/theme';
import * as React from 'react';
import {StyleSheet, Text, View } from 'react-native';

type PageHeaderProps = {
  title: string;
  titleAction?: React.ReactNode;
  showUserMenu?: boolean;
};

export function PageHeader({ title, titleAction, showUserMenu = true }: PageHeaderProps) {
  const theme = useTheme();

  return (
    <View style={styles.container}>
      <View style={[styles.titleRow, titleAction ? styles.titleRowWithAction : undefined]}>
        <Text accessibilityRole="header" style={[styles.title, { color: theme.text }]}>
          {title}
        </Text>
        {titleAction}
      </View>
      {showUserMenu && <UserMenu />}
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
  titleRow: { flexDirection: 'row', alignItems: 'center', gap: Spacing.two, flexShrink: 1 },
  titleRowWithAction: { alignItems: 'baseline' },
  title: {
    flexShrink: 1,
    fontSize: 34,
    fontWeight: '700',
  },
});
