# Fase 3 - Evidencia de verificacion

**Fecha de ejecucion:** 2026-09-29 (UTC), contra la VM de VirtualBox 'ubuntu server' (SQL Server 2022 del stack `db-infra`, base `SeminarioDb`).  
**Como:** el script `sql/04_fase3_big_data.sql` se ejecuto una vez contra la base; la API se levanto en local (`dotnet run --no-build --launch-profile http`, `http://localhost:5000`) apuntando a la misma base y se probaron los tres roles con peticiones HTTP reales.  
**Resultado:** 43 comprobaciones HTTP, 0 fallidas (`results_fase3.json` de la corrida).

## 1. Inyeccion masiva (`sql/04_fase3_big_data.sql`)

Sin `WHILE` ni cursores: una CTE tally (`TOP 250` sobre `master.dbo.spt_values`) genera los encabezados con `OUTPUT inserted.FechaEmision, inserted.Id INTO @Encabezados`, y luego `@Encabezados CROSS JOIN` una segunda CTE tally (`TOP 200`) genera las lineas. Exactamente dos `INSERT ... SELECT` dentro de una transaccion (`SET XACT_ABORT ON`, `TRY/CATCH`, `ROLLBACK`).

| Concepto | Valor medido |
|---|---|
| Lote | 1 (`BD26-L01-000001` ... `BD26-L01-000250`) |
| INSERT 1: 250 encabezados | **613 ms** |
| INSERT 2: 50,000 lineas (con el trigger de presupuesto por conjuntos) | **5,266 ms** |
| Total de la carga | 5,879 ms |
| Encabezados / lineas / monto total | 250 / 50,000 / Q 1,572,723.37 |
| Fechas de emision | 2026-01-05 .. 2026-09-28 (repartidas al azar en el anio fiscal) |

Monto del lote por bodega (lo calculo el trigger `TR_OC_Detalle_Presupuesto`, que tambien comprometio el presupuesto; el disponible de cada bodega sigue por encima de Q 2.1M, asi que se puede re-correr varias veces sin error `50012`):

| Bodega | Ordenes | Monto del lote (GTQ) |
|---|---|---|
| Bodega Norte Chimaltenango | 62 | 388,345.62 |
| Bodega Regional Quetzaltenango | 63 | 399,412.34 |
| Centro de Distribucion Villa Nueva | 63 | 396,676.94 |
| Cross-dock Puerto Barrios | 62 | 388,288.47 |

Despues del `COMMIT` el mismo script hizo el soft delete de prueba: **10 encabezados** en `ANULADA` (con `UsuarioAnulaId = 1`, `FechaAnulacion` y `MotivoAnulacion`) y **50 lineas** con `EstadoLinea = 0` (con `UsuarioBajaId`, `FechaBaja`, `MotivoBaja`). Los triggers liberaron el presupuesto: el comprometido final del 2026 quedo en Q 376,088.96 / 379,824.67 / 377,375.86 / 375,047.63 para las cuatro bodegas.

Nota de la corrida: la primera version del script elegia al proveedor con `CHECKSUM(n.N, @Semilla)`, que no repartia de forma sesgada (8 proveedores casi parejos). Se cambio a un hash `SHA1` de semilla + N (ver `sql/04_fase3_big_data.sql`) y se reasigno el proveedor de las 250 ordenes del lote 1 con la misma expresion (un `UPDATE` de `ProveedorNit` / `ProveedorNombre`, sin tocar montos ni presupuesto). Un `NEWID()` dentro de la condicion del `JOIN` no sirve: SQL Server lo evalua por comparacion y duplica o pierde filas (en la prueba salieron 224, 262 y 286 filas en vez de 250).

## 2. Filas por particion (`sql/05_fase3_evidencia_particiones.sql`)

`PS_RangoFechasOrdenes`, `RANGE RIGHT` con limites 2024-01-01, 2025-01-01, 2026-01-01 y 2027-01-01. Las 50,000 lineas nuevas caen todas en la particion 4 (2026), la misma de sus encabezados.

| Tabla | Particion | Rango | Filas |
|---|---|---|---|
| `TB_Ordenes_Compra` | 1 | -inf .. 2024-01-01 | 0 |
| `TB_Ordenes_Compra` | 2 | 2024-01-01 .. 2025-01-01 | 16,630 |
| `TB_Ordenes_Compra` | 3 | 2025-01-01 .. 2026-01-01 | 16,821 |
| `TB_Ordenes_Compra` | 4 | 2026-01-01 .. 2027-01-01 | 16,801 |
| `TB_Ordenes_Compra` | 5 | 2027-01-01 .. +inf | 0 |
| `TB_Ordenes_Compra_Detalle` | 1 | -inf .. 2024-01-01 | 0 |
| `TB_Ordenes_Compra_Detalle` | 2 | 2024-01-01 .. 2025-01-01 | 0 |
| `TB_Ordenes_Compra_Detalle` | 3 | 2025-01-01 .. 2026-01-01 | 0 |
| `TB_Ordenes_Compra_Detalle` | 4 | 2026-01-01 .. 2027-01-01 | **50,006** (50,000 del lote + 6 previas) |
| `TB_Ordenes_Compra_Detalle` | 5 | 2027-01-01 .. +inf | 0 |

