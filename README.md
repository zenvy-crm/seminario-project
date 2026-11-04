# seminario-backend-service

API REST en ASP.NET Core 8 (autenticacion JWT + MFA, roles ADMIN/INTERMEDIO/USER, documentos, auditoria asincrona a MongoDB).

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

Al iniciar, la aplicacion aplica las migraciones de EF Core automaticamente (con un timeout de 10 minutos, porque algunas recorren `TB_Ordenes_Compra` completa) y crea los roles `ADMIN`/`INTERMEDIO`/`USER` que falten. Si no existe ningun usuario con rol `ADMIN`, crea el administrador inicial:

- Usuario: `admin`
- Password: `Admin#2026!`

Los usuarios de demostracion de la Fase 2 (`estandar` / `Estandar#2026!` con rol `USER` e `intermedio` / `Intermedio#2026!` con rol `INTERMEDIO`) solo se crean en `Development` o si se define `Seed__DemoUsers=true` en el `.env` / `api.env`. En produccion no se siembran cuentas con contrasenas publicadas.

Swagger queda disponible en `/swagger` en modo desarrollo, con soporte para autenticacion Bearer.

## Health check

Patron Health Check de ASP.NET Core (`AddHealthChecks` + `IHealthCheck`), sin autenticacion:

| Endpoint | Que revisa | Respuesta |
|---|---|---|
| `GET /api/health` | Readiness: sonda real a SQL Server (`CanConnectAsync`) y a MongoDB (`ping`), cada una con timeout de 3 s | `200 Healthy`; `200 Degraded` si MongoDB esta caido (la bitacora es asincrona y no debe impedir operar); `503 Unhealthy` si SQL Server esta caido |
| `GET /api/health/live` | Liveness: el proceso atiende peticiones | Siempre `200` mientras la API este viva |

`/api/health` devuelve el estado agregado, el estado y la duracion de cada sonda, version, entorno, `uptimeSeconds` y memoria del proceso. Es el endpoint que vigila Uptime Kuma. El `HEALTHCHECK` del contenedor usa `/api/health/live`, para que una base caida no marque el contenedor como roto. Las sondas estan en `Infrastructure/Health/`.

## Despliegue como contenedor en la VM

El API corre como un contenedor mas (`seminario-api`) en la misma VM que el stack `db-infra`, unido a su red `db-infra_db-infra-net`. Por eso llega a las bases de datos por nombre de contenedor (`mssql-relational`, `mongo-nosql`) sin pasar por los puertos publicados del host.

| Archivo | Rol |
|---|---|
| `seminario-backend-service/Dockerfile` | Build multi-stage (.NET 8 SDK -> runtime ASP.NET), usuario sin privilegios, puerto 8080, `HEALTHCHECK` contra `/api/health` (con bash, sin instalar `curl`) |
| `docker-compose.yml` | Servicio `api`: usa la imagen ya cargada, publica `5000 -> 8080`, volumen `seminario-api_uploads` para los documentos, red externa de `db-infra` |
| `api.env.example` | Plantilla de variables del contenedor. El `api.env` real no se versiona |
| `deploy.ps1` | Construye la imagen en local, la exporta (`docker save`), la copia por scp y en la VM hace `docker load` + `docker compose up -d` |

```
.\deploy.ps1 -User <usuario-ssh-de-la-vm>
```

La imagen se construye en la maquina de desarrollo (requiere Docker Desktop; el script lo inicia si esta apagado) y viaja ya hecha a la VM. No se construye en la VM porque alli los contenedores de build no resuelven DNS (`apt` y `dotnet restore` fallan) y la descarga de la imagen del SDK es muy lenta; asi la VM solo ejecuta y no necesita salida a internet. El paquete pesa ~95 MB.

Si `api.env` no existe, el script lo genera a partir del `.env` local cambiando los hosts de las bases de datos por los nombres de contenedor. Al terminar espera a que `http://192.168.1.135:5000/api/health` responda 200. Con `-PackageOnly` solo genera el paquete en `%TEMP%\seminario-api.tgz`, sin tocar la VM.

Notas:

- El contenedor arranca en `Production`: Swagger queda deshabilitado y la cookie del refresh token se emite con `Secure`, por lo que un navegador no la guarda sobre HTTP plano. Para una demo por HTTP en la LAN, cambiar `ASPNETCORE_ENVIRONMENT=Development` en `api.env` y volver a desplegar.
- `Cors__AllowedOrigin` en `api.env` debe ser el origen desde el que el navegador carga el frontend.
- Como `db-infra` es otro proyecto compose no hay `depends_on`: si la VM reinicia y SQL Server aun no esta listo, el API falla al migrar y `restart: unless-stopped` lo reintenta hasta que conecta.

## Contrasenas

