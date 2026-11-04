# Fase 2 - Evidencia de verificacion de punta a punta

**Fecha de ejecucion:** 2026-09-26 05:10:47 UTC, contra la VM de VirtualBox 'ubuntu server' (SQL Server 2022 y MongoDB 7 del stack `db-infra`; IP DHCP 192.168.1.179 durante la corrida).  
**Quien:** verificacion automatizada (etapa 6 del workflow), actuando como catedratico: peticiones HTTP reales contra el API y consultas SQL reales con `SqlConnection`.  
**Resultado:** los 11 escenarios PASAN (30 comprobaciones, 0 fallidas). El documento de auditoria se leyo de MongoDB (seccion 9).

## 1. Entorno y como se levanto el API

- **Base de datos real:** `SeminarioDb` en el SQL Server 2022 de la VM (puerto 1433) y la coleccion `audit_logs` del MongoDB 7 de la misma VM (`:27017`), ambos contenedores del stack `db-infra`. La VM toma su IP por DHCP y cambio durante el trabajo (.181 -> .179 -> .135): el `.env` apunta a la IP vigente; conviene fijarle una IP estatica.
- La migracion `20260926031522_Fase2OrdenCompraDetalle` (tabla de detalle particionada, presupuesto, SP y triggers) se aplico con `dotnet ef database update` sobre la base ya cargada con **50,000 ordenes del stress test**; tardo segundos, sin tocar el timeout por defecto.
- Al arrancar, `DbSeeder` sembro el rol `INTERMEDIO` y los usuarios de demo `intermedio` y `estandar` (entorno Development). El API se levanto con la configuracion del `.env`, sin overrides.

```powershell
dotnet build --nologo -v q          # Build succeeded, 0 Warning(s), 0 Error(s)
Start-Process dotnet -ArgumentList 'run --no-build --launch-profile http' -WorkingDirectory <repo>\seminario-backend-service `
  -RedirectStandardOutput $env:TEMP\api-fase2.log -RedirectStandardError $env:TEMP\api-fase2.err -PassThru
# GET http://localhost:5000/api/health -> 200 {"status":"ok","service":"seminario-backend-service",...}
```

Login de cada usuario: `POST /api/auth/login` -> OTP de 6 digitos leido del log del backend (`Codigo MFA para el usuario N: ######`) -> `POST /api/auth/mfa/verify` -> `accessToken`. Cada usuario entro por una direccion de loopback distinta (127.0.0.1, [::1], localhost) porque el rate limit de auth es 5/min por IP. Claims leidos de cada JWT:

| usuario | rol (ClaimTypes.Role) | nivel |
|---|---|---|
| estandar | USER | 1 |
| intermedio | INTERMEDIO | 2 |
| admin | ADMIN | 3 |

Orden bajo prueba: **OC-2026-000005** (id `1f48016d-114e-476f-ae96-2cb7381a2b36`, fechaEmision 2026-09-25), bodega GT-CENTRO, anio 2026. NIT del escenario 1: `77596141-1`; NIT del escenario 2 (rollback): `78596141-2`.

## 2. Tabla de escenarios

| # | Escenario | Resultado | Evidencia resumida |
|---|---|---|---|
| 1 | Estandar POST con 3 lineas -> 201 CREADA, monto = suma; comprometido sube exactamente el monto | **PASA** | POST 3 lineas -> 201, CREADA, monto = suma; comprometido subio exactamente el monto |
| 2 | Estandar POST con una linea de cantidad 0 -> 422 y ROLLBACK absoluto (API + SQL directo) | **PASA** | POST con una linea de cantidad 0 -> 422; GET listado por proveedorNit no la muestra; SQL directo: 0 encabezados y 0 lineas del proveedor, totales sin cambio |
| 3 | Estandar PUT linea -> 403 y DELETE -> 403 | **PASA** | Estandar PUT linea 1 -> 403; Estandar DELETE -> 403 |
| 4 | Intermedio PUT linea 1 -> 200 (monto y comprometido bajan el importe); repetido -> 409; sin motivo -> 422 | **PASA** | Intermedio PUT linea 1 -> 200, monto bajo el importe de la linea; comprometido bajo lo mismo (capacidad devuelta por el trigger); mismo PUT repetido -> 409; PUT sin motivo -> 422 |
| 5 | Intermedio DELETE -> 403 | **PASA** | Intermedio DELETE -> 403 |
| 6 | Admin DELETE con motivo -> 200 ANULADA, presupuesto vuelve al inicial; repetido -> 409; sin motivo -> 422 | **PASA** | Admin DELETE sin motivo -> 422; Admin DELETE con motivo -> 200 ANULADA; comprometido volvio a su valor inicial; DELETE repetido -> 409 |
| 7 | Estandar no ve la ANULADA (listado y GET id 404); Admin si (200 ANULADA); la fila sigue en SQL | **PASA** | Estandar GET /ordenes?proveedorNit no muestra la ANULADA; Estandar GET /ordenes?estado=ANULADA tampoco; Estandar GET /ordenes/{id} -> 404; Admin GET /ordenes/{id} -> 200 ANULADA; Admin GET /ordenes?proveedorNit si la muestra; SQL directo: la fila sigue en la base con Estado = ANULADA |
| 8 | SQL directo: DELETE fisico -> 50020; EXEC del SP con 2 lineas devuelve el encabezado | **PASA** | DELETE FROM TB_Ordenes_Compra_Detalle -> error 50020; DELETE FROM TB_Ordenes_Compra -> error 50020; las lineas siguen intactas; EXEC SP_OrdenCompra_Crear con 2 lineas devuelve el encabezado; el SP inserto las 2 lineas |
| 9 | MongoDB: documento ANULACION_MASTER con los campos exigidos | **PASA** | Documento leido de MongoDB con `accion = ANULACION_MASTER` y los 9 campos exigidos (seccion 9) |
| 10 | Tiempo de respuesta del DELETE (Measure-Command) menor que el timeout de Mongo | **PASA** | DELETE no espera al hilo de auditoria |
| 11 | Stress test: Admin POST /api/seed-data 2000 filas -> 200 | **PASA** | Admin POST /api/seed-data 2000 -> 200 |

