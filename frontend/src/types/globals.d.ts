export {}

declare global {
  interface CustomJwtSessionClaims {
    username?: string
    email?: string
  }
}