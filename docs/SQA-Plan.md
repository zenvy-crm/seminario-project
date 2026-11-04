# Plan de Aseguramiento de la Calidad del Software (SQA Plan)

| Campo | Valor |
|---|---|
| Proyecto | Portal de Proveedores — módulo de identidad, seguridad, auditoría, órdenes de compra y reportes |
| Repositorio | https://github.com/zenvy-crm/seminario-project |
| Curso | Aseguramiento de la Calidad del Software (SQA) — Proyecto Final 2026 |
| Responsable | Alder Solis |
| Versión del plan | 1.0 (noviembre 2026) |
| Estructura de referencia | IEEE Std 730-2014 (*Software Quality Assurance Processes*) e ISO/IEC 25010 (modelo de calidad) |

---

## 1. Propósito

Definir las actividades, estándares, métricas, herramientas y responsabilidades que garantizan que todo cambio al sistema pase por verificaciones de calidad **automatizadas y objetivas** antes de integrarse a la rama principal (`main`). El plan se aplica al código existente del seminario (no a un sistema nuevo) y busca:

1. Detectar defectos antes de que lleguen a `main` (pruebas automatizadas + quality gates).
2. Hacer visible y medible la deuda técnica (análisis estático + SonarCloud).
3. Evitar que defectos ya corregidos vuelvan a aparecer (pruebas de regresión).
4. Medir el impacto de la intervención SQA comparando la línea base contra el estado final.

## 2. Alcance

### 2.1 Elementos de configuración bajo el plan

| Componente | Tecnología | Ubicación |
|---|---|---|
| API REST (backend) | ASP.NET Core 8, EF Core 8, SQL Server 2022, MongoDB 7 | `seminario-backend-service/` |
| SPA (frontend) | React 18 + TypeScript + Vite + MUI | `frontend/` |
| Base de datos | Migraciones EF Core + SQL nativo (particiones, SP, triggers) | `Infrastructure/Persistence/Migrations`, `sql/` |
| Pruebas | xUnit, Moq, Testcontainers, Vitest, Testing Library | `tests/`, `frontend/src/**/*.test.*` |
| Pipeline | GitHub Actions | `.github/workflows/ci.yml` |

### 2.2 Fuera de alcance

- Pruebas de carga, estrés y rendimiento (excluidas por el enunciado).
- Desarrollo de funcionalidades nuevas: la intervención solo agrega pruebas, configuración y correcciones de calidad.
- Pruebas de las páginas de React con lógica de presentación extensa (ver §7.3, se registran como deuda técnica).

### 2.3 Justificación del stack

El enunciado sugiere Node.js + Express + MySQL, pero permite variantes justificadas. Se reutiliza el proyecto del curso *Seminario de Tecnologías de Información* (ASP.NET Core + SQL Server + MongoDB + React) porque ya contiene un CRUD completo, reglas de negocio no triviales (bloqueo de cuentas, MFA, RBAC por niveles, soft delete, presupuesto con triggers) y defectos históricos documentados en Git que sirven como casos de regresión reales. Los conceptos SQA evaluados se cubren con herramientas equivalentes:

| Concepto SQA | Sugerido | Utilizado |
|---|---|---|
| Pruebas unitarias | Jest | xUnit + Moq (backend), Vitest (frontend) |
| Pruebas de API / integración | Supertest | `WebApplicationFactory` + Testcontainers (SQL Server y MongoDB reales) |
| Componentes de UI | React Testing Library | React Testing Library (sobre Vitest) |
| Cobertura | Istanbul / nyc | Coverlet (backend), V8 (frontend), ReportGenerator (consolidado) |
| Análisis estático | ESLint + Prettier + SonarCloud | Analizadores .NET (`latest-recommended`) + `dotnet format` + ESLint + Prettier + SonarCloud |
| CI/CD | GitHub Actions | GitHub Actions |
| Contenedores | Docker (opcional) | Docker: imagen de la API construida en cada pipeline + contenedores efímeros para pruebas |

## 3. Referencias

- IEEE Std 730-2014, *Software Quality Assurance Processes*.
- ISO/IEC 25010:2011, *Systems and software Quality Requirements and Evaluation (SQuaRE)*.
- Enunciado *Proyecto Final 2026 — Pipeline CI/CD con Aseguramiento de Calidad Integrado*.
- Documentación de SonarCloud: *Quality Gates* ("Sonar way").

## 4. Estándares y convenciones

