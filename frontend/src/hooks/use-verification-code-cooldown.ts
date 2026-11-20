import { useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';

const defaultCooldownSeconds = 30;
const resendDeadlines = new Map<string, number>();

export function getVerificationCodeWaitSeconds(identifier: string) {
  const deadline = resendDeadlines.get(identifier.trim().toLowerCase()) ?? 0;
  return Math.max(0, Math.ceil((deadline - Date.now()) / 1000));
}

export function useVerificationCodeCooldown(identifier: string | null | undefined) {
  const email = identifier?.trim().toLowerCase() ?? '';
  const [countdown, setCountdown] = useState(() => ({
    email,
    seconds: getVerificationCodeWaitSeconds(email),
  }));
  const secondsLeft = countdown.email === email ? countdown.seconds : 0;
  const isCountingDown = secondsLeft > 0;

  useFocusEffect(useCallback(() => {
    function refreshCountdown() {
      setCountdown({ email, seconds: getVerificationCodeWaitSeconds(email) });
    }

    refreshCountdown();
    if (!isCountingDown) return;

    const timer = setInterval(refreshCountdown, 1000);
    return () => clearInterval(timer);
  }, [email, isCountingDown]));

  function startCooldown(address: string, retryAfter = defaultCooldownSeconds) {
    const key = address.trim().toLowerCase();
    const now = Date.now();
    for (const [storedEmail, deadline] of resendDeadlines) {
      if (deadline <= now) resendDeadlines.delete(storedEmail);
    }
    const seconds = Math.max(defaultCooldownSeconds, Math.ceil(retryAfter));
    resendDeadlines.set(key, now + seconds * 1000);
    setCountdown({ email: key, seconds });
  }

  return { secondsLeft, startCooldown };
}
