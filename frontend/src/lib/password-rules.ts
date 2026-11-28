const MIN_PASSWORD_LENGTH = 15;

export const passwordRules = [
  {
    label: `At least ${MIN_PASSWORD_LENGTH} characters`,
    test: (password: string) => Array.from(password).length >= MIN_PASSWORD_LENGTH,
  },
];

export function meetsPasswordRequirements(password: string): boolean {
  return passwordRules.every(({ test }) => test(password));
}
