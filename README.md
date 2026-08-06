# seminario-backend-service

API REST en ASP.NET Core 8 (autenticacion JWT + MFA, roles ADMIN/USER, documentos, auditoria asincrona a MongoDB).

## Requisitos

- .NET 8 SDK
- SQL Server y MongoDB accesibles (ver Tarea 2 de infraestructura)

## Configuracion

1. Copiar `seminario-backend-service/.env.example` como `seminario-backend-service/.env` y completar con las credenciales reales.
2. El archivo `.env` es solo para desarrollo local y nunca debe subirse al repositorio (ya esta en `.gitignore`).

## Ejecutar

```
cd seminario-backend-service
dotnet run
```

Al iniciar, la aplicacion aplica las migraciones de EF Core automaticamente y crea (si no existen) los roles `ADMIN`/`USER` y un usuario administrador:

- Usuario: `admin`
- Password: `Admin#2026!`

Swagger queda disponible en `/swagger` en modo desarrollo, con soporte para autenticacion Bearer.

## MFA

El segundo factor es un codigo temporal (OTP) de 6 digitos, valido por 5 minutos. En este entorno de desarrollo, al no existir integracion con SMS/email, el codigo se imprime en la consola del backend (nivel `Information`) al momento de iniciar sesion.

## Auditoria

Los eventos de auditoria (login, cambios de usuario, documentos, etc.) se encolan en memoria y se escriben de forma asincrona a la coleccion `audit_logs` de MongoDB mediante un `BackgroundService`. Una falla temporal de MongoDB no afecta la respuesta de la operacion principal.