| Ámbito | Estándar | Cómo se verifica |
|---|---|---|
| Estilo C# | `.editorconfig` (namespaces de archivo, llaves Allman, `_camelCase` en campos privados, `PascalCase` en constantes) | `dotnet format --verify-no-changes` en el pipeline y `EnforceCodeStyleInBuild` |
| Calidad C# | Reglas de análisis de .NET `latest-recommended` (globalización, rendimiento, confiabilidad, seguridad) | Compilación con `TreatWarningsAsErrors` en CI: **0 advertencias** |
| Estilo TS/React | `.eslintrc.json` (`eslint:recommended`, `@typescript-eslint/recommended`, `react`, `react-hooks`) | `pnpm lint --max-warnings 0` |
| Formato TS/React | `.prettierrc` | `pnpm format:check` |
| Tipado | TypeScript `strict` | `tsc -b` en el build |
| Mensajes de commit | Conventional Commits (`feat:`, `fix:`, `test:`, `ci:`, `docs:`, `refactor:`, `chore:`) | Revisión en el PR |
| Nombres de pruebas | ID trazable (`UT-…`, `IT-…`, `FE-…`) en `DisplayName` / descripción | Matriz de pruebas |
| Ramas | `main` protegida; trabajo en ramas `tipo/descripcion`; integración solo por pull request | Reglas de protección de GitHub |

## 5. Organización y responsabilidades

| Rol | Responsable | Responsabilidades |
|---|---|---|
| Desarrollador | Alder Solis | Escribe código y pruebas, corrige los issues reportados, mantiene la documentación |
| Revisor | Alder Solis + revisión automática (pipeline) | Aprueba el PR solo si todos los checks están en verde |
| Pipeline (GitHub Actions) | Automático | Ejecuta build, pruebas, análisis y reportes; bloquea el merge si falla un gate |
| SonarCloud | Automático | Evalúa el Quality Gate sobre el código nuevo y publica la deuda técnica |

## 6. Actividades SQA

| # | Actividad | Momento | Herramienta | Evidencia |
|---|---|---|---|---|
| A1 | Medición de la línea base | Antes de la intervención | Workflow `linea-base.yml`, jscpd, cloc, SonarCloud | `docs/Linea-Base-Calidad.md` |
| A2 | Diseño de casos de prueba | Fase 2 | Matriz de pruebas | `docs/Matriz-Pruebas.md` |
| A3 | Pruebas unitarias | Cada push / PR | xUnit, Moq, Vitest | Job `2 · Pruebas …` |
| A4 | Pruebas de integración | Cada push / PR | Testcontainers (SQL Server + MongoDB) | Job `2 · Pruebas backend` |
| A5 | Pruebas de regresión | Cada push / PR | Casos `Category=Regression` | Matriz (columna *Regresión*) |
| A6 | Análisis estático | Cada push / PR | Analizadores .NET, `dotnet format`, ESLint, Prettier | Job `3 · Análisis estático` |
| A7 | Revisión automática de código | Cada PR | SonarCloud (Quality Gate) | Job `3 · SonarCloud`, comentario en el PR |
| A8 | Revisión manual | Cada PR | Pull request en GitHub | Conversación del PR |
| A9 | Medición de cobertura | Cada push / PR | Coverlet, V8, ReportGenerator | Job `4 · Reporte`, artefacto `reporte-calidad` |
| A10 | Gestión de deuda técnica | Cada análisis | SonarCloud (*Technical Debt*), registro en el reporte de mejora | `docs/Reporte-Mejora.md` |
| A11 | Comparativa antes/después | Fase 4 | Métricas A1 vs. estado final | `docs/Reporte-Mejora.md` |

### 6.1 Flujo de trabajo

1. Crear una rama desde `main` (`feat/…`, `fix/…`).
2. Implementar el cambio **y** sus pruebas.
3. Abrir un pull request hacia `main`.
4. El pipeline ejecuta `build → test → analyze → report`.
5. Si cualquier gate falla, el PR queda bloqueado; se corrige en la misma rama.
6. Con todos los checks en verde y la revisión aprobada, se integra a `main`.

## 7. Métricas y umbrales (quality gates)

### 7.1 Gates que bloquean el merge

| Gate | Métrica | Umbral | Dónde se evalúa |
|---|---|---|---|
| G1 | Compilación backend con analizadores | 0 errores, **0 advertencias** | Job `1 · Build backend` |
| G2 | Compilación frontend (`tsc -b` + `vite build`) | 0 errores | Job `1 · Build frontend` |
| G3 | Imagen Docker de la API | Construye correctamente | Job `1 · Build imagen Docker` |
| G4 | Pruebas backend (unitarias + integración + regresión) | 100 % aprobadas | Job `2 · Pruebas backend` |
| G5 | Pruebas frontend | 100 % aprobadas | Job `2 · Pruebas frontend` |
| G6 | Cobertura de líneas del backend | **≥ 60 %** | Job `4 · Reporte` (ReportGenerator) |
| G7 | Cobertura del frontend (líneas, ramas, funciones, sentencias) en el alcance §7.3 | **≥ 60 %** | `vitest.config.ts` (`thresholds`) |
| G8 | Formato C# | 0 diferencias con `.editorconfig` | `dotnet format --verify-no-changes` |
| G9 | ESLint | 0 errores y 0 advertencias | `pnpm lint --max-warnings 0` |
| G10 | Prettier | 0 archivos sin formato | `pnpm format:check` |
| G11 | SonarCloud Quality Gate ("Sonar way", código nuevo) | Cobertura ≥ 80 %, duplicación ≤ 3 %, calificaciones A en confiabilidad/seguridad/mantenibilidad, 100 % de *hotspots* revisados | Job `3 · SonarCloud` (`sonar.qualitygate.wait=true`) |

