export type Role = "ADMIN" | "USER";
export type UserStatus = "ACTIVE" | "INACTIVE";

export interface AppDocument {
  id: number;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  uploadedAt: string;
  url: string;
}

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  username: string;
  email: string;
  phone?: string;
  birthDate?: string;
  identification?: string;
  role: Role;
  status: UserStatus;
  createdAt: string;
  lastLoginAt?: string;
  isLocked?: boolean;
  lockedUntil?: string;
  documents?: AppDocument[];
}

export interface AuthUser {
  id: number;
  username: string;
  firstName: string;
  lastName: string;
  role: Role;
}

export interface AuthResponse {
  success: boolean;
  requiresMfa?: boolean;
  message?: string;
  data: {
    accessToken?: string;
    expiresAt?: string;
    user?: AuthUser;
    mfaToken?: string;
  };
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
