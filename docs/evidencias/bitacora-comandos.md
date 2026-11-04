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

### #12 — Fase 1 · Repositorio
- Fecha/hora: 2026-11-03 22:34:22 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add -A
```
```text

```

### #13 — Fase 1 · Repositorio — verificar que no se suben secretos
- Fecha/hora: 2026-11-03 22:34:33 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git diff --cached --name-only | grep -Ei '(^|/)(\.env|api\.env)$|uploads/' || echo 'OK: ningun .env, api.env ni uploads/ en el staging'
```
```text
OK: ningun .env, api.env ni uploads/ en el staging
```

### #14 — Fase 1 · Repositorio — commit del estado base (antes de la intervencion SQA)
- Fecha/hora: 2026-11-03 22:34:39 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'chore: estado base del backend (Fases 2 y 3 del seminario) antes de la intervencion SQA' && git log --oneline -1
```
```text
0c8d137 chore: estado base del backend (Fases 2 y 3 del seminario) antes de la intervencion SQA
```

### #15 — Fase 1 · Repositorio — conectar remoto
- Fecha/hora: 2026-11-03 22:34:50 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git remote add origin https://github.com/zenvy-crm/seminario-project.git
```
```text

```

### #16 — Fase 1 · Repositorio
- Fecha/hora: 2026-11-03 22:34:53 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git branch -M main
```
```text

```

### #17 — Fase 1 · Repositorio — push inicial
- Fecha/hora: 2026-11-03 22:35:04 (America/Guatemala)
- Codigo de salida: 128
```bash
$ GIT_TERMINAL_PROMPT=0 git push -u origin main
```
```text
fatal: could not read Username for 'https://github.com': terminal prompts disabled
```

### #18 — Fase 1 · Repositorio — empaquetar historial para publicarlo (la VM no tiene credenciales de GitHub)
- Fecha/hora: 2026-11-03 22:35:21 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git bundle create docs/evidencias/.tmp-repo.bundle main && git bundle verify docs/evidencias/.tmp-repo.bundle
```
```text
docs/evidencias/.tmp-repo.bundle is okay
The bundle contains this ref:
0c8d13704a0de754e7ef9de7e7dc72d2bdf3c825 refs/heads/main
The bundle records a complete history.
```

### #19 — Fase 1 · Línea base — tamaño del código (cloc)
- Fecha/hora: 2026-11-03 22:36:49 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes cloc@2.0.0 seminario-backend-service --exclude-dir=bin,obj,.vs,uploads,.claude,Migrations --include-lang=C#
```
```text
     100 files     107 text files.
classified 107 filesDuplicate file check 107 files (106 known unique)Unique:      100 files                                               107 unique files.                              
Counting:  100      13 files ignored.

https://github.com/AlDanial/cloc v 1.66  T=5.05 s (19.0 files/s, 1136.3 lines/s)
-------------------------------------------------------------------------------
Language                     files          blank        comment           code
-------------------------------------------------------------------------------
C#                              96            875            479           4379
-------------------------------------------------------------------------------
SUM:                            96            875            479           4379
-------------------------------------------------------------------------------
npm notice
npm notice New major version of npm available! 10.9.9 -> 12.2.1
npm notice Changelog: https://github.com/npm/cli/releases/tag/v12.2.1
npm notice To update run: npm install -g npm@12.2.1
npm notice
```

### #20 — Fase 1 · Línea base — duplicación de código (jscpd)
- Fecha/hora: 2026-11-03 22:37:10 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes jscpd@4 seminario-backend-service --format csharp --ignore '**/Migrations/**,**/bin/**,**/obj/**,**/.vs/**' --min-tokens 50 --reporters consoleFull,json --output docs/evidencias/linea-base/jscpd --silent false 2>&1 | tail -40
```
```text
Error: ENOENT: no such file or directory, lstat '/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/mnt/seminario-backend-service/false'
    at realpathSync (node:fs:2801:29)
    at /sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/@jscpd/finder/dist/index.js:364:47
    at Array.map (<anonymous>)
    at getFilesToDetect (/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/@jscpd/finder/dist/index.js:363:45)
    at detectClones (/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/jscpd/dist/chunk-RJJ75QFW.js:371:42)
    at jscpd (/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/jscpd/dist/chunk-RJJ75QFW.js:439:12)
    at /sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/jscpd/dist/bin/jscpd.js:10:34
    at Object.<anonymous> (/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/.npm/_npx/107ce3791633fe64/node_modules/jscpd/dist/bin/jscpd.js:15:3)
    at Module._compile (node:internal/modules/cjs/loader:1781:14)
    at Object..js (node:internal/modules/cjs/loader:1913:10) {
  errno: -2,
  code: 'ENOENT',
  syscall: 'lstat',
  path: '/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/mnt/seminario-backend-service/false'
}
```

