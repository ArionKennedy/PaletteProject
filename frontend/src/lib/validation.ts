import { EMAIL_REGEX, EMPTY_EMAIL_ERROR, INVALID_EMAIL_ERROR } from '@/constants/validationConstants';

export function validateEmail(email: string): string | null {
  if (!email) {
    return EMPTY_EMAIL_ERROR;
  }

  if (!EMAIL_REGEX.test(email)) {
    return INVALID_EMAIL_ERROR;
  }

  return null;
}

export function clerkErrorsToPageErrors(errors: Record<string, any>): Record<string, string> {
  const pageErrors: Record<string, string> = {};

  const globalError = errors?.global?.[0];
  if (globalError) {
    pageErrors.global = globalError?.longMessage || globalError?.message;
  }

  if (errors?.fields?.identifier) {
    pageErrors.identifier = errors?.fields?.identifier?.longMessage;
  }

  if (errors?.fields?.emailAddress) {
    pageErrors.emailAddress = errors?.fields?.emailAddress?.longMessage;
  }

  if (errors?.fields?.username) {
    pageErrors.username = errors?.fields?.username?.longMessage;
  }

  if (errors?.fields?.password) {
    pageErrors.password = errors?.fields?.password?.longMessage;
  }

  if (errors?.fields?.code) {
    pageErrors.code = errors?.fields?.code?.longMessage;
  }

  return pageErrors;
}