El hashing usa **BCrypt con work factor 12** (`BCrypt.Net-Next`), a traves de la abstraccion `IPasswordHasher`. La libreria genera un salt de 16 bytes y lo embebe en el hash resultante (`$2a$12$...`, 60 caracteres), por lo que dos usuarios con la misma contrasena producen hashes distintos.

Los hashes creados por la version anterior (PBKDF2 de ASP.NET Core Identity) se siguen verificando y se marcan como `NeedsRehash`: en el siguiente login exitoso se regeneran con BCrypt automaticamente y se registra un evento `PASSWORD_REHASHED` en la bitacora. No se requiere que el usuario recree su contrasena ni migrar la tabla a mano.

## Rate limiting

`/api/auth/login` y `/api/auth/mfa/verify` estan limitados a **5 peticiones por minuto por direccion IP** (ventana fija, `Microsoft.AspNetCore.RateLimiting`). Al superarlo la API responde `429 Too Many Requests` con cabecera `Retry-After: 60` y el formato de error estandar. Es una defensa perimetral complementaria al bloqueo de cuenta (3 intentos fallidos, 5 minutos de bloqueo) que vive en `AuthService`.

## MFA

El segundo factor es un codigo temporal (OTP) de 6 digitos, valido por 5 minutos. En este entorno de desarrollo, al no existir integracion con SMS/email, el codigo se imprime en la consola del backend (nivel `Information`) al momento de iniciar sesion.

## Auditoria

Los eventos de auditoria (login, cambios de usuario, documentos, etc.) se encolan en memoria y se escriben de forma asincrona a la coleccion `audit_logs` de MongoDB mediante un `BackgroundService`. Una falla temporal de MongoDB no afecta la respuesta de la operacion principal. Los documentos usan los campos `fecha_evento`, `modulo`, `accion`, `entidad_afectada_id`, `jwt_usuario_ejecutor`, `rol_auth`, `justificacion_cambio`, `datos_anteriores` y `datos_nuevos` (mas `actor_id`, `usuario_afectado_id`, `usuario_afectado` e `ip_origen` cuando aplican). La anulacion de ordenes (`ANULACION_MASTER`) usa un camino propio con `Task.Run`: ver la seccion de Fase 2.

## Modulo de Operaciones: ordenes de compra particionadas

`TB_Ordenes_Compra` es la tabla de alta transaccionalidad del portal. Vive sobre un esquema de particion por anio en la columna `FechaEmision`:

- `FN_RangoFechasOrdenes` — funcion de particion `RANGE RIGHT` con limites 2024, 2025, 2026 y 2027 (5 particiones).
- `PS_RangoFechasOrdenes` — esquema que mapea las particiones a filegroups (todas a `PRIMARY` en el laboratorio).
- La llave primaria es clusterizada sobre `(FechaEmision, Id)` porque SQL Server exige que la columna de particion forme parte de la llave. Los dos indices no clusterizados se crean sobre el mismo esquema para quedar alineados.

La migracion `AddOrdenesCompraParticionada` emite este DDL como SQL nativo, ya que EF Core no sabe expresar `PARTITION FUNCTION` ni `PARTITION SCHEME`. El equivalente para ejecutar a mano esta en `sql/01_particionamiento_ordenes_compra.sql`, que ademas incluye la consulta de auditoria de particiones y una prueba de *partition elimination*.

### Endpoints

| Metodo | Ruta | Rol | Descripcion |
|---|---|---|---|
| POST | `/api/operaciones/ordenes` | Estandar | Registra una orden llamando a `SP_OrdenCompra_Crear` (ver Fase 2). Siempre nace en estado `CREADA`: `estado` ya no se envia (si viene se ignora) y `monto` es opcional (lo calcula el trigger a partir de las `lineas`). El `UserId` sale del claim `sub` del JWT, nunca del cuerpo. |
| GET | `/api/operaciones/ordenes` | Estandar | Listado paginado con filtros por rango de fechas, estado, proveedor y `soloMias`. El rango de fechas es lo que habilita la eliminacion de particiones. |
| GET | `/api/operaciones/ordenes/{id}` | Estandar | Detalle de la orden con sus lineas (ver Fase 2). |
| GET | `/api/operaciones/presupuestos` | Estandar | Presupuesto por bodega y anio (ver Fase 2). |
| PUT | `/api/operaciones/ordenes/{id}/lineas/{linea}` | Intermedio | Soft delete de una linea (ver Fase 2). |
| DELETE | `/api/operaciones/ordenes/{id}` | Administrador | Soft delete de la orden, `Estado = ANULADA` (ver Fase 2). |
| GET | `/api/operaciones/particiones` | autenticado | Filas reales por particion fisica (`sys.partitions`). |
| POST | `/api/seed-data` | ADMIN | Stress test: inyecta N ordenes con fechas historicas. |