## 3. Detalle por escenario

Todos los fragmentos son respuestas reales (recortadas a los campos relevantes). Los importes en JSON salen como numeros.

### Escenario 1 - POST de Estandar con 3 lineas

- PASA - POST 3 lineas -> 201, CREADA, monto = suma: HTTP 201, estado=CREADA (se envio estado=PAGADA y se ignoro), monto=25342.5, suma esperada=25342.5, numeroOrden=OC-2026-000005
- PASA - comprometido subio exactamente el monto: comprometido 0.00 -> 25342.50 (delta 25342.50)

Presupuesto GT-CENTRO/2026 antes:
```json
{"anio": 2026, "bodega": "GT-CENTRO", "montoAsignado": 2500000.0, "montoComprometido": 0.0, "montoDisponible": 2500000.0}
```
```http
POST /api/operaciones/ordenes {"proveedorNit": "77596141-1", "proveedorNombre": "Ferreteria El Constructor S.A.", "moneda": "GTQ", "bodegaDestino": "GT-CENTRO", "fechaEmision": "2026-09-25", "detalle": "Evidencia Fase 2 - escenario 1", "estado": "PAGADA", "lineas": [{"descripcion": "Cemento gris 42.5 kg", "cantidad": 120, "precioUnitario": 86.5}, {"descripcion": "Varilla corrugada 3/8", "cantidad": 250, "precioUnitario": 41.25}, {"descripcion": "Alambre de amarre (rollo)", "cantidad": 15, "precioUnitario": 310.0}]}
-> HTTP 201 (813.3 ms)
```
```json
{
  "id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
  "numeroOrden": "OC-2026-000005",
  "fechaEmision": "2026-09-25T00:00:00",
  "userId": 7,
  "proveedorNit": "77596141-1",
  "estado": "CREADA",
  "monto": 25342.5,
  "bodegaDestino": "GT-CENTRO",
  "usuarioAnulaId": null,
  "fechaAnulacion": null,
  "motivoAnulacion": null,
  "rowVer": "AAAAAAABCs4=",
  "lineas": [
    {
      "linea": 1,
      "descripcion": "Cemento gris 42.5 kg",
      "cantidad": 120.0,
      "precioUnitario": 86.5,
      "importe": 10380.0,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    },
    {
      "linea": 2,
      "descripcion": "Varilla corrugada 3/8",
      "cantidad": 250.0,
      "precioUnitario": 41.25,
      "importe": 10312.5,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    },
    {
      "linea": 3,
      "descripcion": "Alambre de amarre (rollo)",
      "cantidad": 15.0,
      "precioUnitario": 310.0,
      "importe": 4650.0,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    }
  ],
  "presupuesto": {
    "anio": 2026,
    "bodega": "GT-CENTRO",
    "montoAsignado": 2500000.0,
    "montoComprometido": 25342.5,
    "montoDisponible": 2474657.5
  }
}
```

Nota: se envio `"estado":"PAGADA"` en el cuerpo y la orden nacio igual en CREADA (el DTO ya no acepta estado).

### Escenario 2 - ROLLBACK absoluto (linea 3 con cantidad 0)

- PASA - POST con una linea de cantidad 0 -> 422: HTTP 422: Datos de la orden invalidos. No se guardo la orden ni ninguna linea. ['Linea 3 invalida: cantidad debe ser un numero mayor que 0']
- PASA - GET listado por proveedorNit no la muestra: HTTP 200, total=0
- PASA - SQL directo: 0 encabezados y 0 lineas del proveedor, totales sin cambio: antes={'EncabezadosProveedor': 0, 'LineasProveedor': 0, 'LineasTotales': 9, 'EncabezadosTotales': 54003} despues={'EncabezadosProveedor': 0, 'LineasProveedor': 0, 'LineasTotales': 9, 'EncabezadosTotales': 54003}; comprometido 25342.50 -> 25342.50

