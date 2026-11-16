// Mirrors the password rules enforced by ASP.NET Identity in the API.
const PASSWORD_RULES = [
  [p => p.length >= 8, 'at least 8 characters'],
  [p => /[a-z]/.test(p), 'a lowercase letter'],
  [p => /[A-Z]/.test(p), 'an uppercase letter'],
  [p => /\d/.test(p), 'a digit'],
  [p => /[^A-Za-z0-9]/.test(p), 'a special character']
]

export const PASSWORD_HINT = 'At least 8 characters, with upper and lower case letters, a digit and a special character.'

// Returns a message describing what the password is missing, or an empty string if it is valid.
export function validatePassword(password) {
  const missing = PASSWORD_RULES.filter(([test]) => !test(password)).map(([, text]) => text)
  return missing.length > 0 ? `Password must contain ${missing.join(', ')}.` : ''
}