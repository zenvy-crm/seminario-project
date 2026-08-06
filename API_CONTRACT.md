# Contrato Frontend ↔ Backend (API REST .NET 8)

Base URL: `VITE_API_URL` (ej. `http://localhost:5000/api`). Todas las respuestas de error deben incluir `message` en español, apto para mostrar al usuario.

## Tipos compartidos (`src/types/index.ts`)

```ts
type Role = "ADMIN" | "USER";
type UserStatus = "ACTIVE" | "INACTIVE";

interface AppDocument { id: number; fileName: string; mimeType: string; sizeBytes: number; uploadedAt: string; url: string; }

interface User {
  id: number; firstName: string; lastName: string; username: string; email: string;
  phone?: string; birthDate?: string; identification?: string;
  role: Role; status: UserStatus; createdAt: string; lastLoginAt?: string;
  documents?: AppDocument[];
}

interface AuthUser { id: number; username: string; firstName: string; lastName: string; role: Role; }

interface AuthResponse {
  success: boolean;
  requiresMfa?: boolean;
  message?: string;
  data: { accessToken?: string; expiresAt?: string; user?: AuthUser; mfaToken?: string; };
}

interface PaginatedResponse<T> { items: T[]; total: number; page: number; pageSize: number; }
```

## Auth

- `POST /auth/login` — body `{ username, password }` → `AuthResponse`. Si requiere MFA: `{ success: true, requiresMfa: true, data: { mfaToken } }` (sin `accessToken` todavía). Si no requiere MFA: `{ success: true, data: { accessToken, expiresAt, user } }`. Credenciales inválidas: `{ success: false, message }` con HTTP 401.
- `POST /auth/mfa/verify` — body `{ mfaToken, code }` → `AuthResponse` con `{ success: true, data: { accessToken, expiresAt, user } }`. Código inválido/expirado: `{ success: false, message }`.
- `POST /auth/register` — `multipart/form-data` con los campos del formulario (`firstName, lastName, username, email, phone, birthDate, identification, password, confirmPassword`) + `document` (archivo, PDF/JPG/JPEG). Respuesta: `AuthResponse` (`data` puede ir vacío).
- `POST /auth/refresh` — sin body (usa cookie/refresh token) → `AuthResponse`.
- `POST /auth/logout` — sin body.

## Usuarios (solo ADMIN)

- `GET /users?search=&role=&status=&page=&pageSize=&sortBy=&sortDir=` → `PaginatedResponse<User>`.
- `GET /users/:id` → `User`.
- `POST /users` — body `Partial<User> & { password }` → `User` creado.
- `PUT /users/:id` — body `Partial<User>` (nunca `passwordHash`) → `User` actualizado.
- El cambio de estado y de rol usan el mismo `PUT /users/:id` con `{ status }` o `{ role }` respectivamente.

## Documentos

- `GET /users/:id/documents` → `AppDocument[]`.
- `POST /users/:id/documents` — `multipart/form-data`, campo `file` → `AppDocument`.
- `DELETE /users/:id/documents/:documentId`.

## Perfil (usuario autenticado)

- `GET /profile` → `User` (el propio usuario).
- `PUT /profile` — body `Partial<User>` (campos autorizados: nombre, apellido, email, teléfono, identificación) → `User`.
- `POST /profile/change-password` — body `{ currentPassword, newPassword }`.

## Seguridad

- Todas las rutas protegidas requieren header `Authorization: Bearer <accessToken>`.
- 401 en cualquier respuesta ⇒ el frontend limpia la sesión y redirige a `/login`.
- El backend nunca debe devolver `passwordHash` en ningún endpoint de usuario/perfil.
- La protección de `/users*` a nivel de UI (rol ADMIN) es solo UX — el backend debe rechazar con 403 cualquier request de un `USER` a esos endpoints.