### #21 — Fase 1 · Línea base — duplicación de código (jscpd)
- Fecha/hora: 2026-11-03 22:37:44 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes jscpd@4 seminario-backend-service --format csharp --ignore '**/Migrations/**,**/bin/**,**/obj/**,**/.vs/**' --min-tokens 50 --reporters console,json --output docs/evidencias/linea-base/jscpd 2>&1 | tail -40
```
```text
   seminario-backend-service/Application/DTOs/Reportes/ReporteDtos.cs [73:28 - 80:102]

Clone found (csharp):
 - seminario-backend-service/Infrastructure/Services/ProfileService.cs [33:61 - 41:73] (8 lines, 105 tokens)
   seminario-backend-service/Infrastructure/Services/UserService.cs [99:57 - 107:69]

Clone found (csharp):
 - seminario-backend-service/Infrastructure/Services/ProfileService.cs [41:73 - 78:19] (37 lines, 354 tokens)
   seminario-backend-service/Infrastructure/Services/UserService.cs [107:69 - 144:16]

Clone found (csharp):
 - seminario-backend-service/Infrastructure/Services/ProfileService.cs [83:13 - 95:15] (12 lines, 88 tokens)
   seminario-backend-service/Infrastructure/Services/UserService.cs [153:13 - 165:11]

Clone found (csharp):
 - seminario-backend-service/Application/Services/AuthService.cs [69:45 - 78:69] (9 lines, 90 tokens)
   seminario-backend-service/Infrastructure/Services/UserService.cs [82:25 - 91:65]

Clone found (csharp):
 - seminario-backend-service/Application/Services/AuthService.cs [136:36 - 146:46] (10 lines, 90 tokens)
   seminario-backend-service/Infrastructure/Services/UserService.cs [219:41 - 229:44]

Clone found (csharp):
 - seminario-backend-service/Application/DTOs/UserDto.cs [4:1 - 11:18] (7 lines, 136 tokens)
   seminario-backend-service/Domain/Entities/User.cs [6:1 - 13:20]

┌────────┬────────────────┬─────────────┬──────────────┬──────────────┬──────────────────┬───────────────────┐
│ Format │ Files analyzed │ Total lines │ Total tokens │ Clones found │ Duplicated lines │ Duplicated tokens │
├────────┼────────────────┼─────────────┼──────────────┼──────────────┼──────────────────┼───────────────────┤
│ csharp │ 95             │ 5728        │ 51520        │ 10           │ 134 (2.34%)      │ 1410 (2.74%)      │
├────────┼────────────────┼─────────────┼──────────────┼──────────────┼──────────────────┼───────────────────┤
│ Total: │ 95             │ 5728        │ 51520        │ 10           │ 134 (2.34%)      │ 1410 (2.74%)      │
└────────┴────────────────┴─────────────┴──────────────┴──────────────┴──────────────────┴───────────────────┘
Found 10 clones.
JSON report saved to docs/evidencias/linea-base/jscpd/jscpd-report.json
time: 1.209s