```http
POST /api/operaciones/ordenes {"proveedorNit": "78596141-2", "proveedorNombre": "Distribuidora Rollback S.A.", "moneda": "GTQ", "bodegaDestino": "GT-CENTRO", "fechaEmision": "2026-09-25", "lineas": [{"descripcion": "Linea valida 1", "cantidad": 10, "precioUnitario": 100}, {"descripcion": "Linea valida 2", "cantidad": 5, "precioUnitario": 20}, {"descripcion": "Linea con cantidad cero", "cantidad": 0, "precioUnitario": 50}]}
-> HTTP 422 (333.6 ms)
```
```json
{
  "success": false,
  "message": "Datos de la orden invalidos. No se guardo la orden ni ninguna linea.",
  "statusCode": 422,
  "errors": [
    "Linea 3 invalida: cantidad debe ser un numero mayor que 0"
  ]
}
```

```http
GET /api/operaciones/ordenes?proveedorNit=78596141-2
-> HTTP 200 (303.0 ms)
```
```json
{
  "total": 0,
  "page": 1,
  "items": []
}
```

Consulta SQL directa (antes y despues del POST, mismos valores):

```sql
SELECT (SELECT COUNT(*) FROM dbo.TB_Ordenes_Compra WHERE ProveedorNit = N'78596141-2') AS EncabezadosProveedor,
 (SELECT COUNT(*) FROM dbo.TB_Ordenes_Compra_Detalle d JOIN dbo.TB_Ordenes_Compra o ON o.FechaEmision = d.FechaEmision AND o.Id = d.OrdenId WHERE o.ProveedorNit = N'78596141-2') AS LineasProveedor,
 (SELECT COUNT(*) FROM dbo.TB_Ordenes_Compra_Detalle) AS LineasTotales,
 (SELECT COUNT(*) FROM dbo.TB_Ordenes_Compra) AS EncabezadosTotales
```
```json
{
  "ok": true,
  "rows": [
    {
      "EncabezadosProveedor": 0,
      "LineasProveedor": 0,
      "LineasTotales": 9,
      "EncabezadosTotales": 54003
    }
  ]
}
```

Nota: el SP ya habia insertado el encabezado y las lineas 1 y 2 cuando fallo la linea 3; el `ROLLBACK` los elimino todos. La secuencia `SEQ_OrdenCompra_Numero` no es transaccional, por eso el numero OC-2026-000002 se consumio y la siguiente orden (escenario 8) es OC-2026-000003.

### Escenario 3 - Estandar no puede PUT ni DELETE

- PASA - Estandar PUT linea 1 -> 403: HTTP 403: No tiene permisos para realizar esta accion.
- PASA - Estandar DELETE -> 403: HTTP 403: No tiene permisos para realizar esta accion.

```http
PUT /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36/lineas/1?fechaEmision=2026-09-25 {"motivo": "intento de estandar"}
-> HTTP 403 (12.5 ms)
```
```json
{
  "success": false,
  "message": "No tiene permisos para realizar esta accion.",
  "statusCode": 403,
  "errors": []
}
```

```http
DELETE /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25 {"motivo": "intento de estandar"}
-> HTTP 403 (7.0 ms)
```
```json
{
  "success": false,
  "message": "No tiene permisos para realizar esta accion.",
  "statusCode": 403,
  "errors": []
}
```

### Escenario 4 - Intermedio anula la linea 1 (trigger devuelve la capacidad)

- PASA - Intermedio PUT linea 1 -> 200, monto bajo el importe de la linea: HTTP 200, monto 25342.50 -> 14962.5 (importe linea 1 = 10380.00); linea 1: estadoLinea=False, usuarioBajaId=6, motivoBaja='Proveedor sin existencia de cemento'
- PASA - comprometido bajo lo mismo (capacidad devuelta por el trigger): comprometido 25342.50 -> 14962.50 (delta -10380.00); presupuesto en la respuesta: {'anio': 2026, 'bodega': 'GT-CENTRO', 'montoAsignado': 2500000.0, 'montoComprometido': 14962.5, 'montoDisponible': 2485037.5}
- PASA - mismo PUT repetido -> 409: HTTP 409: La linea 1 de la orden OC-2026-000005 ya estaba anulada
- PASA - PUT sin motivo -> 422: HTTP 422: Datos invalidos ["El motivo de la baja debe tener entre 3 y 200 caracteres sin contar espacios (en el cuerpo 'motivo' o en ?motivo=)"]

