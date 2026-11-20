import { isClerkAPIResponseError, type useSignIn } from '@clerk/expo';

type SignIn = ReturnType<typeof useSignIn>['signIn'];
export type ClerkSignInRequestError = NonNullable<Awaited<ReturnType<SignIn['create']>>['error']>;

function getErrorDetails(error: ClerkSignInRequestError) {
  return isClerkAPIResponseError(error) ? error.errors : [error];
}

export function hasClerkErrorCode(error: ClerkSignInRequestError, ...codes: string[]) {
  return getErrorDetails(error).some(detail => codes.includes(detail.code));
}

const verificationCodeLimitErrors = [
  'verification_code_rate_limit_exceeded',
  'verification_code_too_many_requests',
];

export function getVerificationCodeRetryAfter(error: ClerkSignInRequestError) {
  if (isClerkAPIResponseError(error) && error.status === 429) {
    const { retryAfter } = error;
    return typeof retryAfter === 'number' && Number.isFinite(retryAfter) ? retryAfter : 0;
  }

  return hasClerkErrorCode(error, ...verificationCodeLimitErrors) ? 0 : null;
}

export function clerkRequestToPageErrors(error: ClerkSignInRequestError, fallback: string) {
  const sendLimit = getErrorDetails(error).find(detail => verificationCodeLimitErrors.includes(detail.code));
  if (sendLimit) {
    return {
      global: sendLimit.longMessage || sendLimit.message || fallback,
      globalTitle: 'Unable to send verification code',
    };
  }

  if (!isClerkAPIResponseError(error)) return { global: error.longMessage || fallback };

  const fields: Record<string, string> = {};
  for (const detail of error.errors) {
    const field = detail.meta?.paramName;
    const key = field === 'identifier' || field === 'password' || field === 'code' ? field : 'global';
    fields[key] ??= detail.longMessage || detail.message || fallback;
  }
  return Object.keys(fields).length > 0 ? fields : { global: fallback };
}

export function canResumePasswordReset(signIn: SignIn, emailAddress: string) {
  const verification = signIn.firstFactorVerification;
  if (!signIn.id || signIn.identifier?.toLowerCase() !== emailAddress.toLowerCase()
    || verification?.strategy !== 'reset_password_email_code') {
    return false;
  }

  return signIn.status === 'needs_new_password'
    || (signIn.status === 'needs_first_factor' && verification.status === 'unverified'
      && (!verification.expireAt || verification.expireAt.getTime() > Date.now()));
}
