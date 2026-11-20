import { useFocusEffect } from 'expo-router';
import { useCallback, useRef, useState } from 'react';

type PageErrors = Record<string, string>;

export function useAuthFormErrors() {
  const [pageErrors, setErrors] = useState<PageErrors>({});
  const focused = useRef(false);
  const timeout = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearErrorTimeout = useCallback(() => {
    if (timeout.current !== null) clearTimeout(timeout.current);
    timeout.current = null;
  }, []);

  useFocusEffect(useCallback(() => {
    focused.current = true;
    return () => {
      focused.current = false;
      clearErrorTimeout();
      setErrors({});
    };
  }, [clearErrorTimeout]));

  const setPageErrors = useCallback((errors: PageErrors) => {
    if (!focused.current) return;
    clearErrorTimeout();
    setErrors(errors);

    if (Object.keys(errors).length > 0) {
      timeout.current = setTimeout(() => {
        timeout.current = null;
        setErrors({});
      }, 5000);
    }
  }, [clearErrorTimeout]);

  const isFocused = useCallback(() => focused.current, []);

  return { pageErrors, setPageErrors, isFocused };
}
