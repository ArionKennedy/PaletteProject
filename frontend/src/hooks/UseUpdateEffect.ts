import { useEffect, useRef, type DependencyList, type EffectCallback } from 'react';

// Custom hook that runs an effect only when the dependencies change, not on the initial render.
export function useUpdateEffect(effect: EffectCallback, dependencies: DependencyList) {
  const isMounted = useRef(false);

  useEffect(() => {
    if (!isMounted.current) {
      isMounted.current = true;
      return;
    }
    return effect();
  }, dependencies);
}