```http
PUT /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36/lineas/1?fechaEmision=2026-09-25 {"motivo": "Proveedor sin existencia de cemento"}
-> HTTP 200 (1316.7 ms)
```
```json
{
  "id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
  "numeroOrden": "OC-2026-000005",
  "fechaEmision": "2026-09-25T00:00:00",
  "userId": 7,
  "proveedorNit": "77596141-1",
  "estado": "CREADA",
  "monto": 14962.5,
  "bodegaDestino": "GT-CENTRO",
  "usuarioAnulaId": null,
  "fechaAnulacion": null,
  "motivoAnulacion": null,
  "rowVer": "AAAAAAABCtQ=",
  "lineas": [
    {
      "linea": 1,
      "descripcion": "Cemento gris 42.5 kg",
      "cantidad": 120.0,
      "precioUnitario": 86.5,
      "importe": 10380.0,
      "estadoLinea": false,
      "usuarioBajaId": 6,
      "motivoBaja": "Proveedor sin existencia de cemento"
    },
    {
      "linea": 2,
      "descripcion": "Varilla corrugada 3/8",
      "cantidad": 250.0,
      "precioUnitario": 41.25,
      "importe": 10312.5,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    },
    {
      "linea": 3,
      "descripcion": "Alambre de amarre (rollo)",
      "cantidad": 15.0,
      "precioUnitario": 310.0,
      "importe": 4650.0,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    }
  ],
  "presupuesto": {
    "anio": 2026,
    "bodega": "GT-CENTRO",
    "montoAsignado": 2500000.0,
    "montoComprometido": 14962.5,
    "montoDisponible": 2485037.5
  }
}
```

```http
PUT /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36/lineas/1?fechaEmision=2026-09-25 {"motivo": "Proveedor sin existencia de cemento"}
-> HTTP 409 (41.3 ms)
```
```json
{
  "success": false,
  "message": "La linea 1 de la orden OC-2026-000005 ya estaba anulada",
  "statusCode": 409,
  "errors": []
}
```

```http
PUT /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36/lineas/2?fechaEmision=2026-09-25 {}
-> HTTP 422 (7.4 ms)
```
```json
{
  "success": false,
  "message": "Datos invalidos",
  "statusCode": 422,
  "errors": [
    "El motivo de la baja debe tener entre 3 y 200 caracteres sin contar espacios (en el cuerpo 'motivo' o en ?motivo=)"
  ]
}
```

### Escenario 5 - Intermedio no puede DELETE

- PASA - Intermedio DELETE -> 403: HTTP 403: No tiene permisos para realizar esta accion.

```http
DELETE /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25 {"motivo": "intento de intermedio"}
-> HTTP 403 (4.8 ms)
```
```json
{
  "success": false,
  "message": "No tiene permisos para realizar esta accion.",
  "statusCode": 403,
  "errors": []
}
```

### Escenario 6 - Administrador: Soft Delete (ANULADA)

- PASA - Admin DELETE sin motivo -> 422: HTTP 422: Datos invalidos ["La justificacion de la anulacion debe tener entre 3 y 200 caracteres sin contar espacios (en el cuerpo 'motivo' o en ?motivo=)"]
- PASA - Admin DELETE con motivo -> 200 ANULADA: HTTP 200, estado=ANULADA, motivoAnulacion='El proveedor no puede surtir', usuarioAnulaId=1, fechaAnulacion=2026-09-26T05:10:47
- PASA - comprometido volvio a su valor inicial: inicial 0.00, tras anular 0.00
- PASA - DELETE repetido -> 409: HTTP 409: Solo se puede anular una orden en estado CREADA o EMITIDA; la orden OC-2026-000005 esta ANULADA

```http
DELETE /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25 {}
-> HTTP 422 (21.0 ms)
```
```json
{
  "success": false,
  "message": "Datos invalidos",
  "statusCode": 422,
  "errors": [
    "La justificacion de la anulacion debe tener entre 3 y 200 caracteres sin contar espacios (en el cuerpo 'motivo' o en ?motivo=)"
  ]
}
```

```http
DELETE /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25 {"motivo": "El proveedor no puede surtir"}
-> HTTP 200 (583.4 ms)
```
```json
{
  "id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
  "numeroOrden": "OC-2026-000005",
  "fechaEmision": "2026-09-25T00:00:00",
  "userId": 7,
  "proveedorNit": "77596141-1",
  "estado": "ANULADA",
  "monto": 14962.5,
  "bodegaDestino": "GT-CENTRO",
  "usuarioAnulaId": 1,
  "fechaAnulacion": "2026-09-26T05:10:47",
  "motivoAnulacion": "El proveedor no puede surtir",
  "rowVer": "AAAAAAABCtY=",
  "lineas": [
    {
      "linea": 1,
      "descripcion": "Cemento gris 42.5 kg",
      "cantidad": 120.0,
      "precioUnitario": 86.5,
      "importe": 10380.0,
      "estadoLinea": false,
      "usuarioBajaId": 6,
      "motivoBaja": "Proveedor sin existencia de cemento"
    },
    {
      "linea": 2,
      "descripcion": "Varilla corrugada 3/8",
      "cantidad": 250.0,
      "precioUnitario": 41.25,
      "importe": 10312.5,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    },
    {
      "linea": 3,
      "descripcion": "Alambre de amarre (rollo)",
      "cantidad": 15.0,
      "precioUnitario": 310.0,
      "importe": 4650.0,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    }
  ],
  "presupuesto": {
    "anio": 2026,
    "bodega": "GT-CENTRO",
    "montoAsignado": 2500000.0,
    "montoComprometido": 0.0,
    "montoDisponible": 2500000.0
  }
}
```