Total de encabezados: 50,252 (50,002 previos + 250 del lote).

## 3. Ranking de proveedores calculado por SQL Server

`dbo.FN_Ranking_Proveedores` (migracion EF `20260929043210_Fase3ReportesRanking`, aplicada con `dotnet ef database update`). `GET /api/reportes/ranking-proveedores?anio=2026&top=20` como `estandar` -> 200, 13 filas (los 12 proveedores de la carga mas uno de las ordenes de la Fase 2). Top 5:

| Pos. | NIT | Proveedor | Total comprometido (GTQ) | Ordenes | Lineas | Participacion |
|---|---|---|---|---|---|---|
| 1 | 2456781-3 | Distribuidora La Fragua, S.A. | 462,827.64 | 74 | 14,785 | 30.16 % |
| 2 | 1087452-6 | Cementos Progreso, S.A. | 213,669.26 | 34 | 6,785 | 13.92 % |
| 3 | 5561238-1 | Ferreteria El Constructor, S.A. | 108,003.49 | 17 | 3,400 | 7.04 % |
| 4 | 6672033-8 | Agroindustria Las Verapaces | 107,669.35 | 17 | 3,400 | 7.02 % |
| 5 | 3345120-9 | Alimentos Kerns y Cia. | 99,045.88 | 16 | 3,195 | 6.45 % |

Comprobado por HTTP: posiciones 1..13 ascendentes, totales descendentes, participaciones que suman 100.00, `top=3` devuelve 3 filas. Solo cuentan lineas vigentes de ordenes no anuladas (por eso los 10 encabezados anulados del lote y las 50 lineas dadas de baja no suman).

## 4. Codigos HTTP observados

| Peticion | Usuario | Esperado | Observado |
|---|---|---|---|
| `GET /api/reportes/ranking-proveedores?anio=2026&top=20` | estandar | 200 | 200 |
| `GET /api/reportes/ranking-proveedores?anio=2026&top=3` | intermedio | 200 | 200 |
| `GET /api/reportes/ranking-proveedores` (defaults) | admin | 200 | 200 |
| `GET /api/reportes/ranking-proveedores?anio=2030` / `top=101` / `top=0` | estandar | 422 | 422 / 422 / 422 |
| `GET /api/reportes/ranking-proveedores` sin token | - | 401 | 401 |
| `GET /api/reportes/ordenes` sin fechas | estandar | 400 | 400 (`Debe indicar el rango de fechas (desde y hasta) para ejecutar el reporte.`) |
| `GET /api/reportes/ordenes?fechaDesde=2026-01-01` (solo una fecha) | estandar | 400 | 400 |
| `GET /api/reportes/ordenes?fechaDesde=2026-03-31&fechaHasta=2026-03-01` | estandar | 400 | 400 |
| `GET /api/reportes/ordenes` con rango de 367 dias | estandar | 400 | 400 |
| `GET /api/reportes/ordenes` con rango de 366 dias (limite) | estandar | 200 | 200 |
| `GET /api/reportes/ordenes?fechaDesde=2026-01-01&fechaHasta=2026-03-31&pageSize=50` | estandar | 200 | 200 (3,171 ordenes en el rango, 50 items) |
| Igual, `estado=ANULADA` (2026-01-01 .. 2026-09-28) | admin | 200 con filas | 200, 2,973 ordenes ANULADA |
| Igual, `estado=ANULADA` | estandar | 200 sin filas | 200, total 0 (el rol Estandar no ve ANULADA) |
| `GET /api/reportes/auditoria/soft-delete` | estandar | 403 | 403 |
| `GET /api/reportes/auditoria/soft-delete` | intermedio | 403 | 403 |
| `GET /api/reportes/auditoria/soft-delete` sin token | - | 401 | 401 |
| `GET /api/reportes/auditoria/soft-delete` (tipo ordenes) | admin | 200 | 200, `resumen` = 11 ordenes / 50 lineas / 1 documento |
| `...?tipo=lineas` | admin | 200 | 200, total 50 |
| `...?tipo=documentos` | admin | 200 | 200, total 1 |
| `...?tipo=xyz` | admin | 422 | 422 |

El resumen de la auditoria (11 ordenes) son las 10 del lote mas una anulacion de la Fase 2; las ~12,000 ordenes `ANULADA` del stress test no aparecen porque no tienen `FechaAnulacion` (no llevan traza). Un item de ejemplo (`tipo=ordenes`, admin):

```json
{
  "tipo": "ORDEN",
  "referencia": "BD26-L01-000001",
  "detalle": "Aceros de Guatemala, S.A.",
  "monto": 6243.72,
  "fechaBaja": "2026-09-29T04:30:03",
  "usuarioBajaId": 1,
  "usuarioBaja": "admin",
  "motivo": "Anulacion de prueba Fase 3 (lote 1)",
  "entidadId": "4f4b092a-bc99-43bd-b918-aa230150ce14"
}
```

Tiempos de respuesta de la API (incluida la red local hacia la VM): ranking top 20 ~250 ms; reporte de ordenes de un trimestre ~50 ms y de todo el anio (pagina 2) ~136 ms; auditoria ~120 ms. El rango de fechas obligatorio deja a SQL Server tocar solo la particion de 2026 (la 4); un trimestre devuelve menos filas que el anio completo.
