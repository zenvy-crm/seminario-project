# Matriz de pruebas

Casos diseñados para el proyecto, con el resultado obtenido en el pipeline. Cada ID aparece en el nombre de la prueba (`DisplayName` en xUnit, descripción en Vitest), así que se puede buscar en el código y en los logs de GitHub Actions.

**Resultado obtenido (backend):** [run 41779244372](https://github.com/zenvy-crm/seminario-project/actions/runs/41779244372), commit `1b418d2`.  
**Resultado obtenido (frontend):** el mismo run (job *2 · Pruebas frontend*, 45/45), confirmado con el reporte JUnit local `frontend/coverage/junit.xml`.

## Resumen

| Tipo | Casos diseñados | Ejecuciones (incluye filas de `Theory`/`each`) | Aprobadas |
|---|---|---|---|
| Unitaria | 90 | 103 | 103 |
| Integración | 39 | 47 | 47 |
| Unitaria (frontend) | 40 | 45 | 45 |
| **Total** | **169** | **195** | **195** |

Casos de **regresión** (protegen defectos ya corregidos): **16**. Están marcados con `[Trait("Category", "Regression")]` en xUnit o con *regresión* en el nombre en Vitest, y se pueden ejecutar solos con `dotnet test --filter Category=Regression`.

### Origen de los casos de regresión

| Caso(s) | Defecto o riesgo original | Commit de la corrección |
|---|---|---|
| UT-AUTH-09, UT-AUTH-11, UT-USR-13, IT-AUTH-03 | Bloqueo tras 3 intentos fallidos, desbloqueo automático a los 5 min y desbloqueo manual | `0cd866a` |
| UT-FS-03, UT-FS-04, IT-DOC-03 | Nombres de archivo con caracteres de control o demasiado largos | `2fef4cd` |
| IT-AUTH-07 | El usuario se creaba aunque el documento de registro fuera inválido | `ad3f3e9` |
| UT-MFA-03, UT-MFA-05 | No se auditaba MFA_FAILED con un código expirado o inválido | `3546cce` |
| FE-API-06 | El frontend enviaba `role=ALL`/`status=ALL` como filtro literal | `80d7e43` (frontend) |
| IT-OC-03 | Rollback absoluto del SP cuando una línea es inválida | Fase 2 del seminario |
| IT-OC-08 | El rol Estándar no debe ver órdenes ANULADA (soft delete) | Fase 2 del seminario |
| IT-AUTH-05 | Rate limiting de 5 intentos/min por IP en el login | Fase 2 del seminario |
| UT-RBAC-02 | El claim `nivel` del JWT no puede escalar privilegios | Fase 2 del seminario |
| FE-REG-01 | Regex de zona horaria sin precedencia explícita (bug S5850 de SonarCloud) | Esta intervención |

## Pruebas unitarias — backend

| ID | Regresión | Componente | Entrada / escenario | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|
| UT-API-01 |  | Middleware / RBAC / Auditoría | ApiException | Se traduce a su codigo HTTP con cuerpo JSON uniforme | ✅ Aprobado |
| UT-API-02 |  | Middleware / RBAC / Auditoría | Excepcion no controlada | Responde 500 sin filtrar detalles internos | ✅ Aprobado |
| UT-API-03 |  | Middleware / RBAC / Auditoría | Middleware con un siguiente delegado que responde 204 | 204 sin modificar | ✅ Aprobado |
| UT-API-04 |  | Middleware / RBAC / Auditoría | NotFound, Conflict, Forbidden y UnauthorizedApiException | 404, 409, 403 y 401 respectivamente | ✅ Aprobado (4/4) |
| UT-API-05 |  | Middleware / RBAC / Auditoría | ConflictException | Conserva la excepcion original | ✅ Aprobado |
| UT-RBAC-01 |  | Middleware / RBAC / Auditoría | Roles USER, intermedio, ADMIN, OTRO y null | Niveles 1, 2, 3, 0 y 0; rol_auth Estandar, Intermedio, Administrador, OTRO y "" | ✅ Aprobado (5/5) |
| UT-RBAC-02 | Sí | Middleware / RBAC / Auditoría | JWT con rol USER y claim nivel=3 | GetNivel() = 1 (no hay escalamiento de privilegios) | ✅ Aprobado |
| UT-RBAC-03 |  | Middleware / RBAC / Auditoría | JWT con role=INTERMEDIO, sin claim nivel | Nivel 2 | ✅ Aprobado |
| UT-RBAC-04 |  | Middleware / RBAC / Auditoría | Claims sub=21, username=estandar, rol USER, nivel 1 | UsuarioSesion(21, estandar, USER, 1) con EsEstandar = true | ✅ Aprobado |
| UT-RBAC-05 |  | Middleware / RBAC / Auditoría | Token sin usuario valido | Responde 403 | ✅ Aprobado |
| UT-RBAC-06 |  | Middleware / RBAC / Auditoría | GetUserId | Lee NameIdentifier y el username cae a ClaimTypes.Name | ✅ Aprobado |
| UT-RBAC-07 |  | Middleware / RBAC / Auditoría | Id no numerico | Se interpreta como 0 | ✅ Aprobado |
| UT-MAP-01 |  | Middleware / RBAC / Auditoría | IsLocked solo | Es true mientras el bloqueo esta vigente | ✅ Aprobado |
| UT-MAP-02 |  | Middleware / RBAC / Auditoría | El DTO | Expone la URL de descarga de cada documento y nunca el hash | ✅ Aprobado |
| UT-AUD-01 |  | Middleware / RBAC / Auditoría | Enqueue | Completa ip (X-Forwarded-For), ejecutor y rol desde el JWT | ✅ Aprobado |
| UT-AUD-02 |  | Middleware / RBAC / Auditoría | Enqueue sin HttpContext | Evento encolado sin IP ni actor y sin excepción | ✅ Aprobado |
| UT-AUD-03 |  | Middleware / RBAC / Auditoría | FireAndForget | Reintenta cuando MongoDB falla y termina guardando | ✅ Aprobado |
| UT-AUD-04 |  | Middleware / RBAC / Auditoría | MongoAuditStore.ToDocument | Normaliza la fecha a UTC y omite textos vacios | ✅ Aprobado |
| UT-PWD-01 |  | BCrypt / TokenService | Hash BCrypt | Verifica la contrasena correcta y rechaza la incorrecta | ✅ Aprobado |
| UT-PWD-02 |  | BCrypt / TokenService | Hash vacio o PBKDF2 corrupto | Nunca se acepta | ✅ Aprobado (2/2) |
| UT-PWD-03 |  | BCrypt / TokenService | Hash heredado PBKDF2 valido | Se acepta y se marca para re-hash | ✅ Aprobado |
| UT-PWD-04 |  | BCrypt / TokenService | Hash BCrypt con costo menor al vigente | Se marca para re-hash | ✅ Aprobado |
| UT-TKN-01 |  | BCrypt / TokenService | Access token | Incluye sub, username, rol y nivel RBAC | ✅ Aprobado |
| UT-TKN-02 |  | BCrypt / TokenService | Refresh token | Es aleatorio y seguro para URL | ✅ Aprobado |
| UT-TKN-03 |  | BCrypt / TokenService | HashToken | Es SHA-256 hexadecimal y determinista | ✅ Aprobado |
| UT-FS-01 |  | FileStorageService | Archivo vacio | Responde 422 | ✅ Aprobado |
| UT-FS-02 |  | FileStorageService | Archivo mayor a 5 MB | Responde 422 | ✅ Aprobado |
| UT-FS-03 | Sí | FileStorageService | Nombre con caracteres de control | Responde 422 | ✅ Aprobado |
| UT-FS-04 | Sí | FileStorageService | Nombre de mas de 200 caracteres | Responde 422 | ✅ Aprobado |
| UT-FS-05 |  | FileStorageService | Extension no permitida | Responde 422 | ✅ Aprobado (3/3) |
| UT-FS-06 |  | FileStorageService | Content-Type distinto a la extension | Responde 422 | ✅ Aprobado |
| UT-FS-07 |  | FileStorageService | Contenido que no corresponde a la extension (firma) | Responde 422 | ✅ Aprobado |
| UT-FS-08 |  | FileStorageService | PDF valido | Se guarda en la carpeta del usuario con nombre aleatorio | ✅ Aprobado |
| UT-FS-09 |  | FileStorageService | JPEG valido | Se acepta | ✅ Aprobado |
| UT-FS-10 |  | FileStorageService | Lectura con path traversal (../) | Responde 404 | ✅ Aprobado |
| UT-FS-11 |  | FileStorageService | Lectura de un archivo inexistente | Responde 404 | ✅ Aprobado |
| UT-FS-12 |  | FileStorageService | Delete (compensacion) | Borra el archivo y es idempotente | ✅ Aprobado |
| UT-FS-13 |  | FileStorageService | Delete | Ignora rutas fuera de la carpeta de uploads | ✅ Aprobado |
| UT-MFA-01 |  | MfaService | Generar | Guarda el codigo hasheado (nunca en claro) con vigencia de 5 minutos | ✅ Aprobado |
| UT-MFA-02 |  | MfaService | Codigo correcto | Devuelve el usuario y lo marca como usado | ✅ Aprobado |
| UT-MFA-03 | Sí | MfaService | Reusar un codigo ya usado | Responde 401 y audita MFA_FAILED | ✅ Aprobado |
| UT-MFA-04 |  | MfaService | Token MFA inexistente | Responde 401 | ✅ Aprobado |
| UT-MFA-05 | Sí | MfaService | Codigo expirado | Responde 401 y audita MFA_FAILED | ✅ Aprobado |
| UT-MFA-06 |  | MfaService | Codigo incorrecto | Incrementa los intentos | ✅ Aprobado |
| UT-MFA-07 |  | MfaService | Superar el maximo de intentos | Bloquea el codigo aunque sea correcto | ✅ Aprobado |
| UT-AUTH-01 |  | AuthService | Registro | Rechaza un username duplicado (409) | ✅ Aprobado |
| UT-AUTH-02 |  | AuthService | Registro | Rechaza un correo duplicado (409) | ✅ Aprobado |
| UT-AUTH-03 |  | AuthService | Registro válido sin el rol USER sembrado | ApiException 500 | ✅ Aprobado |
| UT-AUTH-04 |  | AuthService | Registro valido | Guarda el hash (nunca la contrasena) y audita USER_CREATED | ✅ Aprobado |
| UT-AUTH-05 |  | AuthService | BirthDate = "no-es-fecha" | El usuario se guarda con BirthDate null | ✅ Aprobado |
| UT-AUTH-06 |  | AuthService | Login con usuario inexistente | Responde 401 y audita LOGIN_FAILED | ✅ Aprobado |
| UT-AUTH-07 |  | AuthService | Login de una cuenta INACTIVE | Responde 403 | ✅ Aprobado |
| UT-AUTH-08 |  | AuthService | Usuario con LockedUntil = ahora + 3 min | 403 sin llamar a Verify() | ✅ Aprobado |
| UT-AUTH-09 | Sí | AuthService | LockedUntil vencido, 3 intentos fallidos y la clave correcta | Se desbloquea (intentos = 0, LockedUntil null), se audita ACCOUNT_UNLOCKED y se emite el desafío MFA | ✅ Aprobado |
| UT-AUTH-10 |  | AuthService | Contrasena incorrecta | Incrementa el contador y responde 401 | ✅ Aprobado |
| UT-AUTH-11 | Sí | AuthService | Tercer intento fallido | Bloquea la cuenta 5 minutos (403) | ✅ Aprobado |
| UT-AUTH-12 |  | AuthService | Clave correcta con 2 intentos previos | Intentos = 0, RequiresMfa = true, se devuelve el mfaToken y no hay access token | ✅ Aprobado |
| UT-AUTH-13 |  | AuthService | Hash PBKDF2 heredado y la clave correcta | PasswordHash regenerado con BCrypt y auditoría PASSWORD_REHASHED | ✅ Aprobado |
| UT-AUTH-14 |  | AuthService | Login correcto sin cambios pendientes | No guarda en la base | ✅ Aprobado |
| UT-AUTH-15 |  | AuthService | MFA valido | Emite access token, guarda el refresh hasheado y audita el login | ✅ Aprobado |
| UT-AUTH-16 |  | AuthService | Refresh sin cookie | Responde 401 | ✅ Aprobado (2/2) |
| UT-AUTH-17 |  | AuthService | Refresh con token revocado o expirado | Responde 401 | ✅ Aprobado |
| UT-AUTH-18 |  | AuthService | Refresh valido | Rota el token: revoca el anterior y emite uno nuevo | ✅ Aprobado |
| UT-AUTH-19 |  | AuthService | Logout sin cookie | No hace nada | ✅ Aprobado |
| UT-AUTH-20 |  | AuthService | Refresh token activo "abc" | RevokeAsync + SaveChanges y auditoría LOGOUT | ✅ Aprobado |
| UT-AUTH-21 |  | AuthService | Refresh token desconocido | Sin auditoría LOGOUT | ✅ Aprobado |
| UT-PRF-01 |  | ProfileService | Perfil de un usuario inexistente | Responde 404 | ✅ Aprobado |
| UT-PRF-02 |  | ProfileService | GetAsync(10) de un usuario existente | DTO con el username y la lista de documentos | ✅ Aprobado |
| UT-PRF-03 |  | ProfileService | Actualizar perfil sin cambios | No guarda ni audita | ✅ Aprobado |
| UT-PRF-04 |  | ProfileService | Actualizar perfil | Guarda y audita los campos cambiados | ✅ Aprobado |
| UT-PRF-05 |  | ProfileService | Actualizar perfil con correo de otro usuario | Responde 409 | ✅ Aprobado |
| UT-PRF-06 |  | ProfileService | Actualizar perfil de usuario inexistente | Responde 404 | ✅ Aprobado |
| UT-PRF-07 |  | ProfileService | Cambio de contrasena con la actual incorrecta | Responde 401 | ✅ Aprobado |
| UT-PRF-08 |  | ProfileService | Cambio de contrasena valido | Guarda el nuevo hash y audita PASSWORD_CHANGED | ✅ Aprobado |
| UT-PRF-09 |  | ProfileService | Cambio de contrasena de usuario inexistente | Responde 404 | ✅ Aprobado |
| UT-USR-01 |  | UserService | page/pageSize = (0,0), (-3,500), (2,50) | (1,20), (1,20), (2,50) | ✅ Aprobado (3/3) |
| UT-USR-02 |  | UserService | Consultar un usuario inexistente | Responde 404 | ✅ Aprobado |
| UT-USR-03 |  | UserService | Crear usuario sin rol | Asigna USER, guarda el hash y audita | ✅ Aprobado |
| UT-USR-04 |  | UserService | Crear usuario | Normaliza el rol a mayusculas | ✅ Aprobado |
| UT-USR-05 |  | UserService | Crear usuario con rol inexistente | Responde 422 | ✅ Aprobado |
| UT-USR-06 |  | UserService | Crear usuario con username o correo duplicado | Responde 409 | ✅ Aprobado |
| UT-USR-07 |  | UserService | Cambian nombre, correo, teléfono, identificación y fecha; el apellido queda igual | Un solo evento USER_UPDATED con 5 campos en Before (sin lastName) | ✅ Aprobado |
| UT-USR-08 |  | UserService | Actualizar con un correo de otro usuario | Responde 409 | ✅ Aprobado |
| UT-USR-09 |  | UserService | Role "intermedio" y Status "inactive" | Rol INTERMEDIO y estado INACTIVE; se auditan ROLE_CHANGED y STATUS_CHANGED, no USER_UPDATED | ✅ Aprobado |
| UT-USR-10 |  | UserService | Cambiar a un rol inexistente | Responde 422 | ✅ Aprobado |
| UT-USR-11 |  | UserService | Estado invalido | Se ignora sin romper la actualizacion | ✅ Aprobado |
| UT-USR-12 |  | UserService | Actualizar un usuario inexistente | Responde 404 | ✅ Aprobado |
| UT-USR-13 | Sí | UserService | Desbloqueo manual del admin | Limpia intentos y bloqueo | ✅ Aprobado |
| UT-USR-14 |  | UserService | UnlockAsync de un usuario sin bloqueo | No guarda ni audita | ✅ Aprobado |
| UT-USR-15 |  | UserService | Desbloquear un usuario inexistente | Responde 404 | ✅ Aprobado |

## Pruebas de integración — backend (API real + SQL Server + MongoDB)

| ID | Regresión | Componente | Entrada / escenario | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|
| IT-AUTH-01 |  | API /auth | admin/Admin#2026! → POST /login, código MFA capturado → /mfa/verify, GET /profile, POST /refresh, POST /logout, POST /refresh | 200 en cada paso; tras el logout, el refresh responde 401 | ✅ Aprobado |
| IT-AUTH-02 |  | API /auth | Contrasena incorrecta | Responde 401 con el formato de error uniforme | ✅ Aprobado |
| IT-AUTH-03 | Sí | API /auth | Usuario nuevo: 2 logins con clave incorrecta, un 3.º incorrecto y luego la clave correcta; el admin hace PATCH /users/{id}/unlock | 401, 401, 403 (bloqueada) y 403 con la clave correcta; después del desbloqueo, 200 con desafío MFA | ✅ Aprobado |
| IT-AUTH-04 |  | API /auth | Codigo MFA incorrecto | Responde 401 | ✅ Aprobado |
| IT-AUTH-05 | Sí | API /auth | Rate limiting: el 6.o login por minuto desde la misma IP | Responde 429 | ✅ Aprobado |
| IT-AUTH-06 |  | API /auth | Registro con datos invalidos | Responde 422 con la lista de errores | ✅ Aprobado |
| IT-AUTH-07 | Sí | API /auth | Registro con documento invalido | No crea el usuario | ✅ Aprobado |
| IT-AUTH-08 |  | API /auth | Registro con un PDF valido | Guarda el usuario y su documento | ✅ Aprobado |
| IT-AUTH-09 |  | API /auth | Registro con username existente | Responde 409 | ✅ Aprobado |
| IT-SEC-01 |  | API /auth | GET /api/profile sin token y con un JWT con la firma alterada | 401 con cuerpo ApiErrorResponse | ✅ Aprobado |
| IT-SEC-02 |  | API /auth | Usuario estandar → GET /api/users | 403 | ✅ Aprobado |
| IT-DOC-01 |  | API /documents | Usuario sube constancia.pdf, lista, descarga, elimina y vuelve a descargar | 200, 1 documento, mismos bytes y application/pdf, 204, lista vacía, 404 | ✅ Aprobado |
| IT-DOC-02 |  | API /documents | Usuario B → GET /users/{A}/documents; admin → la misma ruta | 403 para B; 200 para el admin | ✅ Aprobado |
| IT-DOC-03 | Sí | API /documents | Nombre de archivo con caracteres de control | Responde 422 | ✅ Aprobado |
| IT-DOC-04 |  | API /documents | Un PDF disfrazado de JPG | Se rechaza por su firma (422) | ✅ Aprobado |
| IT-OC-01 |  | API /operaciones | Estandar crea una orden en GT-CENTRO: 10×125.50 y 4×80 | 201, estado CREADA, número OC-yyyy-NNNNNN, monto 1575.00; el presupuesto comprometido sube 1575.00 | ✅ Aprobado |
| IT-OC-02 |  | API /operaciones | Orden sin líneas con monto 250.75 USD | 201 con una única línea "Monto global" y monto 250.75 | ✅ Aprobado |
| IT-OC-03 | Sí | API /operaciones | Orden con una línea válida y otra con cantidad 0 | 422; no queda la orden ni ninguna línea (listado por NIT = 0) | ✅ Aprobado |
| IT-OC-04 |  | API /operaciones | Bodega sin presupuesto / moneda EUR / fecha de emisión +30 días | 422 en cada caso | ✅ Aprobado (3/3) |
| IT-OC-05 |  | API /operaciones | Orden sin líneas ni monto; NIT "NIT<script>" | 422 en ambos casos | ✅ Aprobado |
| IT-OC-06 |  | API /operaciones | Estandar → PUT para anular una línea; Intermedio → DELETE de la orden | 403 en ambos casos | ✅ Aprobado |
| IT-OC-07 |  | API /operaciones | Intermedio anula la línea 2 (motivo " x " y luego "Proveedor sin existencias"), repite, y anula la línea 99 | 422 por el motivo corto; 200 con monto 1255.00 y presupuesto −320.00; 409 al repetir; 404 para la línea 99 | ✅ Aprobado |
| IT-OC-08 | Sí | API /operaciones | Admin hace DELETE ?motivo=Duplicada por error; luego consulta el estandar; el admin repite el DELETE | 200 ANULADA con motivo; 404 y listado vacío para el estandar; el admin la ve ANULADA; 409 al repetir | ✅ Aprobado |
| IT-OC-09 |  | API /operaciones | DELETE con rowVer = AAAAAAAAAAA= (no coincide) | 409 | ✅ Aprobado |
| IT-OC-10 |  | API /operaciones | GET /ordenes con desde, hasta, NIT, soloMias, conDetalle, numeroOrden, page=0 y pageSize=500; GET por id; GET por un id inexistente; GET /particiones | Total 1 y pageSize normalizado a 20; detalle con presupuesto; 404; particiones no vacías | ✅ Aprobado |
| IT-REP-01 |  | API /reportes y /health | Orden de 99,000 en GT-NORTE → GET ranking-proveedores?anio=actual&top=100 | El NIT aparece con total 99,000 y posición ≥ 1 | ✅ Aprobado |
| IT-REP-02 |  | API /reportes y /health | Ranking con anio o top fuera de rango | Responde 422 | ✅ Aprobado (3/3) |
| IT-REP-03 |  | API /reportes y /health | Sin fechas / desde > hasta / rango de 2 años | 400 en cada caso | ✅ Aprobado (3/3) |
| IT-REP-04 |  | API /reportes y /health | Admin → reporte del día con estado, bodega y pageSize=10 | 200, montoTotal ≥ 0 y como máximo 10 filas | ✅ Aprobado |
| IT-REP-05 |  | API /reportes y /health | Admin → auditoria/soft-delete?tipo=ordenes\|lineas\|documentos | 200 con resumen | ✅ Aprobado (3/3) |
| IT-REP-06 |  | API /reportes y /health | Estandar → auditoria; admin → tipo=otro | 403; 422 | ✅ Aprobado |
| IT-HLT-01 |  | API /reportes y /health | /api/health/live | Responde 200 sin autenticacion | ✅ Aprobado |
| IT-HLT-02 |  | API /reportes y /health | /api/health | Reporta SQL Server y MongoDB como Healthy | ✅ Aprobado |
| IT-HLT-03 |  | API /reportes y /health | Swagger | Publica el contrato OpenAPI en Development | ✅ Aprobado |
| IT-USR-01 |  | API /users y /profile | Admin crea usuario INTERMEDIO, lo consulta, lo edita y lo busca | 201 con Location, 200, nombre actualizado, total 1 | ✅ Aprobado |
| IT-USR-02 |  | API /users y /profile | Desactivar un usuario | Le impide iniciar sesion (403) | ✅ Aprobado |
| IT-USR-03 |  | API /users y /profile | Cambiar el rol de un usuario | Se refleja en su DTO | ✅ Aprobado |
| IT-USR-04 |  | API /users y /profile | GET /users/999999; POST /users sin correo y con clave de 3 caracteres | 404; 422 | ✅ Aprobado |
| IT-PRF-01 |  | API /users y /profile | Usuario edita su nombre, cambia la clave con la actual incorrecta y luego con la correcta, e inicia sesión con la nueva | 200; 401; 200; login 200 | ✅ Aprobado |

## Pruebas unitarias y de componentes — frontend

| ID | Regresión | Componente | Entrada / escenario | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|---|
| FE-API-05 |  | Capa API (frontend) | authApi | Llama a los endpoints de autenticacion | ✅ Aprobado |
| FE-API-06 | Sí | Capa API (frontend) | getUsers({role:"ALL", status:"ALL"}) | No se envían role ni status como "ALL" | ✅ Aprobado |
| FE-API-07 |  | Capa API (frontend) | usersApi | Cubre consultar, crear, editar, estado, rol y desbloqueo | ✅ Aprobado |
| FE-API-08 |  | Capa API (frontend) | profileApi consulta, actualiza y cambia la contrasena | Se cumple lo descrito | ✅ Aprobado |
| FE-API-09 |  | Capa API (frontend) | documentsApi | Rechaza extensiones no permitidas antes de llamar a la API | ✅ Aprobado |
| FE-API-10 |  | Capa API (frontend) | documentsApi | Rechaza un archivo cuyo contenido no es PDF/JPEG | ✅ Aprobado |
| FE-API-11 |  | Capa API (frontend) | documentsApi sube un PDF valido y gestiona descarga y borrado | Se cumple lo descrito | ✅ Aprobado |
| FE-API-12 |  | Capa API (frontend) | reportesApi | Elimina filtros vacios antes de consultar | ✅ Aprobado |
| FE-API-13 |  | Capa API (frontend) | operacionesApi | Envia la fecha de emision para la eliminacion de particiones | ✅ Aprobado |
| FE-API-14 |  | Capa API (frontend) | mensajeError | Prioriza el primer error de validacion, luego el mensaje | ✅ Aprobado |
| FE-API-01 |  | axios.ts | localStorage industry_session con accessToken jwt-123 | La cabecera Authorization es "Bearer jwt-123" | ✅ Aprobado |
| FE-API-02 |  | axios.ts | Sin sesión / sesión con JSON corrupto | Sin cabecera Authorization y sin excepción | ✅ Aprobado |
| FE-API-03 |  | axios.ts | un 401 | Borra la sesion, avisa a la app y rechaza la promesa | ✅ Aprobado |
| FE-API-04 |  | axios.ts | Respuesta 500 | Rechaza la promesa y conserva la sesión; las respuestas 2xx pasan sin cambios | ✅ Aprobado |
| FE-VAL-01 |  | fileValidator.ts | contrato.pdf, FOTO.JPG, imagen.jpeg, script.exe, sin-extension, doble.pdf.exe | true, true, true, false, false, false | ✅ Aprobado (6/6) |
| FE-VAL-02 |  | fileValidator.ts | Bytes con firma %PDF y FF D8 FF | { valid: true } | ✅ Aprobado |
| FE-VAL-03 |  | fileValidator.ts | PNG renombrado a .pdf; texto plano | Inválido con "Solo se permiten archivos PDF o JPEG"; inválido con "No se pudo detectar el tipo de archivo" | ✅ Aprobado |
| FE-VAL-04 |  | fileValidator.ts | si la lectura del archivo falla | Devuelve un error controlado | ✅ Aprobado |
| FE-AUTH-01 |  | AuthContext / rutas | Sesión guardada con rol ADMIN que vence en 1 h | isAuthenticated = true y rol ADMIN | ✅ Aprobado |
| FE-AUTH-02 |  | AuthContext / rutas | Sesión vencida; sesión con JSON corrupto | No autenticado y la sesión se borra de localStorage | ✅ Aprobado |
| FE-AUTH-03 |  | AuthContext / rutas | authApi.login devuelve requiresMfa con mfa-1 | { requiresMfa: true, mfaToken: "mfa-1" } y sin sesión abierta | ✅ Aprobado |
| FE-AUTH-04 |  | AuthContext / rutas | login fallido | Devuelve el mensaje del backend o uno por defecto | ✅ Aprobado |
| FE-AUTH-05 |  | AuthContext / rutas | authApi.login devuelve el access token directamente | Sesión persistida en localStorage | ✅ Aprobado |
| FE-AUTH-06 |  | AuthContext / rutas | verifyMfa: fallido con mensaje, fallido sin mensaje y luego exitoso | Error con el mensaje del backend, error por defecto y sesión abierta | ✅ Aprobado |
| FE-AUTH-07 |  | AuthContext / rutas | logout() con la API caída; evento auth:unauthorized | La sesión se cierra en ambos casos | ✅ Aprobado |
| FE-AUTH-08 |  | AuthContext / rutas | Sesión que vence en 5 s y el reloj avanza 6 s | isAuthenticated = false | ✅ Aprobado |
| FE-AUTH-09 |  | AuthContext / rutas | useAuth fuera del proveedor | Lanza un error claro | ✅ Aprobado |
| FE-AUTH-10 |  | AuthContext / rutas | sin sesion | Redirige a /login | ✅ Aprobado |
| FE-AUTH-11 |  | AuthContext / rutas | con un rol no permitido | Redirige a /403 | ✅ Aprobado |
| FE-AUTH-12 |  | AuthContext / rutas | con el rol permitido | Muestra la pagina | ✅ Aprobado |
| FE-UI-01 |  | Componentes comunes | RoleBadge ADMIN/USER, StatusBadge ACTIVE/INACTIVE, LockedBadge | Textos ADMIN, USER, Activo, Inactivo y Bloqueado | ✅ Aprobado |
| FE-UI-02 |  | Componentes comunes | LoadingState, EmptyState y ErrorState con onRetry | Textos visibles; Reintentar invoca el callback | ✅ Aprobado |
| FE-UI-03 |  | Componentes comunes | ErrorState sin onRetry no | Muestra el boton | ✅ Aprobado |
| FE-UI-04 |  | Componentes comunes | ConfirmDialog | Ejecuta confirmar y cancelar | ✅ Aprobado |
| FE-UI-05 |  | Componentes comunes | ToastProvider | Muestra el mensaje y se puede cerrar | ✅ Aprobado |
| FE-UI-06 |  | Componentes comunes | useToast fuera del proveedor | Lanza error | ✅ Aprobado |
| FE-UI-07 |  | Componentes comunes | ErrorBoundary | Captura el error y permite reintentar | ✅ Aprobado |
| FE-REG-01 | Sí | Utilidades de reportes | formatoFechaHora | Trata como UTC las fechas sin zona y respeta Z/offset | ✅ Aprobado |
| FE-REG-02 |  | Utilidades de reportes | formatoFecha("2026-01-31T00:00:00"), null; fechaLocal(5 ene 2026) | "31/01/2026", "-", "2026-01-05" | ✅ Aprobado |
| FE-REG-03 |  | Utilidades de reportes | {response:{status:403}}; Error("red") | 403; undefined | ✅ Aprobado |