```http
DELETE /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25 {"motivo": "El proveedor no puede surtir"}
-> HTTP 409 (25.1 ms)
```
```json
{
  "success": false,
  "message": "Solo se puede anular una orden en estado CREADA o EMITIDA; la orden OC-2026-000005 esta ANULADA",
  "statusCode": 409,
  "errors": []
}
```

### Escenario 7 - Visibilidad despues de anular

- PASA - Estandar GET /ordenes?proveedorNit no muestra la ANULADA: HTTP 200, total=0
- PASA - Estandar GET /ordenes?estado=ANULADA tampoco: HTTP 200, total=0
- PASA - Estandar GET /ordenes/{id} -> 404: HTTP 404: La orden de compra no existe
- PASA - Admin GET /ordenes/{id} -> 200 ANULADA: HTTP 200, estado=ANULADA, numeroOrden=OC-2026-000005, lineas=3
- PASA - Admin GET /ordenes?proveedorNit si la muestra: HTTP 200, total=1
- PASA - SQL directo: la fila sigue en la base con Estado = ANULADA: [{"Id": "1f48016d-114e-476f-ae96-2cb7381a2b36", "NumeroOrden": "OC-2026-000005", "Estado": "ANULADA", "Monto": "14962.50", "UsuarioAnulaId": 1, "FechaAnulacion": "2026-09-26T05:10:47", "MotivoAnulacion": "El proveedor no puede surtir"}]

```http
GET /api/operaciones/ordenes?proveedorNit=77596141-1
-> HTTP 200 (48.5 ms)
```
```json
{
  "total": 0,
  "page": 1,
  "items": []
}
```

```http
GET /api/operaciones/ordenes?proveedorNit=77596141-1&estado=ANULADA
-> HTTP 200 (77.5 ms)
```
```json
{
  "total": 0,
  "page": 1,
  "items": []
}
```

```http
GET /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25
-> HTTP 404 (31.7 ms)
```
```json
{
  "success": false,
  "message": "La orden de compra no existe",
  "statusCode": 404,
  "errors": []
}
```

```http
GET /api/operaciones/ordenes/1f48016d-114e-476f-ae96-2cb7381a2b36?fechaEmision=2026-09-25
-> HTTP 200 (25.2 ms)
```
```json
{
  "id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
  "numeroOrden": "OC-2026-000005",
  "fechaEmision": "2026-09-25T00:00:00",
  "userId": 7,
  "proveedorNit": "77596141-1",
  "estado": "ANULADA",
  "monto": 14962.5,
  "bodegaDestino": "GT-CENTRO",
  "usuarioAnulaId": 1,
  "fechaAnulacion": "2026-09-26T05:10:47",
  "motivoAnulacion": "El proveedor no puede surtir",
  "rowVer": "AAAAAAABCtY=",
  "lineas": [
    {
      "linea": 1,
      "descripcion": "Cemento gris 42.5 kg",
      "cantidad": 120.0,
      "precioUnitario": 86.5,
      "importe": 10380.0,
      "estadoLinea": false,
      "usuarioBajaId": 6,
      "motivoBaja": "Proveedor sin existencia de cemento"
    },
    {
      "linea": 2,
      "descripcion": "Varilla corrugada 3/8",
      "cantidad": 250.0,
      "precioUnitario": 41.25,
      "importe": 10312.5,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    },
    {
      "linea": 3,
      "descripcion": "Alambre de amarre (rollo)",
      "cantidad": 15.0,
      "precioUnitario": 310.0,
      "importe": 4650.0,
      "estadoLinea": true,
      "usuarioBajaId": null,
      "motivoBaja": null
    }
  ],
  "presupuesto": {
    "anio": 2026,
    "bodega": "GT-CENTRO",
    "montoAsignado": 2500000.0,
    "montoComprometido": 0.0,
    "montoDisponible": 2500000.0
  }
}
```

```http
GET /api/operaciones/ordenes?proveedorNit=77596141-1
-> HTTP 200 (4152.9 ms)
```
```json
{
  "total": 1,
  "page": 1,
  "items": [
    {
      "id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
      "numeroOrden": "OC-2026-000005",
      "estado": "ANULADA",
      "monto": 14962.5,
      "proveedorNit": "77596141-1"
    }
  ]
}
```

Fila en SQL Server (sigue existiendo, no hubo DELETE fisico):