💡 Auto-refactor with AI: npx skills add kucherenko/jscpd
🎩 New: Gangsta Agents — discipline your AI coding → gangsta.page
💖 Support jscpd project → https://opencollective.com/jscpd
```

### #22 — Fase 1 · Frontend — ignorar archivos locales del IDE y de build
- Fecha/hora: 2026-11-03 22:42:53 (America/Guatemala)
- Codigo de salida: 0
```bash
$ printf '.claude/\n.idea/\n*.tsbuildinfo\ncoverage/\n' >> .gitignore && cat .gitignore
```
```text
node_modules
dist
.env
.env.local
*.log
.claude/
.idea/
*.tsbuildinfo
coverage/
```

### #23 — Fase 1 · Frontend
- Fecha/hora: 2026-11-03 22:42:55 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add -A && git status --short
```
```text
warning: unable to unlink '.git/objects/8f/tmp_obj_wjkVnO': Operation not permitted
warning: unable to unlink '.git/objects/54/tmp_obj_Sy476V': Operation not permitted
warning: unable to unlink '.git/objects/72/tmp_obj_MBhTs2': Operation not permitted
warning: unable to unlink '.git/objects/b5/tmp_obj_GcihC8': Operation not permitted
warning: unable to unlink '.git/objects/4d/tmp_obj_2hNIV9': Operation not permitted
warning: unable to unlink '.git/objects/2c/tmp_obj_IR4fwb': Operation not permitted
warning: unable to unlink '.git/objects/cb/tmp_obj_gHwv7h': Operation not permitted
warning: unable to unlink '.git/objects/d7/tmp_obj_kPj5Lj': Operation not permitted
warning: unable to unlink '.git/objects/bf/tmp_obj_haPOFe': Operation not permitted
warning: unable to unlink '.git/objects/e5/tmp_obj_TpZcrg': Operation not permitted
warning: unable to unlink '.git/objects/e6/tmp_obj_9Bvgnh': Operation not permitted
warning: unable to unlink '.git/objects/93/tmp_obj_fDW8Xj': Operation not permitted
warning: unable to unlink '.git/objects/da/tmp_obj_dUVVto': Operation not permitted
warning: unable to unlink '.git/objects/f0/tmp_obj_fFDEJt': Operation not permitted
warning: unable to unlink '.git/objects/d1/tmp_obj_z1nhLB': Operation not permitted
warning: unable to unlink '.git/objects/95/tmp_obj_RCGzJG': Operation not permitted
warning: unable to unlink '.git/objects/a8/tmp_obj_taAS0K': Operation not permitted
warning: unable to unlink '.git/objects/14/tmp_obj_RC2u9P': Operation not permitted
warning: unable to unlink '.git/objects/34/tmp_obj_3WNTHQ': Operation not permitted
warning: unable to unlink '.git/objects/96/tmp_obj_f1IVTV': Operation not permitted
D  .env.example
M  .gitignore
D  API_CONTRACT.md
D  README.md
M  package.json
M  pnpm-lock.yaml
M  src/App.tsx
M  src/api/authApi.ts
M  src/api/documentsApi.ts
A  src/api/fileValidator.ts
D  src/api/mock/db.ts
D  src/api/mock/mockAuth.ts
D  src/api/mock/mockUsers.ts
A  src/api/operacionesApi.ts
M  src/api/profileApi.ts
A  src/api/reportesApi.ts
M  src/api/usersApi.ts
M  src/components/common/Badges.tsx
M  src/components/layout/Sidebar.tsx
M  src/components/users/UserForm.tsx
M  src/pages/auth/Login.tsx
M  src/pages/auth/Mfa.tsx
A  src/pages/operaciones/Operaciones.tsx
A  src/pages/ordenes/OrdenCompraPage.tsx
A  src/pages/reportes/AuditoriaAnulacionesPage.tsx
A  src/pages/reportes/RankingProveedoresPage.tsx
A  src/pages/reportes/ReporteOrdenesPage.tsx
A  src/pages/reportes/comunes.tsx
M  src/pages/users/UsersList.tsx
M  src/types/index.ts
A  vite.config.d.ts
A  vite.config.js
```

### #24 — Fase 1 · Frontend — no versionar archivos generados por tsc -b (vite.config.js / .d.ts)
- Fecha/hora: 2026-11-03 22:43:19 (America/Guatemala)
- Codigo de salida: 0
```bash
$ printf 'vite.config.js\nvite.config.d.ts\n' >> .gitignore && git rm -q --cached vite.config.js vite.config.d.ts && git add .gitignore && git status --short | grep -E 'vite|gitignore'
```
```text
M  .gitignore
```

