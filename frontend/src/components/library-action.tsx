import { useCallback, useRef, useState } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { useFocusEffect } from 'expo-router';
import { Minus, Plus } from 'lucide-react-native';

import { useTheme } from '@/hooks/use-theme';
import { getLibraryMembership, setLibraryMembership } from '@/services/library';
import type { LibraryMediaType } from '@/types/library';

type LibraryActionProps = {
  type: LibraryMediaType;
  externalId: string;
  initialSaved?: boolean;
  onError: (message: string | null) => void;
};

export function LibraryAction({ type, externalId, initialSaved, onError }: LibraryActionProps) {
  const theme = useTheme();
  const [saved, setSaved] = useState(initialSaved);
  const [saving, setSaving] = useState(false);
  const didFocus = useRef(false);
  const mutationVersion = useRef(0);
  const pending = useRef(false);

  useFocusEffect(useCallback(() => {
    if (!didFocus.current && initialSaved !== undefined) {
      didFocus.current = true;
      return;
    }

    // Keep the existing icon visible if a retained screen regains focus.
    didFocus.current = true;
    let active = true;
    const version = mutationVersion.current;
    const canApply = () => active && !pending.current && version === mutationVersion.current;
    if (!pending.current) {
      void getLibraryMembership(type, externalId)
        .then(result => {
          if (canApply()) {
            setSaved(result.saved);
            onError(null);
          }
        })
        .catch((problem: unknown) => {
          if (canApply() && initialSaved === undefined) {
            onError(problem instanceof Error ? problem.message : 'Could not check your library.');
          }
        });
    }
    return () => { active = false; };
  }, [externalId, initialSaved, onError, type]));

  async function toggle() {
    if (saved === undefined || pending.current) return;
    pending.current = true;
    mutationVersion.current += 1;
    setSaving(true);
    setSaved(!saved);
    onError(null);
    try {
      const result = await setLibraryMembership(type, externalId, !saved);
      setSaved(result.saved);
    } catch (problem) {
      setSaved(saved);
      onError(problem instanceof Error ? problem.message : 'Could not update your library.');
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }

  if (saved === undefined) return null;
  const ActionIcon = saved ? Minus : Plus;
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={saved ? 'Remove from My Library' : 'Add to My Library'}
      accessibilityState={{ busy: saving, disabled: saving }}
      disabled={saving}
      onPress={toggle}
      style={({ pressed }) => [styles.button, pressed && styles.pressed]}>
      <ActionIcon size={28} strokeWidth={3} color={theme.text} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  button: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center' },
  pressed: { opacity: 0.55 },
});
