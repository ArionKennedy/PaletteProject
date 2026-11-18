import { validateEmail, clerkErrorsToPageErrors } from '@/lib/validation';
import {
  EMPTY_EMAIL_ERROR,
  INVALID_EMAIL_ERROR,
} from '@/constants/validationConstants';

// No mocks needed — these are pure functions

describe('validateEmail()', () => {


  test('returns an error when the email is empty', () => {
    expect(validateEmail('')).toBe(EMPTY_EMAIL_ERROR);
  });



  test('returns an error when the email format is invalid', () => {
    expect(validateEmail('notanemail')).toBe(INVALID_EMAIL_ERROR);
    expect(validateEmail('missing@tld')).toBe(INVALID_EMAIL_ERROR);
    expect(validateEmail('@nodomain.com')).toBe(INVALID_EMAIL_ERROR);
  });



  test('returns null for a valid email address', () => {
    expect(validateEmail('user@example.com')).toBeNull();
    expect(validateEmail('user.name+tag@domain.co')).toBeNull();
  });


});

describe('clerkErrorsToPageErrors()', () => {


  test('returns an empty object when there are no errors', () => {
    expect(clerkErrorsToPageErrors({})).toEqual({});
  });



  test('maps a global error to pageErrors.global', () => {
    const errors = {
      fields: {
        global: [{ longMessage: 'Invalid credentials' }],
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      global: 'Invalid credentials',
    });
  });



  test('maps an identifier field error', () => {
    const errors = {
      fields: {
        identifier: { longMessage: 'Email not recognised' },
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      identifier: 'Email not recognised',
    });
  });



  test('maps an emailAddress field error', () => {
    const errors = {
      fields: {
        emailAddress: { longMessage: 'Email is already taken' },
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      emailAddress: 'Email is already taken',
    });
  });



  test('maps a password field error', () => {
    const errors = {
      fields: {
        password: { longMessage: 'Password is too short' },
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      password: 'Password is too short',
    });
  });



  test('maps a username field error', () => {
    const errors = {
      fields: {
        username: { longMessage: 'Username is already taken' },
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      username: 'Username is already taken',
    });
  });


  
  test('maps multiple field errors at once', () => {
    const errors = {
      fields: {
        emailAddress: { longMessage: 'Email is already taken' },
        password: { longMessage: 'Password is too short' },
      },
    };

    expect(clerkErrorsToPageErrors(errors)).toEqual({
      emailAddress: 'Email is already taken',
      password: 'Password is too short',
    });
  });
});
