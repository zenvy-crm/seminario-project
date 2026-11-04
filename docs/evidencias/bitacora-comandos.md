# Bitácora de comandos — Proyecto Final SQA

Registro cronológico de cada comando ejecutado durante el proyecto, uno por uno, con fecha, hora, código de salida y salida de consola.

Entorno de ejecución: VM Linux (Ubuntu 22.04) del equipo de desarrollo, con el repositorio montado.

### #1 — Fase 1 · Entorno
- Fecha/hora: 2026-11-03 22:30:07 (America/Guatemala)
- Codigo de salida: 0
```bash
$ uname -a
```
```text
Linux claude 6.8.0-138-generic #138~22.04.1-Ubuntu SMP PREEMPT_DYNAMIC Fri Aug  7 13:43:15 UTC  x86_64 x86_64 x86_64 GNU/Linux
```

### #2 — Fase 1 · Entorno
- Fecha/hora: 2026-11-03 22:30:12 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git --version
```
```text
git version 2.34.1
```

### #3 — Fase 1 · Entorno
- Fecha/hora: 2026-11-03 22:30:14 (America/Guatemala)
- Codigo de salida: 127
```bash
$ dotnet --version
```
```text
bash: line 1: dotnet: command not found
```

### #4 — Fase 1 · Entorno
- Fecha/hora: 2026-11-03 22:30:17 (America/Guatemala)
- Codigo de salida: 127
```bash
$ docker --version
```
```text
bash: line 1: docker: command not found
```

### #5 — Fase 1 · Entorno — instalar .NET SDK
- Fecha/hora: 2026-11-03 22:30:21 (America/Guatemala)
- Codigo de salida: 56
```bash
$ curl -sSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && echo descargado
```
```text
curl: (56) Received HTTP code 403 from proxy after CONNECT
```

### #6 — Fase 1 · Entorno — instalar .NET SDK
- Fecha/hora: 2026-11-03 22:30:26 (America/Guatemala)
- Codigo de salida: 1
```bash
$ sudo -n true && echo sudo-ok
```
```text
sudo: /etc/sudo.conf is owned by uid 65534, should be 0
sudo: The "no new privileges" flag is set, which prevents sudo from running as root.
sudo: If sudo is running in a container, you may need to adjust the container configuration to disable the flag.
```

### #7 — Fase 1 · Entorno — conectividad
- Fecha/hora: 2026-11-03 22:30:49 (America/Guatemala)
- Codigo de salida: 56
```bash
$ curl -s -o /dev/null -w 'api.nuget.org -> HTTP %{http_code}\n' -m 10 https://api.nuget.org/v3/index.json
```
```text
api.nuget.org -> HTTP 000
```

### #8 — Fase 1 · Repositorio
- Fecha/hora: 2026-11-03 22:34:00 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git status --short --branch
```
```text
## master
 M .gitignore
 M README.md
 M postman/seminario-backend-service.postman_collection.json
 M seminario-backend-service/.claude/launch.json
 M seminario-backend-service/Application/Audit/AuditEvent.cs
 M seminario-backend-service/Application/Audit/AuditEventTypes.cs
 M seminario-backend-service/Application/Common/ApiException.cs
 M seminario-backend-service/Application/Interfaces/IAuditLogger.cs
 M seminario-backend-service/Application/Interfaces/IDocumentService.cs
 M seminario-backend-service/Application/Interfaces/IFileStorageService.cs
 M seminario-backend-service/Application/Services/AuthService.cs
 M seminario-backend-service/Controllers/AuthController.cs
 M seminario-backend-service/Controllers/DocumentsController.cs
 M seminario-backend-service/Domain/Entities/UserDocument.cs
 M seminario-backend-service/Domain/Enums/RoleName.cs
 M seminario-backend-service/Infrastructure/Auth/MfaService.cs
 M seminario-backend-service/Infrastructure/Auth/TokenService.cs
 M seminario-backend-service/Infrastructure/MongoDB/AuditBackgroundService.cs
 M seminario-backend-service/Infrastructure/MongoDB/AuditChannel.cs
 M seminario-backend-service/Infrastructure/MongoDB/AuditLogDocument.cs
 M seminario-backend-service/Infrastructure/MongoDB/MongoSettings.cs
 M seminario-backend-service/Infrastructure/Persistence/AppDbContext.cs
 M seminario-backend-service/Infrastructure/Persistence/Configurations/UserDocumentConfiguration.cs
 M seminario-backend-service/Infrastructure/Persistence/DbSeeder.cs
 M seminario-backend-service/Infrastructure/Persistence/Migrations/20260806065422_InitialCreate.Designer.cs
 M seminario-backend-service/Infrastructure/Persistence/Migrations/20260806065422_InitialCreate.cs
 M seminario-backend-service/Infrastructure/Persistence/Migrations/20260806203257_AddAccountLockout.Designer.cs
 M seminario-backend-service/Infrastructure/Persistence/Migrations/20260806203257_AddAccountLockout.cs
 M seminario-backend-service/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
 M seminario-backend-service/Infrastructure/Services/DocumentService.cs
 M seminario-backend-service/Infrastructure/Services/FileStorageService.cs
 M seminario-backend-service/Infrastructure/Services/ProfileService.cs
 M seminario-backend-service/Infrastructure/Services/UserService.cs
 M seminario-backend-service/Program.cs
 M seminario-backend-service/Properties/launchSettings.json
 M seminario-backend-service/appsettings.Development.json
 M seminario-backend-service/appsettings.json
 M seminario-backend-service/seminario-backend-service.csproj
 M seminario-backend-service/seminario-backend-service.slnx
?? api.env.example
?? deploy.ps1
?? docker-compose.yml
?? docs/
?? seminario-backend-service/.claude/settings.local.json
?? seminario-backend-service/.dockerignore
?? seminario-backend-service/Api/AuthorizationPolicies.cs
?? seminario-backend-service/Api/RateLimitPolicies.cs
?? seminario-backend-service/Api/RateLimitingExtensions.cs
?? seminario-backend-service/Api/RoleClaimsExtensions.cs
?? seminario-backend-service/Application/Common/UsuarioSesion.cs
?? seminario-backend-service/Application/DTOs/Operaciones/
?? seminario-backend-service/Application/DTOs/Reportes/
?? seminario-backend-service/Application/Interfaces/IAuditStore.cs
?? seminario-backend-service/Application/Interfaces/IOrdenCompraService.cs
?? seminario-backend-service/Application/Interfaces/IPasswordHasher.cs
?? seminario-backend-service/Application/Interfaces/IReporteService.cs
?? seminario-backend-service/Controllers/HealthController.cs
?? seminario-backend-service/Controllers/OperacionesController.cs
?? seminario-backend-service/Controllers/ReportesController.cs
?? seminario-backend-service/Controllers/SeedDataController.cs
?? seminario-backend-service/Dockerfile
?? seminario-backend-service/Domain/Entities/OrdenCompra.cs
?? seminario-backend-service/Domain/Entities/OrdenCompraDetalle.cs
?? seminario-backend-service/Domain/Entities/PresupuestoBodega.cs
?? seminario-backend-service/Domain/Enums/OrdenEstado.cs
?? seminario-backend-service/Infrastructure/Auth/BcryptPasswordHasher.cs
?? seminario-backend-service/Infrastructure/Health/
?? seminario-backend-service/Infrastructure/MongoDB/MongoAuditStore.cs
?? seminario-backend-service/Infrastructure/Persistence/AppDbContextFactory.cs
?? seminario-backend-service/Infrastructure/Persistence/Configurations/OrdenCompraConfiguration.cs
?? seminario-backend-service/Infrastructure/Persistence/Configurations/OrdenCompraDetalleConfiguration.cs
?? seminario-backend-service/Infrastructure/Persistence/Configurations/PresupuestoBodegaConfiguration.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260829024753_AddOrdenesCompraParticionada.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260829024753_AddOrdenesCompraParticionada.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260829033009_AddUserDocumentRowVersion.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260829033009_AddUserDocumentRowVersion.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260926031522_Fase2OrdenCompraDetalle.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260926031522_Fase2OrdenCompraDetalle.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929001718_UserDocumentSoftDelete.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929001718_UserDocumentSoftDelete.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929001731_SeedPresupuestoBodegasFrontend.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929001731_SeedPresupuestoBodegasFrontend.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929043210_Fase3ReportesRanking.Designer.cs
?? seminario-backend-service/Infrastructure/Persistence/Migrations/20260929043210_Fase3ReportesRanking.cs
?? seminario-backend-service/Infrastructure/Persistence/OrdenCompraSqlObjects.cs
?? seminario-backend-service/Infrastructure/Services/OrdenCompraService.cs
?? seminario-backend-service/Infrastructure/Services/ReporteService.cs
?? seminario-backend-service/Infrastructure/Services/SeedDataService.cs
?? sql/
```

