# Sistema de Gestión — Frontend (React + TypeScript + Vite)

Frontend consumidor de la API REST en C# .NET 8: login, registro, MFA, dashboard, sidebar por rol, gestión de usuarios, perfil y documentos.

## Cómo ejecutar

```bash
npm install
cp .env.example .env
npm run dev
```

Abre `http://localhost:5173`.

## Modo mock (datos dummy)

`VITE_USE_MOCK_API=true` (valor por defecto en `.env.example`) hace que los servicios en `src/api/*Api.ts` respondan con datos en memoria (`src/api/mock/`), sin backend real. Usuarios de prueba:

- `admin@bowpi.com` / `Admin123!` → rol **ADMIN**
- `jperez@bowpi.com` / `User123!` → rol **USER**
- Código MFA (ambos usuarios): `123456`

Cuando el backend esté disponible, cambia `VITE_USE_MOCK_API=false` y define `VITE_API_URL`. Ningún componente cambia: los servicios ya llaman a los mismos endpoints documentados en `API_CONTRACT.md`.

## Estructura

```
src/
├── api/          servicios (authApi, usersApi, profileApi, documentsApi) + mocks
├── auth/         AuthContext, ProtectedRoute, RoleRoute
├── components/   layout (Sidebar/Header/MainLayout), users, common (toasts, estados, badges)
├── pages/        auth, dashboard, users, profile, errors
├── theme/        tema MUI alineado al design system Industry
└── types/        contratos TypeScript compartidos con el backend
```

## Contrato con el backend

Ver `API_CONTRACT.md`: endpoints, payloads y respuestas esperadas. Los tipos en `src/types/index.ts` son la fuente de verdad — cualquier cambio de contrato debe reflejarse ahí primero.
