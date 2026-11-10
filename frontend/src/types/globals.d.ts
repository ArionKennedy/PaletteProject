export {}

// TODO document how to access using sessionClaims.username
declare global {
  interface CustomJwtSessionClaims {
    username?: string
    email?: string
  }
}