```sql
SELECT Id, NumeroOrden, Estado, Monto, UsuarioAnulaId, FechaAnulacion, MotivoAnulacion FROM dbo.TB_Ordenes_Compra WHERE Id = '1f48016d-114e-476f-ae96-2cb7381a2b36'
```
```json
{
  "ok": true,
  "rows": [
    {
      "Id": "1f48016d-114e-476f-ae96-2cb7381a2b36",
      "NumeroOrden": "OC-2026-000005",
      "Estado": "ANULADA",
      "Monto": "14962.50",
      "UsuarioAnulaId": 1,
      "FechaAnulacion": "2026-09-26T05:10:47",
      "MotivoAnulacion": "El proveedor no puede surtir"
    }
  ]
}
```

### Escenario 8 - Pruebas directas en SQL Server

- PASA - DELETE FROM TB_Ordenes_Compra_Detalle -> error 50020: {"ok": false, "number": 50020, "error": "Borrado fisico prohibido: use Soft Delete"}
- PASA - DELETE FROM TB_Ordenes_Compra -> error 50020: {"ok": false, "number": 50020, "error": "Borrado fisico prohibido: use Soft Delete"}
- PASA - las lineas siguen intactas: lineas de la orden: 3 -> 3
- PASA - EXEC SP_OrdenCompra_Crear con 2 lineas devuelve el encabezado: [{"Id": "f1741930-830f-4bdd-bb72-9f405bd72044", "FechaEmision": "2026-09-25T00:00:00", "NumeroOrden": "OC-2026-000007", "Monto": "6950.00", "Estado": "CREADA", "RowVer": "AAAAAAABCt8="}]
- PASA - el SP inserto las 2 lineas: [{"Linea": 1, "Descripcion": "Block 15x20x40", "Cantidad": "1000.000", "PrecioUnitario": "4.8500", "Importe": "4850.00", "EstadoLinea": true}, {"Linea": 2, "Descripcion": "Arena de rio (m3)", "Cantidad": "12.000", "PrecioUnitario": "175.0000", "Importe": "2100.00", "EstadoLinea": true}]

```sql
DELETE FROM dbo.TB_Ordenes_Compra_Detalle WHERE OrdenId = '1f48016d-114e-476f-ae96-2cb7381a2b36'
```
```json
{
  "ok": false,
  "number": 50020,
  "error": "Borrado fisico prohibido: use Soft Delete"
}
```

```sql
DELETE FROM dbo.TB_Ordenes_Compra WHERE Id = '1f48016d-114e-476f-ae96-2cb7381a2b36'
```
```json
{
  "ok": false,
  "number": 50020,
  "error": "Borrado fisico prohibido: use Soft Delete"
}
```

```sql
DECLARE @j nvarchar(max) = N'{"fechaEmision": "2026-09-25", "proveedorNit": "79596141-3", "proveedorNombre": "Proveedor directo SQL", "moneda": "GTQ", "bodegaDestino": "GT-SUR", "detalle": "Ejecucion directa del SP", "lineas": [{"descripcion": "Block 15x20x40", "cantidad": 1000, "precioUnitario": 4.85}, {"descripcion": "Arena de rio (m3)", "cantidad": 12, "precioUnitario": 175}]}';
EXEC dbo.SP_OrdenCompra_Crear @Json = @j, @UserId = 1;
```
```json
{
  "ok": true,
  "rows": [
    {
      "Id": "f1741930-830f-4bdd-bb72-9f405bd72044",
      "FechaEmision": "2026-09-25T00:00:00",
      "NumeroOrden": "OC-2026-000007",
      "Monto": "6950.00",
      "Estado": "CREADA",
      "RowVer": "AAAAAAABCt8="
    }
  ]
}
```

```sql
SELECT Linea, Descripcion, Cantidad, PrecioUnitario, Importe, EstadoLinea FROM dbo.TB_Ordenes_Compra_Detalle WHERE OrdenId = 'f1741930-830f-4bdd-bb72-9f405bd72044' ORDER BY Linea
```
```json
{
  "ok": true,
  "rows": [
    {
      "Linea": 1,
      "Descripcion": "Block 15x20x40",
      "Cantidad": "1000.000",
      "PrecioUnitario": "4.8500",
      "Importe": "4850.00",
      "EstadoLinea": true
    },
    {
      "Linea": 2,
      "Descripcion": "Arena de rio (m3)",
      "Cantidad": "12.000",
      "PrecioUnitario": "175.0000",
      "Importe": "2100.00",
      "EstadoLinea": true
    }
  ]
}
```

### Escenario 9 - Documento ANULACION_MASTER en MongoDB

Consulta real a la coleccion `audit_logs` de la VM, hecha con el driver oficial (`MongoDB.Driver`) justo despues del DELETE: ultimo documento con `accion = "ANULACION_MASTER"`, ordenado por `fecha_evento` descendente. Equivale en mongosh a `db.audit_logs.find({accion:"ANULACION_MASTER"}).sort({fecha_evento:-1}).limit(1)`.