### 7.2 Métricas de seguimiento (no bloqueantes)

Líneas de código, bugs, vulnerabilidades, code smells, *security hotspots*, duplicación, deuda técnica (minutos), cobertura de ramas, número de casos de prueba y tiempo de ejecución del pipeline.

### 7.3 Alcance de la cobertura

- **Backend:** todo el ensamblado `seminario-backend-service` excepto las migraciones generadas por EF Core (`Infrastructure/Persistence/Migrations`), que son código producido por la herramienta.
- **Frontend:** capa de acceso a la API (`src/api`), autenticación y rutas protegidas (`src/auth`) y componentes comunes (`src/components/common`). Las páginas (`src/pages`) se validan indirectamente por las pruebas de integración de la API y por pruebas manuales; su cobertura automatizada queda registrada como deuda técnica.

## 8. Herramientas

| Herramienta | Versión | Uso |
|---|---|---|
| GitHub Actions | — | Orquestación del pipeline |
| .NET SDK | 8.0 | Build, pruebas, analizadores, `dotnet format` |
| xUnit / Moq | 2.9 / 4.20 | Pruebas unitarias backend |
| Microsoft.AspNetCore.Mvc.Testing | 8.0 | Host de la API en pruebas de integración |
| Testcontainers | 3.10 | SQL Server 2022 y MongoDB 7 efímeros |
| EF Core InMemory | 8.0 | Pruebas unitarias de servicios con `DbContext` |
| Coverlet / ReportGenerator | 6.0 / 5.x | Cobertura backend y reporte consolidado |
| Vitest / Testing Library / V8 | 2.1 / 16.1 | Pruebas y cobertura frontend |
| ESLint / Prettier | 8.57 / 3.x | Análisis estático y formato frontend |
| SonarCloud | — | Revisión automática, Quality Gate y deuda técnica |
| Docker | — | Imagen de la API y contenedores de prueba |
| jscpd / cloc | 4 / 2.0 | Duplicación y tamaño en la línea base |

## 9. Gestión de defectos y de la deuda técnica

1. Todo issue detectado (pipeline, SonarCloud o revisión) se corrige en la misma rama antes del merge cuando es **bloqueante** (bug, vulnerabilidad, *hotspot*, gate incumplido).
2. Los issues no bloqueantes (code smells menores) se registran en `docs/Reporte-Mejora.md` con su severidad y la decisión tomada.
3. Cada defecto corregido debe acompañarse de una prueba de regresión (`Category=Regression` o prefijo `FE-…` con la palabra *regresión*).
4. La deuda técnica se mide en minutos (índice SQALE de SonarCloud) y se compara contra la línea base.

## 10. Gestión de riesgos

| # | Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|---|
| R1 | Las pruebas de integración dependen de Docker y pueden ser lentas o inestables | Media | Medio | Contenedores compartidos por colección (una sola instancia por ejecución); imágenes fijas (`mssql/server:2022-latest`, `mongo:7.0`) |
| R2 | El código de negocio vive en SQL Server (SP y triggers) y no se puede probar con una base en memoria | Alta | Alto | Las pruebas de órdenes y reportes se ejecutan contra SQL Server real con las mismas migraciones de producción |
| R3 | El código MFA solo se emite por log en desarrollo | Alta | Medio | Las pruebas capturan el código con un `ILoggerProvider` de prueba, sin modificar la lógica de producción |
| R4 | El rate limiting (5 logins/min por IP) interfiere con las pruebas | Alta | Medio | Cada cliente de prueba usa una IP distinta (cabecera `X-Test-IP` solo en el host de pruebas) |
| R5 | Fuga de secretos en el repositorio o en el pipeline | Baja | Alto | `.env`/`api.env` en `.gitignore`, verificación antes del primer push, `SONAR_TOKEN` como *secret* de GitHub, credenciales de prueba efímeras |
| R6 | Ajustar umbrales para "pasar" el gate en lugar de corregir | Media | Alto | Umbrales fijados en este plan antes de medir; cualquier cambio requiere actualizar el plan y justificarlo en el PR |
| R7 | El frontend tiene páginas grandes sin pruebas | Alta | Medio | Alcance de cobertura explícito (§7.3) y registro como deuda técnica |

## 11. Registros

- Bitácora de comandos ejecutados: `docs/evidencias/bitacora-comandos.md` y `docs/evidencias/bitacora-github.md`.
- Ejecuciones del pipeline: pestaña *Actions* del repositorio.
- Reportes de cobertura: artefacto `reporte-calidad` de cada ejecución.
- Historial de calidad: proyecto `zenvy-crm_seminario-project` en SonarCloud.