### Stress test

`POST /api/seed-data` con `{ "cantidad": 50000, "anioInicial": 2024, "anioFinal": 2026 }` usa **`SqlBulkCopy`** en lotes de 5,000 filas. Entity Framework emitiria un INSERT por fila (50,000 round-trips); `SqlBulkCopy` usa el protocolo de carga masiva de TDS. La respuesta devuelve duracion, filas por segundo y el conteo por particion resultante.

El frontend expone todo esto en `/operaciones` (Dashboard de Operaciones), que es la pagina a la que se redirige tras un login exitoso.

## Fase 2: SP transaccional, triggers, RBAC y Soft Delete

Modulo Orden de Compra de un portal de proveedores. La logica transaccional vive en SQL Server (migracion `20260926031522_Fase2OrdenCompraDetalle`). El equivalente para ejecutar a mano, idempotente y con un bloque de pruebas comentado, esta en `sql/02_fase2_sp_triggers_soft_delete.sql`. La verificacion de punta a punta (peticiones HTTP y consultas SQL reales) esta en `docs/Fase2-Evidencia.md`.

### Modelo de datos

| Tabla | Rol |
|---|---|
| `TB_Ordenes_Compra` | Encabezado (la misma tabla particionada de Operaciones). Columnas nuevas: `UsuarioAnulaId`, `FechaAnulacion`, `MotivoAnulacion` y `RowVer` (`rowversion`). `Monto` ahora es el total que mantiene el trigger: suma de `Importe` de las lineas vigentes. |
| `TB_Ordenes_Compra_Detalle` | Lineas de la orden, particionadas con el mismo esquema (`ON PS_RangoFechasOrdenes([FechaEmision])`). PK `(FechaEmision, OrdenId, Linea)` y FK `(FechaEmision, OrdenId)` al encabezado. `Importe` es una columna calculada `PERSISTED` (`ROUND(Cantidad * PrecioUnitario, 2)`). `EstadoLinea = 0` es el soft delete de la linea (con `UsuarioBajaId`, `FechaBaja` y `MotivoBaja`). CHECKs: `Cantidad > 0` y `PrecioUnitario >= 0`. |
| `TB_Presupuesto_Bodega` | La **"capacidad"**: presupuesto de compras por `(Anio, Bodega)`. `MontoDisponible = MontoAsignado - MontoComprometido` (calculada `PERSISTED`), con CHECK `MontoComprometido >= 0`. Se siembran 8 bodegas para 2024..2027 con 2,500,000.00 cada una: `GT-CENTRO`, `GT-NORTE` y `GT-SUR` (migracion `Fase2OrdenCompraDetalle`) y las 5 que usan el formulario del frontend y el stress test (`Bodega Central Zona 12`, `Centro de Distribucion Villa Nueva`, `Bodega Norte Chimaltenango`, `Bodega Regional Quetzaltenango` y `Cross-dock Puerto Barrios`, migracion `SeedPresupuestoBodegasFrontend`). |

Estados de la orden: `CREADA` (estado inicial), `EMITIDA`, `RECIBIDA`, `PAGADA` y `ANULADA`. El `NumeroOrden` sale de la secuencia `SEQ_OrdenCompra_Numero` con formato `OC-yyyy-NNNNNN`.

### Roles (RBAC por claims del JWT)

Los nombres tecnicos `USER` y `ADMIN` no se renombraron porque el frontend los usa. El mapeo oficial es:

| Rol en BD / JWT | Rol de la Fase 2 | Claim `nivel` | Usuario de demo | Password |
|---|---|---|---|---|
| `USER` | Estandar | 1 | `estandar` | `Estandar#2026!` |
| `INTERMEDIO` | Intermedio | 2 | `intermedio` | `Intermedio#2026!` |
| `ADMIN` | Administrador | 3 | `admin` | `Admin#2026!` |

`admin` se crea si no existe ningun usuario `ADMIN`. `estandar` e `intermedio` solo se siembran en `Development` o con `Seed__DemoUsers=true`.

El JWT lleva el rol en `ClaimTypes.Role` y el nivel en el claim `nivel`. Las politicas (`Api/AuthorizationPolicies.cs`) son acumulativas:

| Politica | Roles admitidos |
|---|---|
| `NivelEstandar` | `USER`, `INTERMEDIO`, `ADMIN` |
| `NivelIntermedio` | `INTERMEDIO`, `ADMIN` |
| `NivelAdministrador` | `ADMIN` |

Un rol que no cumple la politica recibe `403` JSON; sin token, `401`.

### Endpoints

Todos bajo `/api/operaciones`. Los que reciben `{id}` aceptan `?fechaEmision=yyyy-MM-dd` para que SQL Server elimine particiones; sin ella se busca el `Id` en toda la tabla.

