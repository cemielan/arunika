export type AuthFieldErrors = {
  email?: string;
  password?: string;
  confirmPassword?: string;
};

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function normalizeEmail(email: string): string {
  return email.trim().toLowerCase();
}

export function validateEmail(email: string): string | undefined {
  if (!email.trim()) return "Email is required.";
  if (!EMAIL_REGEX.test(email.trim())) return "Enter a valid email address.";
  return undefined;
}

export function validatePasswordForSignIn(password: string): string | undefined {
  if (!password) return "Password is required.";
  return undefined;
}

export function validatePasswordForSignUp(password: string): string | undefined {
  if (!password) return "Password is required.";
  if (password.length < 8) return "Password must be at least 8 characters.";
  return undefined;
}

export function validateSignInFields(email: string, password: string): AuthFieldErrors {
  const emailError = validateEmail(email);
  const passwordError = validatePasswordForSignIn(password);

  return {
    email: emailError,
    password: passwordError,
  };
}

export function validateSignUpFields(
  email: string,
  password: string,
  confirmPassword: string,
): AuthFieldErrors {
  const emailError = validateEmail(email);
  const passwordError = validatePasswordForSignUp(password);
  const confirmPasswordError = !confirmPassword
    ? "Please confirm your password."
    : password !== confirmPassword
      ? "Passwords do not match."
      : undefined;

  return {
    email: emailError,
    password: passwordError,
    confirmPassword: confirmPasswordError,
  };
}

export function hasAuthFieldErrors(errors: AuthFieldErrors): boolean {
  return Boolean(errors.email || errors.password || errors.confirmPassword);
}