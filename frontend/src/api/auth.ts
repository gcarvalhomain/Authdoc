import { apiRequest } from './client.ts'
import type { LoginRequest, LoginResponse } from './types.ts'

// One function per call of /api/auth, so a page does not repeat the path and the method.

export function login(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/auth/login', { method: 'POST', body: request })
}