| Metodo | Ruta | Politica | Descripcion |
|---|---|---|---|
| POST | `/ordenes` | `NivelEstandar` | Crea la orden llamando a `SP_OrdenCompra_Crear`. Nace `CREADA`. Responde `201` con encabezado, `lineas` y `presupuesto` de la bodega. |
| GET | `/ordenes` | `NivelEstandar` | Listado paginado (`desde`, `hasta`, `estado`, `proveedorNit`, `soloMias`, `conDetalle`, `numeroOrden`, `page`, `pageSize`). `conDetalle=true` devuelve solo las ordenes maestro-detalle (excluye las filas del stress test); `numeroOrden` busca por numero exacto (sin distinguir mayusculas). El rol Estandar **nunca** ve ordenes `ANULADA`, aunque pida `estado=ANULADA`. |
| GET | `/ordenes/{id}` | `NivelEstandar` | Encabezado + todas las lineas (las anuladas con `estadoLinea = false`) + presupuesto. Para Estandar una orden `ANULADA` responde `404`. |
| GET | `/presupuestos?anio=` | `NivelEstandar` | Asignado, comprometido y disponible por bodega y anio. |
| PUT | `/ordenes/{id}/lineas/{linea}` | `NivelIntermedio` | Soft delete de la linea: `EstadoLinea = 0`, `UsuarioBajaId` (del JWT), `FechaBaja` y `MotivoBaja`. El trigger devuelve la capacidad. Cuerpo `{ "motivo": "...", "estadoLinea": 0, "rowVer": "..." }` (`estadoLinea` y `rowVer` son opcionales; el motivo tambien se acepta como `?motivo=`). |
| DELETE | `/ordenes/{id}` | `NivelAdministrador` | **Soft delete** de la orden: `Estado = ANULADA`, `UsuarioAnulaId`, `FechaAnulacion` y `MotivoAnulacion`. La fila sigue en la base. Cuerpo `{ "motivo": "...", "rowVer": "..." }` o, sin cuerpo, `?motivo=...`. Dispara la auditoria `ANULACION_MASTER`. |

Reglas comunes de PUT y DELETE:

- `motivo` es obligatorio, de 3 a 200 caracteres despues de recortar espacios (`422` si no).
- Solo se aplican sobre ordenes `CREADA` o `EMITIDA`. Una linea ya anulada o una orden ya anulada responde `409`.
- `rowVer` (base64) es bloqueo optimista: en PUT es el de la **linea**, en DELETE el de la **orden**. Si no coincide, `409`. Anular una linea cambia el `rowVer` de la orden (el trigger actualiza `Monto`), asi que antes del DELETE hay que usar el `rowVer` que devolvio el PUT.

Traduccion de errores del SP y los triggers: `50010`, `50011`, `547` (CHECK de cantidad o precio) y `8115` responden `422`; `50012`, `50013`, `50020`, `50021`, `50022`, `1205` y el CHECK del presupuesto responden `409`.

### Stored Procedure `dbo.SP_OrdenCompra_Crear`

`EXEC dbo.SP_OrdenCompra_Crear @Json = N'...', @UserId = 3;` recibe la orden completa en JSON (llaves camelCase, sensibles a mayusculas):

```json
{
  "fechaEmision": "2026-09-25",
  "proveedorNit": "77370471-1",
  "proveedorNombre": "Ferreteria El Constructor S.A.",
  "moneda": "GTQ",
  "bodegaDestino": "GT-CENTRO",
  "detalle": "Pedido de obra gris",
  "lineas": [
    { "descripcion": "Cemento gris 42.5 kg", "cantidad": 120, "precioUnitario": 86.5 },
    { "descripcion": "Varilla corrugada 3/8", "cantidad": 250, "precioUnitario": 41.25 }
  ]
}
```

Flujo: `SET XACT_ABORT ON` -> `BEGIN TRY` -> `BEGIN TRAN` -> valida que exista presupuesto para `(anio, bodega)` (`THROW 50010`) -> genera `Id` y `NumeroOrden` -> inserta el encabezado con `Estado = 'CREADA'` y `Monto = 0` -> inserta **todas** las lineas numeradas 1..N (el trigger compromete presupuesto y calcula `Monto`) -> `COMMIT` -> `SELECT Id, FechaEmision, NumeroOrden, Monto, Estado, RowVer`. En el `BEGIN CATCH`: `IF XACT_STATE() <> 0 ROLLBACK; THROW;`.

