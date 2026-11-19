import { apiRequest } from './client.ts'
import type { LoginRequest, LoginResponse, RegisterUserRequest, UserResponse } from './types.ts'

// One function per call of /api/auth, so a page does not repeat the path and the method.

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', { method: 'POST', body: request })
}

export function register(request: RegisterUserRequest): Promise<UserResponse> {
  return apiRequest<UserResponse>('/auth/register', { method: 'POST', body: request })
}
