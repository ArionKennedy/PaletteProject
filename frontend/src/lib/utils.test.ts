import { cn } from '@/lib/utils';

describe('cn', () => {
  it('merges class names and Tailwind overrides', () => {
    expect(cn('px-2 text-sm', 'text-lg px-4')).toBe('text-lg px-4');
  });

  it('ignores false and null values', () => {
    expect(cn('base', false, null, undefined, 'active')).toBe('base active');
  });
});
