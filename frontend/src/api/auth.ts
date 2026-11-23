import { apiRequest } from './client.ts'
import { authRequest } from './session.ts'
import type {
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  RegisterUserRequest,
  UserResponse,
} from './types.ts'

// One function per call of /api/auth, so a page does not repeat the path and the method.

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', { method: 'POST', body: request })
}

export function register(request: RegisterUserRequest): Promise<UserResponse> {
  return apiRequest<UserResponse>('/auth/register', { method: 'POST', body: request })
}

// Ends the session on the server: the API revokes this refresh token.
export function logout(refreshToken: string): Promise<void> {
  return apiRequest<void>('/auth/logout', { method: 'POST', body: { refreshToken } })
}

// Changes the password of the signed-in user (204 No Content).
// After it, the API ends every session of the user, this one too:
// the refresh tokens are revoked and the access tokens stop working.
export function changePassword(request: ChangePasswordRequest): Promise<void> {
  return authRequest<void>('/auth/password', { method: 'PUT', body: request })
}

// The signed-in user, read from the database (not from the token).
export function getMe(): Promise<UserResponse> {
  return authRequest<UserResponse>('/auth/me')
}
