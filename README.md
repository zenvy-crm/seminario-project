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

## Documentos: validaciones y limitacion conocida

Cada archivo subido se valida por extension, Content-Type declarado, tamano maximo (5MB), longitud y caracteres del nombre, y firma binaria real (magic bytes) para PDF/JPEG. Esto bloquea archivos con extension incorrecta, contenido que no coincide con la extension declarada, y nombres de archivo maliciosos (path traversal, bytes de control).

**Limitacion conocida:** no se inspecciona el contenido interno de los PDF aceptados. Un PDF valido puede llevar JavaScript embebido (`/JavaScript`, `/OpenAction`, `/AA`) que algunos lectores de PDF vulnerables podrian ejecutar al abrirlo. Sanitizar esto requeriria una libreria de parseo de PDF (ej. iText7, que es AGPL o de pago) o un escaner de malware externo (ej. ClamAV), lo cual se considero fuera de alcance para este proyecto.