**Rollback absoluto:** si una sola linea falla (sin lineas, cantidad <= 0, precio < 0, decimales de mas o importe fuera de rango -> `50011`; presupuesto insuficiente -> `50012`; violacion de CHECK -> `547`) no queda ni el encabezado ni ninguna linea, y el comprometido no cambia. La API no valida cantidad ni precio en el DTO a proposito, para que el rollback se pueda demostrar por HTTP.

| Codigo | Significado |
|---|---|
| 50010 | No existe presupuesto para ese anio y bodega |
| 50011 | JSON invalido, faltan datos, no hay lineas o una linea es invalida (`Linea N invalida: ...`) |
| 50012 | Presupuesto insuficiente |
| 50013 | Se intentaron modificar lineas de una orden `ANULADA` |
| 50020 | Borrado fisico prohibido |
| 50021 | Una orden `ANULADA` no puede volver a otro estado |
| 50022 | No se puede cambiar `BodegaDestino` de una orden con lineas vigentes |

### Triggers: el presupuesto como "capacidad"

| Trigger | Tabla | Que hace |
|---|---|---|
| `TR_OC_Detalle_Presupuesto` (`AFTER INSERT, UPDATE`) | Detalle | Trabaja por conjuntos (`inserted`/`deleted`, multi-fila). Recalcula `Monto` de cada orden afectada como `SUM(Importe)` de sus lineas con `EstadoLinea = 1` y aplica el **delta** a `MontoComprometido` de `(YEAR(FechaEmision), BodegaDestino)`. Al insertar lineas la capacidad se **consume**; al poner `EstadoLinea = 0` se **devuelve**. Si el disponible quedaria negativo lanza `50012`. |
| `TR_OC_Encabezado_Anulacion` (`AFTER UPDATE`) | Encabezado | Cuando `Estado` pasa a `ANULADA` libera del comprometido la suma de las lineas vigentes de la orden. Revertir una `ANULADA` lanza `50021`. |
| `TR_OC_Encabezado_NoDelete` y `TR_OC_Detalle_NoDelete` (`INSTEAD OF DELETE`) | Ambas | Todo `DELETE` fisico responde `THROW 50020 'Borrado fisico prohibido: use Soft Delete'`. |

### Sin DELETE fisico

Ningun rol ejecuta un `DELETE` en SQL: el verbo HTTP `DELETE` del Administrador es un `UPDATE` de `Estado`, y la anulacion de lineas es un `UPDATE` de `EstadoLinea`. Aunque alguien lance un `DELETE` directo contra la base, los triggers `INSTEAD OF DELETE` lo rechazan. La orden anulada desaparece de la vista del rol Estandar, pero sigue en la base para Intermedio, Administrador y la auditoria.

