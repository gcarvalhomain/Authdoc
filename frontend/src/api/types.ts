// The shapes of the JSON that the API sends and receives.
// Each type has the same name as its C# DTO in Authdoc/Src/Application/DTOs.
//
// JSON has no date type, so every date arrives as text:
// - date and time (DateTime in C#): "2026-11-15T14:30:00Z"
// - date only (DateOnly in C#): "2026-11-15"

// ---------- Responses (what the API sends) ----------

export type UserRole = 'Admin' | 'User'

export type UserResponse = {
  id: string
  email: string
  name: string
  role: UserRole
  createdAt: string
  updatedAt: string
}

// The answer of POST /api/auth/login and POST /api/auth/refresh.
export type LoginResponse = {
  user: UserResponse
  token: string
  expiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export type DocumentResponse = {
  id: string
  type: string
  name: string
  number: string
  description: string | null
  issuedAt: string
  expiresAt: string | null
  createdAt: string
  updatedAt: string
}

// One page of a list. T is the type of each item,
// for example PagedResponse<DocumentResponse>.
export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// The body of every error answer.
export type ErrorResponse = {
  code: string
  message: string
}

// ---------- Requests (what the frontend sends) ----------

export type LoginRequest = {
  email: string
  password: string
}

export type RegisterUserRequest = {
  name: string
  email: string
  password: string
  confirmationPassword: string
}

export type ChangePasswordRequest = {
  currentPassword: string
  newPassword: string
  confirmationPassword: string
}

export type RefreshTokenRequest = {
  refreshToken: string
}

// Create and update of a document send the same fields, so one type is enough.
export type DocumentRequest = {
  type: string
  name: string
  number: string
  description: string | null
  // Optional only so that an empty date can be left out. Without it, the API
  // answers ISSUED_AT_REQUIRED; with an empty text ("") it answers INVALID_REQUEST.
  issuedAt?: string
  expiresAt: string | null
}