### #25 — Fase 1 · Frontend — commit del estado base
- Fecha/hora: 2026-11-03 22:43:28 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'chore: estado base del frontend (Fases 2 y 3) antes de la intervencion SQA' && git log --oneline -1
```
```text
0e97f0a chore: estado base del frontend (Fases 2 y 3) antes de la intervencion SQA
```

### #26 — Fase 1 · Frontend — empaquetar historial para integrarlo al monorepo
- Fecha/hora: 2026-11-03 22:43:31 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git bundle create /tmp/frontend.bundle HEAD && mkdir -p '/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/mnt/seminario-backend-service/docs/evidencias' && cp /tmp/frontend.bundle '/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/mnt/seminario-backend-service/docs/evidencias/.tmp-frontend.bundle' && git bundle verify /tmp/frontend.bundle
```
```text
/tmp/frontend.bundle is okay
The bundle contains this ref:
0e97f0a9a711fcd478f6182e556cf1d12fe3df68 HEAD
The bundle records a complete history.
```

### #27 — Fase 1 · Línea base frontend — tamaño (cloc)
- Fecha/hora: 2026-11-03 22:44:06 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes cloc@2.0.0 src --include-lang=TypeScript,JavaScript
```
```text
      43 text files.
classified 43 files      43 unique files.                              
      32 files ignored.

https://github.com/AlDanial/cloc v 1.66  T=1.44 s (7.6 files/s, 418.2 lines/s)
-------------------------------------------------------------------------------
Language                     files          blank        comment           code
-------------------------------------------------------------------------------
TypeScript                      11             72             28            503
-------------------------------------------------------------------------------
SUM:                            11             72             28            503
-------------------------------------------------------------------------------
```

### #28 — Fase 1 · Línea base frontend — duplicación (jscpd)
- Fecha/hora: 2026-11-03 22:44:10 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes jscpd@4 src --min-tokens 50 --reporters console,json --output /tmp/jscpd-front 2>&1 | tail -12 && cp /tmp/jscpd-front/jscpd-report.json '/sessions/rcw-01q7qtmzfqcoseboj79cxhcn/mnt/seminario-backend-service/docs/evidencias/jscpd-frontend.json'
```
```text
├────────────┼────────────────┼─────────────┼──────────────┼──────────────┼──────────────────┼───────────────────┤
│ typescript │ 10             │ 602         │ 5925         │ 0            │ 0 (0%)           │ 0 (0%)            │
├────────────┼────────────────┼─────────────┼──────────────┼──────────────┼──────────────────┼───────────────────┤
│ Total:     │ 73             │ 5645        │ 80661        │ 7            │ 76 (1.35%)       │ 1587 (1.97%)      │
└────────────┴────────────────┴─────────────┴──────────────┴──────────────┴──────────────────┴───────────────────┘
Found 7 clones.
JSON report saved to /tmp/jscpd-front/jscpd-report.json
time: 1.170s

💡 Auto-refactor with AI: npx skills add kucherenko/jscpd
🎩 New: Gangsta Agents — discipline your AI coding → gangsta.page
💖 Support jscpd project → https://opencollective.com/jscpd
```

### #29 — Fase 1 · Línea base frontend — tamaño incluyendo .tsx (cloc no reconoce tsx por defecto)
- Fecha/hora: 2026-11-03 22:44:20 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes cloc@2.0.0 src --force-lang=TypeScript,tsx --include-lang=TypeScript
```
```text
      43 text files.
classified 43 files      43 unique files.                              
       0 files ignored.

https://github.com/AlDanial/cloc v 1.66  T=1.86 s (23.1 files/s, 2133.8 lines/s)
-------------------------------------------------------------------------------
Language                     files          blank        comment           code
-------------------------------------------------------------------------------
TypeScript                      43            353             68           3556
-------------------------------------------------------------------------------
SUM:                            43            353             68           3556
-------------------------------------------------------------------------------
```