La regla aplica tambien a los documentos de la Fase 1: `DELETE /api/users/{userId}/documents/{documentId}` es un Soft Delete (ver [Documentos](#documentos-validaciones-y-limitacion-conocida)) y `UserDocuments` tiene su propio `INSTEAD OF DELETE` (`TR_UserDocuments_NoDelete`, mismo error `50020`).

### Auditoria NoSQL asincrona

Los documentos de `audit_logs` usan los nombres de campo exigidos (`[BsonElement]` en `AuditLogDocument`). Hay dos caminos de escritura:

| Evento | Camino | Detalle |
|---|---|---|
| `ORDEN_COMPRA_CREADA`, `LINEA_ANULADA` y los de Fase 1 (login, usuarios, documentos) | Canal en memoria (`IAuditLogger.Enqueue`) | `AuditBackgroundService` drena el `Channel` y escribe en MongoDB. |
| `ANULACION_MASTER` | `IAuditLogger.FireAndForget` -> `Task.Run` | Justo despues del `UPDATE` en SQL Server se lanza un hilo en segundo plano y la respuesta HTTP sale sin esperarlo. Hace 1 intento + 3 reintentos (esperas de 200 ms, 1 s y 3 s), registra cada fallo con `LogWarning` y el definitivo con `LogError`. |

`rol_auth` se deriva del rol del JWT (`USER` -> `Estandar`, `INTERMEDIO` -> `Intermedio`, `ADMIN` -> `Administrador`) y `jwt_usuario_ejecutor` es el `username` del token. Documento `ANULACION_MASTER` leido de la coleccion `audit_logs` de MongoDB en la VM (orden `OC-2026-000001`, anulada por `admin`):

```json
{
  "_id": { "$oid": "6ab751a50523b5856c40ade5" },
  "fecha_evento": { "$date": "2026-09-26T05:01:25.572Z" },
  "modulo": "ORDEN_COMPRA",
  "accion": "ANULACION_MASTER",
  "entidad_afectada_id": "OC-2026-000001",
  "jwt_usuario_ejecutor": "admin",
  "rol_auth": "Administrador",
  "justificacion_cambio": "El proveedor no puede surtir",
  "datos_anteriores": { "estado": "CREADA" },
  "datos_nuevos": { "estado": "ANULADA" },
  "actor_id": 1,
  "usuario_afectado_id": 7,
  "ip_origen": "::1"
}
```

Los campos `actor_id`, `usuario_afectado_id`, `usuario_afectado` e `ip_origen` vienen de la auditoria de la Fase 1 y se omiten si son null.

Para consultarlo en la VM: `db.audit_logs.find({accion:"ANULACION_MASTER"}).sort({fecha_evento:-1}).limit(1)`. La verificacion completa (11 escenarios con los tres roles, el documento de `LINEA_ANULADA` y el tiempo de respuesta del `DELETE`) esta en `docs/Fase2-Evidencia.md`.

### Como reproducir la demo

Coleccion Postman: carpeta **Fase 2 - Orden de Compra** en `postman/seminario-backend-service.postman_collection.json`. El OTP de cada login se lee en la consola del backend. El login esta limitado a 5 peticiones por minuto por IP, asi que conviene espaciar los cambios de usuario.

1. Login como `estandar` -> `mfa/verify`. `GET /presupuestos?anio=2026` y anotar el comprometido de `GT-CENTRO`.
2. `POST /ordenes` con 3 lineas -> `201`, `CREADA`, `monto` = suma de las lineas; el comprometido sube ese monto.
3. `POST /ordenes` con una linea de `cantidad: 0` -> `422`. `GET /ordenes?proveedorNit=...` devuelve `total 0` y el comprometido no cambio (rollback absoluto).
4. Con `estandar`, `PUT .../lineas/1` y `DELETE /ordenes/{id}` -> `403`.
5. Login como `intermedio`. `PUT /ordenes/{id}/lineas/1` con `{ "motivo": "..." }` -> `200`: la linea queda con `estadoLinea = false`, `monto` baja y `presupuesto` muestra la capacidad devuelta. Repetirlo -> `409`. `DELETE` -> `403`.
6. Login como `admin`. `DELETE /ordenes/{id}` con `{ "motivo": "El proveedor no puede surtir" }` -> `200` `ANULADA`; el comprometido vuelve al valor inicial. Repetirlo -> `409`. En MongoDB aparece el documento `ANULACION_MASTER`.
7. Con `estandar`, `GET /ordenes/{id}` -> `404` y el listado no la muestra; con `admin` sigue visible. En SQL la fila sigue ahi con `Estado = 'ANULADA'`.
8. En SQL Server, `DELETE FROM dbo.TB_Ordenes_Compra WHERE Id = '...'` -> error `50020`. El bloque de pruebas de `sql/02_fase2_sp_triggers_soft_delete.sql` repite estos casos directamente sobre el SP y los triggers.

Las filas que inserta el stress test son historicas (`EMITIDA`, `RECIBIDA` o `PAGADA`): no pasan por el SP ni por los triggers, por lo que no tienen lineas ni comprometen presupuesto. Si el Administrador anula una de ellas, el trigger de anulacion libera la suma de sus lineas vigentes, que es 0: el presupuesto no se descuadra.

### Frontend

El frontend (repo aparte) tiene la pantalla **Orden de Compra** (`/ordenes-compra`, en el menu para los tres roles), hecha sobre el prototipo de la Tarea 8.1:

- **Cabecera de contexto** con los datos del JWT: usuario, rol, nivel y ejercicio.
- **Paleta transaccional:** navegacion `Primero / Anterior / Siguiente / Ultimo` sobre las ordenes maestro-detalle (`GET /ordenes?conDetalle=true`), buscador por numero de orden o Id, `NUEVO`, `GUARDAR` y `ANULAR ORDEN`. Al guardar, la pantalla muestra la orden recien creada con su numero.
- **Alta maestro-detalle:** encabezado y grid de lineas editables; el total y el saldo que quedaria en la bodega se calculan en vivo. `GUARDAR` envia todo al SP en un solo `POST`. Si una linea es invalida (por ejemplo cantidad 0), el 422 se muestra como *"No se guardo la orden ni ninguna linea (ROLLBACK)"* y el saldo no cambia.
- **Detalle:** las lineas anuladas quedan tachadas, con el usuario, la fecha y el motivo de la baja. El boton *Anular* de cada linea esta habilitado desde el rol Intermedio; *ANULAR ORDEN*, solo para el Administrador. Para un rol insuficiente los botones se ven deshabilitados con candado (la API igual responde `403`).
- **Saldos:** panel con el asignado, comprometido y disponible de la bodega de la orden, y tabla de todas las bodegas por anio (`GET /presupuestos`). Se refrescan despues de cada operacion para que se vea el efecto del trigger.

La pantalla `/operaciones` (dashboard de la tabla particionada) sigue creando ordenes solo con monto: el backend ignora el `estado` y crea una linea "Monto global". El formulario ya no envia `estado` y el tipo `OrdenEstado` incluye `CREADA`. El rol `INTERMEDIO` se agrego a los tipos y a la administracion de usuarios.

### Limitaciones conocidas

- `TRUNCATE TABLE` y `ALTER TABLE ... SWITCH` no disparan triggers: un login con permiso `ALTER` (por ejemplo `sa` o `db_owner`) puede saltarse el `INSTEAD OF DELETE` del detalle. La API solo ejecuta DML. En produccion la app deberia usar un login sin `ALTER` sobre estas tablas.
- Las politicas se evaluan con el rol que trae el JWT. Si a un usuario se le cambia el rol, el access token vigente conserva el anterior hasta que expira (30 minutos).
- El `Task.Run` de `ANULACION_MASTER` no es durable: si el proceso se detiene antes de que MongoDB confirme, el evento se pierde (si agota los reintentos queda el `LogError`).

## Documentos: validaciones y limitacion conocida

Cada archivo subido se valida por extension, Content-Type declarado, tamano maximo (5MB), longitud y caracteres del nombre, y firma binaria real (magic bytes) para PDF/JPEG. Esto bloquea archivos con extension incorrecta, contenido que no coincide con la extension declarada, y nombres de archivo maliciosos (path traversal, bytes de control).

### Transaccionalidad

La subida y el Soft Delete de documentos corren dentro de una transaccion explicita (`BeginTransactionAsync` con `IsolationLevel.ReadCommitted`), porque la operacion mezcla una escritura en disco con una escritura en la base:

- **Compensacion:** si `SaveChangesAsync` falla despues de guardar el archivo, el `catch` hace rollback y borra el archivo con `IFileStorageService.Delete`, para que no quede huerfano en disco.
- **Concurrencia:** `UserDocuments` tiene una columna `rowversion` (`IsRowVersion()`). EF Core la incluye en el `WHERE` de cada UPDATE/DELETE y lanza `DbUpdateConcurrencyException` si otro proceso toco la fila primero; el servicio la traduce a un `409 Conflict`. Es bloqueo optimista: no bloquea filas.
- **Auditoria:** el `finally` encola el evento siempre, incluso cuando el archivo fue rechazado. Es una escritura en memoria, asi que no retrasa la respuesta ni puede provocar el fallo de la transaccion.
- **Soft Delete:** `DELETE /api/users/{userId}/documents/{documentId}` ya no borra la fila ni el archivo. Hace un `UPDATE` de `IsDeleted = 1`, `DeletedAt` y `DeletedByUserId` (el `sub` del JWT: el propio usuario o un ADMIN) y audita `DOCUMENT_DELETED` con el antes y el despues. Un filtro global de EF (`HasQueryFilter(d => !d.IsDeleted)`) oculta el documento del listado, de la descarga (`404`) y del `GET /api/users/{id}`. El archivo queda en disco como evidencia. Un `DELETE` directo en SQL responde `50020` (`TR_UserDocuments_NoDelete`); para permitir ese trigger la FK `UserDocuments -> Users` paso de `CASCADE` a `NO ACTION`. Script manual: `sql/03_soft_delete_documentos.sql`.

**Limitacion conocida:** no se inspecciona el contenido interno de los PDF aceptados. Un PDF valido puede llevar JavaScript embebido (`/JavaScript`, `/OpenAction`, `/AA`) que algunos lectores de PDF vulnerables podrian ejecutar al abrirlo. Sanitizar esto requeriria una libreria de parseo de PDF (ej. iText7, que es AGPL o de pago) o un escaner de malware externo (ej. ClamAV), lo cual se considero fuera de alcance para este proyecto.


## Fase 3: Big Data, ranking con RANK() y reportes

Carga masiva por conjuntos (sin `WHILE` ni cursores), un ranking calculado por SQL Server y tres reportes. Los numeros medidos de punta a punta estan en `docs/Fase3-Evidencia.md`.

| Script | Que hace |
|---|---|
| `sql/04_fase3_big_data.sql` | Inyecta **250 encabezados x 200 lineas = 50,000 lineas** en el anio fiscal 2026 con dos `INSERT ... SELECT` (CTE tally sobre `master.dbo.spt_values` + `OUTPUT ... INTO @Encabezados` + `CROSS JOIN`), dentro de una transaccion con `TRY/CATCH` y `ROLLBACK`. Imprime los milisegundos de cada INSERT, deja un resumen y termina con la evidencia de particiones. |
| `sql/05_fase3_evidencia_particiones.sql` | Solo la consulta a `sys.partitions` de `TB_Ordenes_Compra` y `TB_Ordenes_Compra_Detalle` (filas por particion), para la demo en video. |
| `sql/06_fase3_ranking_reportes.sql` | `dbo.FN_Ranking_Proveedores(@Anio)`. En la aplicacion la crea la migracion `Fase3ReportesRanking` (`CREATE OR ALTER`, con `Down` que la elimina). |

Reglas de la carga masiva:

- Cada corrida es un **lote** nuevo: `NumeroOrden = BD26-L{lote:00}-{n:000000}` con `lote = 1 + el mayor lote existente`, asi que se puede re-correr. Las lineas se cuelgan solo de los encabezados de ese lote (`OUTPUT ... INTO`).
- 12 proveedores guatemaltecos con reparto sesgado (los primeros de la lista salen mucho mas) para que el ranking tenga ganadores claros. Cantidad 1..5 y precio 1.00..19.99: ~1.6M GTQ por lote repartidos en 4 bodegas con presupuesto (no se usa `Bodega Central Zona 12`, que solo tiene 25,000 en 2026, ni las `GT-*`). Monto va en 0 y lo recalcula `TR_OC_Detalle_Presupuesto`, que tambien compromete el presupuesto.
- Ya con `COMMIT`, el script anula 10 encabezados de prueba (`Estado = ANULADA` con `UsuarioAnulaId`, `FechaAnulacion`, `MotivoAnulacion`) y da de baja 50 lineas (`EstadoLinea = 0` con quien, cuando y por que) para que el reporte de auditoria tenga datos. Los triggers liberan el presupuesto.

### Ranking de proveedores (SQL Server)

`dbo.FN_Ranking_Proveedores(@Anio smallint)` es una funcion en linea: suma el `Importe` de las lineas vigentes (`EstadoLinea = 1`) de las ordenes no anuladas del anio y numera con `RANK() OVER (ORDER BY SUM(Importe) DESC)`; devuelve tambien ordenes, lineas y `ParticipacionPct` (`SUM(SUM()) OVER ()`). Filtra por `FechaEmision` (>= 1 de enero, < 1 de enero siguiente) y une el detalle por `(FechaEmision, OrdenId)`, asi SQL Server elimina particiones. El backend **no ordena ni rankea en memoria**: ejecuta `SELECT TOP (@top) * FROM dbo.FN_Ranking_Proveedores(@anio) ORDER BY PosicionRanking, ProveedorNit` con `Database.SqlQuery<T>` parametrizado.

### Endpoints

Todos bajo `/api/reportes` (`ReportesController`, `[Authorize]`). JSON en camelCase. Cada consulta deja un evento de auditoria (`REPORTE_CONSULTADO` o `REPORTE_AUDITORIA_CONSULTADO`) con los filtros usados.

| Metodo | Ruta | Politica | Descripcion |
|---|---|---|---|
| GET | `/ranking-proveedores?anio=2026&top=20` | `NivelEstandar` | `anio` 2024..2027 (default el actual) y `top` 1..100 (default 20); fuera de rango responde `422`. Devuelve `[{ posicionRanking, proveedorNit, proveedorNombre, totalComprometido, ordenes, lineas, participacionPct }]`. |
| GET | `/ordenes?fechaDesde=&fechaHasta=&proveedorNit=&estado=&bodega=&page=1&pageSize=50` | `NivelEstandar` | Reporte transaccional. **`fechaDesde` y `fechaHasta` son obligatorias** (son la clave de particion): si falta alguna responde `400` con `Debe indicar el rango de fechas (desde y hasta) para ejecutar el reporte.`; tambien `400` si desde > hasta o el rango pasa de 366 dias. `pageSize` maximo 200. Orden: `FechaEmision DESC, NumeroOrden`. Devuelve `{ items, total, page, pageSize, montoTotal }`; `montoTotal` es la suma de todas las filas del filtro, no solo de la pagina. |
| GET | `/auditoria/soft-delete?tipo=ordenes\|lineas\|documentos&fechaDesde=&fechaHasta=&page=1&pageSize=50` | `NivelAdministrador` | Solo registros con soft delete **y** traza (orden `ANULADA` con `FechaAnulacion`, linea con `EstadoLinea = 0` y `FechaBaja`, documento con `IsDeleted`), con el username de quien dio la baja. Estandar e Intermedio reciben `403`. `tipo` invalido responde `422`. Devuelve `{ items, total, page, pageSize, resumen: { ordenes, lineas, documentos } }`. |

Notas: el rol Estandar nunca ve ordenes `ANULADA` en el reporte de ordenes (misma regla que `/api/operaciones/ordenes`), y las `ANULADA` del stress test que no tienen `FechaAnulacion` no aparecen en la auditoria porque no llevan traza. La coleccion de Postman incluye la carpeta **Fase 3 - Reportes y Big Data** con los casos 200, 400 y 403.