### #9 — Fase 1 · Repositorio
- Fecha/hora: 2026-11-03 22:34:07 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git log --format='%h %ad %an %s' --date=short
```
```text
12949c8 2026-08-09 Alder Solis chore: agregar config de launch para levantar zenvy-core en preview
4655183 2026-08-06 Alder Solis docs: documentar validaciones de documentos y limitacion conocida de PDF
f8bc389 2026-08-06 Alder Solis feat: endpoint para descargar/ver el contenido de un documento
2fef4cd 2026-08-06 Alder Solis fix: sanitizar nombre de archivo (caracteres de control) y limitar su longitud
0cd866a 2026-08-06 Alder Solis feat: bloquear cuenta tras 3 intentos fallidos de login, con auto-desbloqueo a los 5 min y desbloqueo manual del admin
a9860ca 2026-08-06 Alder Solis feat: capturar el actor (admin) que ejecuta cada accion auditada
ad3f3e9 2026-08-06 Alder Solis fix: validar el documento de registro antes de crear el usuario
40e22d5 2026-08-06 Alder Solis feat: capturar la ip del cliente de forma centralizada en todos los eventos de auditoria
3546cce 2026-08-06 Alder Solis feat: auditar MFA_FAILED tambien cuando el codigo esta expirado o es invalido
7c51bba 2026-08-06 Alder Solis fix: unificar puerto de desarrollo (5000) y desactivar redireccion HTTPS forzada
c82a436 2026-08-06 Alder Solis docs: agregar coleccion y environment de Postman
2f5ec61 2026-08-06 Alder Solis docs: readme con instrucciones de ejecucion y credenciales por defecto
31887fc 2026-08-06 Alder Solis feat: configurar Swagger con soporte de autenticacion Bearer JWT
aa224ff 2026-08-06 Alder Solis feat: middleware global de errores y respuestas HTTP consistentes
de05b05 2026-08-06 Alder Solis feat: endpoints de perfil y cambio de contrasena
52ebf07 2026-08-06 Alder Solis feat: administracion de usuarios (ADMIN) y endpoints de documentos
236bfa0 2026-08-06 Alder Solis feat: autenticacion con JWT, MFA por codigo temporal, refresh y logout
605b7a4 2026-08-06 Alder Solis feat: validacion y almacenamiento seguro de documentos (PDF/JPEG)
3d60df5 2026-08-06 Alder Solis feat: DTOs de autenticacion, usuarios, perfil y documentos
f99fc44 2026-08-06 Alder Solis feat: auditoria asincrona con Channel, BackgroundService y MongoDB
50a1fd9 2026-08-06 Alder Solis feat: entidades de dominio, DbContext y migracion inicial de EF Core
dc07a00 2026-08-06 Alder Solis chore: agregar paquetes NuGet (EF Core, JWT, Identity, MongoDB)
6717473 2026-08-06 Alder Solis feat: cargar variables de entorno de desarrollo con DotNetEnv
d89a82e 2026-08-06 Alder Solis fix: regenerar archivo de solucion (.slnx) invalido
9f44a36 2026-08-06 Alder Solis chore: inicializar repositorio del backend (scaffold ASP.NET Core)
```

### #10 — Fase 1 · Repositorio — limpiar config ajena (zenvy-core)
- Fecha/hora: 2026-11-03 22:34:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git rm --cached seminario-backend-service/.claude/launch.json
```
```text
rm 'seminario-backend-service/.claude/launch.json'
```

### #11 — Fase 1 · Repositorio — ignorar carpeta .claude
- Fecha/hora: 2026-11-03 22:34:18 (America/Guatemala)
- Codigo de salida: 0
```bash
$ sed -i 's#^\.claude/settings\.local\.json$#.claude/#' .gitignore && grep -n claude .gitignore
```
```text
11:.claude/
```