```json
{
  "_id" : { "$oid" : "6ab753d77b66272dfde48b34" },
  "fecha_evento" : { "$date" : "2026-09-26T05:10:47.535Z" },
  "modulo" : "ORDEN_COMPRA",
  "accion" : "ANULACION_MASTER",
  "entidad_afectada_id" : "OC-2026-000005",
  "jwt_usuario_ejecutor" : "admin",
  "rol_auth" : "Administrador",
  "justificacion_cambio" : "El proveedor no puede surtir",
  "datos_anteriores" : {
    "estado" : "CREADA"
  },
  "datos_nuevos" : {
    "estado" : "ANULADA"
  },
  "actor_id" : 1,
  "usuario_afectado_id" : 7,
  "ip_origen" : "::1"
}
```

Los nueve campos exigidos por la consigna estan presentes: `fecha_evento`, `modulo`, `accion`, `entidad_afectada_id`, `jwt_usuario_ejecutor` (username del JWT del Administrador), `rol_auth`, `justificacion_cambio` (el motivo enviado en el DELETE), `datos_anteriores.estado = "CREADA"` y `datos_nuevos.estado = "ANULADA"`. Los campos extra (`actor_id`, `usuario_afectado_id`, `ip_origen`) vienen de la auditoria de la Fase 1.

El Soft Delete de la linea (escenario 4) tambien quedo auditado, por el canal asincrono normal, con `rol_auth = "Intermedio"`:

```json
{
  "_id" : { "$oid" : "6ab753d57b66272dfde48b33" },
  "fecha_evento" : { "$date" : "2026-09-26T05:10:45.683Z" },
  "modulo" : "ORDEN_COMPRA",
  "accion" : "LINEA_ANULADA",
  "entidad_afectada_id" : "OC-2026-000005#1",
  "jwt_usuario_ejecutor" : "intermedio",
  "rol_auth" : "Intermedio",
  "justificacion_cambio" : "Proveedor sin existencia de cemento",
  "datos_anteriores" : {
    "linea" : 1,
    "estadoLinea" : 1,
    "importe" : { "$numberDecimal" : "10380.00" },
    "montoOrden" : { "$numberDecimal" : "25342.50" },
    "comprometido" : { "$numberDecimal" : "25342.50" }
  },
  "datos_nuevos" : {
    "linea" : 1,
    "estadoLinea" : 0,
    "montoOrden" : { "$numberDecimal" : "14962.50" },
    "comprometido" : { "$numberDecimal" : "14962.50" },
    "motivo" : "Proveedor sin existencia de cemento"
  },
  "actor_id" : 6,
  "usuario_afectado_id" : 7,
  "ip_origen" : "::1"
}
```

Hilo en segundo plano (`AuditChannel.FireAndForget` -> `Task.Run`, 1 intento + 3 reintentos): no hubo reintentos, la insercion tuvo exito al primer intento; el log del backend no registra advertencias de auditoria para esta orden.

### Escenario 10 - El DELETE no espera al hilo de auditoria

- PASA - DELETE no espera al hilo de auditoria: Measure-Command: 583.4 ms; la respuesta sale antes de que el hilo Task.Run escriba en MongoDB (el documento real esta en la seccion 9)

```powershell
$t = Measure-Command { Invoke-WebRequest -Method Delete -Uri http://localhost:5000/api/operaciones/ordenes/<id>?fechaEmision=<fecha> -Headers @{Authorization="Bearer <admin>"} -ContentType application/json -Body '{"motivo":"El proveedor no puede surtir"}' }
# HTTP 200 en 583.4 ms
```

El DELETE respondio en 583.4 ms (incluye el UPDATE en SQL Server, la relectura de la orden y del presupuesto, y la ida y vuelta por la red a la VM). La escritura en MongoDB la hizo el hilo `Task.Run` despues de emitir la respuesta: el `fecha_evento` del documento de la seccion 9 es posterior a la `fechaAnulacion` guardada en SQL Server.

### Escenario 11 - Stress test (SqlBulkCopy) sigue funcionando

- PASA - Admin POST /api/seed-data 2000 -> 200: HTTP 200, registrosInsertados=2000, duracionMs=4037, lotes=1

```http
POST /api/seed-data {"cantidad": 2000, "anioInicial": 2025, "anioFinal": 2026}
-> HTTP 200 (4250.0 ms)
```
```json
{
  "registrosInsertados": 2000,
  "duracionMs": 4037,
  "lotes": 1,
  "registrosPorSegundo": 495.4,
  "particiones": "5 filas (ver seccion 11)"
}
```

Particiones reportadas por el endpoint (solo las que tienen filas):

| particion | desde | hasta | filas |
|---|---|---|---|
| 2 | 2024-01-01 | 2025-01-01 | 16630 |
| 3 | 2025-01-01 | 2026-01-01 | 19831 |
| 4 | 2026-01-01 | 2027-01-01 | 19543 |

## 4. Cierre

