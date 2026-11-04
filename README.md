# Portal de Proveedores: pipeline CI/CD con aseguramiento de calidad integrado

[![CI](https://github.com/zenvy-crm/seminario-project/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/zenvy-crm/seminario-project/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=zenvy-crm_seminario-project)
[![Cobertura](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=coverage)](https://sonarcloud.io/component_measures?id=zenvy-crm_seminario-project&metric=coverage)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=bugs)](https://sonarcloud.io/project/issues?id=zenvy-crm_seminario-project&types=BUG)
[![Vulnerabilidades](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=vulnerabilities)](https://sonarcloud.io/project/issues?id=zenvy-crm_seminario-project&types=VULNERABILITY)
[![Code smells](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=code_smells)](https://sonarcloud.io/project/issues?id=zenvy-crm_seminario-project&types=CODE_SMELL)
[![Deuda técnica](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=sqale_index)](https://sonarcloud.io/summary/overall?id=zenvy-crm_seminario-project)
[![Duplicación](https://sonarcloud.io/api/project_badges/measure?project=zenvy-crm_seminario-project&metric=duplicated_lines_density)](https://sonarcloud.io/component_measures?id=zenvy-crm_seminario-project&metric=duplicated_lines_density)

Proyecto final del curso **Aseguramiento de la Calidad del Software (SQA) 2026**. Sobre una aplicación full-stack existente (el portal de proveedores del curso *Seminario de Tecnologías de Información*) se implementó un pipeline de Integración Continua en el que **todo cambio pasa por pruebas automatizadas, análisis estático y quality gates antes de llegar a `main`**.

## La aplicación

Portal de proveedores con:

- autenticación JWT + MFA, bloqueo de cuenta y *rate limiting*;
- usuarios con RBAC de 3 niveles (Estándar, Intermedio, Administrador) y documentos PDF/JPEG validados por firma;
- órdenes de compra sobre una tabla particionada, con presupuesto controlado por SP y triggers y anulación por *soft delete*;
- reportes (ranking con `RANK()`, reporte transaccional, auditoría de bajas) y auditoría asíncrona en MongoDB.

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core 8, EF Core 8, SQL Server 2022, MongoDB 7 |
| Frontend | React 18, TypeScript, Vite, MUI |
| Pruebas | xUnit, Moq, Testcontainers, `WebApplicationFactory`, Vitest, Testing Library |
| Cobertura | Coverlet, V8, ReportGenerator |
| Análisis estático | Analizadores .NET (`latest-recommended`), `dotnet format`, ESLint, Prettier, SonarCloud |
| CI/CD | GitHub Actions |
| Contenedores | Docker (imagen de la API + contenedores efímeros para las pruebas) |

El enunciado sugiere Node + Express + MySQL. La justificación del stack alternativo está en el [SQA Plan §2.3](docs/SQA-Plan.md#23-justificación-del-stack).

## Pipeline y quality gates

```
1 · BUILD     backend (0 advertencias) · frontend (tsc + vite) · imagen Docker
2 · TEST      195 pruebas: unitarias + integración (SQL Server/MongoDB reales) + regresión + frontend
3 · ANALYZE   dotnet format · ESLint (--max-warnings 0) · Prettier · SonarCloud Quality Gate
4 · REPORT    cobertura combinada · gate backend ≥ 60 % · resumen del run · reporte HTML
```

| Métrica | Antes | Después |
|---|---|---|
| Pruebas automatizadas | 0 | 195 |
| Cobertura backend (líneas) | 0 % | 90.7 % |
| Cobertura frontend (alcance del plan) | 0 % | 96.4 % |
| Bugs / vulnerabilidades (SonarCloud) | 3 / 4 | 0 / 0 |
| Confiabilidad / seguridad | E / C | A / A |
| Advertencias de analizadores .NET | 56 | 0 |

Detalle completo en el [Reporte de mejora](docs/Reporte-Mejora.md).

## Instalación y ejecución

### Requisitos

- .NET SDK 8
- Node.js 20 y pnpm 9
- Docker (para SQL Server y MongoDB locales y para las pruebas de integración)

### Backend

```bash
cp seminario-backend-service/.env.example seminario-backend-service/.env   # completar credenciales
cd seminario-backend-service
dotnet run                     # http://localhost:5000, Swagger en /swagger
```

Al arrancar se aplican las migraciones y se crean los roles y el usuario `admin` / `Admin#2026!`. En `Development` también se crean `intermedio` / `Intermedio#2026!` y `estandar` / `Estandar#2026!`.

### Frontend

```bash
cd frontend
pnpm install
echo "VITE_API_URL=http://localhost:5000/api" > .env.local
pnpm dev                       # http://localhost:5173
```

### Pruebas y calidad (lo mismo que ejecuta el pipeline)

```bash
dotnet build seminario.sln -c Release -p:ContinuousIntegrationBuild=true
dotnet test seminario.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"
dotnet format seminario.sln --verify-no-changes
cd frontend && pnpm lint --max-warnings 0 && pnpm format:check && pnpm test:coverage
```

## Flujo de trabajo

1. Crear una rama desde `main` (`feat/…`, `fix/…`, `test/…`).
2. Hacer el cambio **con sus pruebas**.
3. Abrir un pull request: el pipeline corre solo.
4. Si un gate falla, el PR queda bloqueado. Se corrige en la misma rama.
5. Con todos los checks en verde, se integra a `main`.

## Documentación

| Documento | Contenido |
|---|---|
| [docs/SQA-Plan.md](docs/SQA-Plan.md) | Propósito, alcance, estándares, actividades, métricas y umbrales, herramientas, riesgos |
| [docs/Matriz-Pruebas.md](docs/Matriz-Pruebas.md) | 169 casos con ID, tipo, entrada, resultado esperado y resultado obtenido |
| [docs/Linea-Base-Calidad.md](docs/Linea-Base-Calidad.md) | Métricas del código antes de la intervención |
| [docs/Reporte-Mejora.md](docs/Reporte-Mejora.md) | Comparativa antes/después, issues corregidos y deuda técnica pendiente |
| [docs/Manual-Tecnico.md](docs/Manual-Tecnico.md) | Arquitectura del pipeline, decisiones técnicas, mantenimiento y solución de problemas |
| [docs/README-backend.md](docs/README-backend.md) | Documentación funcional del backend (endpoints, SP, triggers, despliegue) |
| [docs/evidencias/](docs/evidencias/) | Bitácora de cada comando ejecutado durante el proyecto |

## Autor

Alder Solis · Universidad Mariano Gálvez de Guatemala · Aseguramiento de la Calidad del Software, 2026.