- API detenido con `Stop-Process` (dotnet y seminario-backend-service.exe); `netstat` confirma que el puerto 5000 quedo libre.
- Las ordenes de prueba quedan en `SeminarioDb` (VM) en estado ANULADA o CREADA con proveedor de prueba: no hay borrado fisico, y son evidencia valida.
- Log completo del backend de esta corrida: `%TEMP%\api-fase2.log`.
- No se modifico codigo ni SQL: ningun escenario fallo por un bug.

## Anexo A. Bodegas del frontend y Soft Delete de documentos (2026-09-29)

Verificacion posterior, contra la misma VM (IP DHCP `192.168.1.180` en esta corrida), despues de aplicar las migraciones `UserDocumentSoftDelete` y `SeedPresupuestoBodegasFrontend`. En la misma sesion del API se repitio la bateria completa de la Fase 2 (secciones 2 y 3) como regresion: 30 de 30 comprobaciones pasaron. Aqui: 14 de 14.

| # | Comprobacion | Resultado | Evidencia |
|---|---|---|---|
| A1 | GET /presupuestos incluye las 5 bodegas del frontend | **PASA** | HTTP 200, bodegas=['Bodega Central Zona 12', 'Bodega Norte Chimaltenango', 'Bodega Regional Quetzaltenango', 'Centro de Distribucion Villa Nueva', 'Cross-dock Puerto Barrios', 'GT-CENTRO', 'GT-NORTE', 'GT-SUR'] |
| A2 | POST con el cuerpo del frontend -> 201 CREADA, 1 linea "Monto global" | **PASA** | HTTP 201, estado=CREADA, monto=1500.75, lineas=['Monto global'], numero=OC-2026-000008 |
| A3 | comprometido de Bodega Central Zona 12 subio el monto de la orden | **PASA** | 0.0 -> 1500.75 |
| A4 | POST documento PDF -> 200 | **PASA** | HTTP 200: {'id': 9, 'fileName': 'rtu-prueba-soft-delete.pdf', 'mimeType': 'application/pdf', 'sizeBytes': 45, 'uploadedAt': '2026-09-29T00:20:43.791341Z', 'url': '/api/users/1/documents/9/content'} |
| A5 | archivo escrito en disco | **PASA** | C:\Users\alder\OneDrive\Desktop\seminario\seminario-backend-service\seminario-backend-service\uploads\1\3a9dae4f7b4d4dddac9372f4bed59430.pdf |
| A6 | GET lista incluye el documento | **PASA** | HTTP 200, ids=[9, 8, 6] |
| A7 | DELETE documento -> 204 | **PASA** | HTTP 204 |
| A8 | GET lista ya no lo incluye | **PASA** | HTTP 200, ids=[8, 6] |
| A9 | GET contenido -> 404 | **PASA** | HTTP 404 |
| A10 | DELETE repetido -> 404 | **PASA** | HTTP 404 |
| A11 | GET /api/users/{id} (Include de Documents) tampoco lo muestra | **PASA** | HTTP 200, documents=[6, 8] |
| A12 | SQL: la fila sigue con IsDeleted = 1 y DeletedByUserId del JWT | **PASA** | [{'Id': 9, 'FileName': 'rtu-prueba-soft-delete.pdf', 'IsDeleted': True, 'DeletedAt': '2026-09-29T00:20:45', 'DeletedByUserId': 1}] |
| A13 | archivo sigue en disco (evidencia) | **PASA** | C:\Users\alder\OneDrive\Desktop\seminario\seminario-backend-service\seminario-backend-service\uploads\1\3a9dae4f7b4d4dddac9372f4bed59430.pdf |
| A14 | SQL directo: DELETE FROM UserDocuments -> 50020 | **PASA** | {'ok': False, 'number': 50020, 'error': 'Borrado fisico prohibido: use Soft Delete'} |

La orden `OC-2026-000008` se creo con el mismo cuerpo que envia `Operaciones.tsx` (sin `lineas`, con `monto` y `estado`): el backend ignora `estado`, la orden nace `CREADA` y el monto entra como una linea "Monto global" que compromete presupuesto en `Bodega Central Zona 12`.

Documento `DOCUMENT_DELETED` leido de `audit_logs` (MongoDB de la VM) despues del Soft Delete del documento 9:

```json
{
  "_id" : { "$oid" : "6abb045ce67f8cf2a85f28e0" },
  "fecha_evento" : { "$date" : "2026-09-29T00:20:44.758Z" },
  "modulo" : "UserDocument",
  "accion" : "DOCUMENT_DELETED",
  "entidad_afectada_id" : "9",
  "jwt_usuario_ejecutor" : "admin",
  "rol_auth" : "Administrador",
  "datos_anteriores" : {
    "FileName" : "rtu-prueba-soft-delete.pdf",
    "ContentType" : "application/pdf",
    "IsDeleted" : false
  },
  "datos_nuevos" : {
    "IsDeleted" : true,
    "DeletedAt" : { "$date" : "2026-09-29T00:20:44.727Z" },
    "DeletedByUserId" : 1
  },
  "actor_id" : 1,
  "usuario_afectado_id" : 1,
  "ip_origen" : "::1"
}
```
