# Línea base de calidad (antes de la intervención SQA)

Esta es la medición del código **tal como estaba al terminar el seminario**, antes de agregar pruebas, pipeline o correcciones.

| Elemento | Commit medido |
|---|---|
| Backend | `0c8d137` — *chore: estado base del backend (Fases 2 y 3 del seminario) antes de la intervención SQA* |
| Frontend | `252e6f0` — integración del frontend al monorepo, sin cambios de código |
| Fecha de medición | 3 de noviembre de 2026 |

## Cómo se midió

| Métrica | Herramienta | Evidencia |
|---|---|---|
| Tamaño | `cloc` 2.0 | Bitácora local, entradas #19, #27 y #29 |
| Duplicación | `jscpd` 4 (mínimo 50 tokens) | `docs/evidencias/linea-base/jscpd/jscpd-report.json`, bitácora #21 y #28 |
| Advertencias de compilador y analizadores .NET | `dotnet build -p:AnalysisLevel=latest-recommended` | Workflow **Linea base de calidad**, run [41774594963](https://github.com/zenvy-crm/seminario-project/actions/runs/41774594963) |
| ESLint, Prettier y TypeScript | `eslint`, `prettier --check`, `tsc --noEmit` | Bitácora GitHub, entradas G27 a G30 |
| Bugs, vulnerabilidades, code smells, deuda técnica | SonarCloud | Workflow **Linea base de calidad**, job `linea-base-sonar` (versión `linea-base` en SonarCloud) |
| Pruebas y cobertura | Conteo de proyectos de prueba | Workflow **Linea base de calidad** |

## Resultados

### Pruebas y cobertura

| Métrica | Backend | Frontend |
|---|---|---|
| Proyectos o archivos de prueba | 0 | 0 |
| Casos de prueba automatizados | 0 | 0 |
| Cobertura de líneas | 0 % | 0 % |
| Pipeline de CI | No existe | No existe |
| Protección de la rama principal | No | No |

### Tamaño y duplicación

| Métrica | Backend (C#, sin migraciones) | Frontend (TS/TSX) |
|---|---|---|
| Archivos | 96 | 43 |
| Líneas de código | 4,379 | 3,556 |
| Clones detectados (jscpd) | 10 | 7 |
| Líneas duplicadas | 134 (2.34 %) | 76 (1.35 %, sobre todo el monorepo) |

Los clones más grandes del backend están entre `ProfileService` y `UserService` (37 líneas en la actualización de datos personales con auditoría).

### Análisis estático del backend (.NET `latest-recommended`)

**56 advertencias únicas** con 0 errores. La compilación pasaba porque las advertencias no bloqueaban nada.

| Regla | Cantidad | Qué significa |
|---|---|---|
| CA1305 | 28 | Conversiones a texto sin cultura explícita (`id.ToString()`, fechas) |
| CA1848 | 6 | Logging sin `LoggerMessage` (boxing y parseo de la plantilla en cada llamada) |
| CA1311 | 6 | `ToLower()` sin cultura |
| CA1304 | 6 | `ToLower()` dependiente de la cultura del servidor |
| CA1862 | 5 | Comparación de texto con `ToLower()`/`ToUpper()` en lugar de `StringComparison` |
| CA1873 | 2 | Argumentos de log evaluados aunque el nivel esté deshabilitado |
| CA1859 | 1 | Parámetro de interfaz donde un tipo concreto evita *virtual dispatch* |
| CA1805 | 1 | Inicialización redundante a su valor por defecto |
| CA1707 | 1 | Guion bajo en el nombre de una migración generada por EF Core |

Cuando se aplicó el estándar de `.editorconfig`, `dotnet format` también encontró un archivo (`Program.cs`) con los `using` desordenados.

### Análisis estático del frontend

| Verificación | Resultado |
|---|---|
| ESLint | **No se podía ejecutar.** `.eslintrc.json` usa el plugin `react-refresh`, pero ese paquete no estaba instalado. |
| ESLint, una vez instalado el plugin | **11 problemas:** 6 errores `no-explicit-any`, 3 imports o parámetros sin uso y 2 dependencias faltantes en `useCallback` |
| Prettier | **38 de 43 archivos** no cumplían el formato del propio `.prettierrc` |
| TypeScript estricto (`tsc --noEmit`) | 0 errores |

### SonarCloud (backend y frontend, sin cobertura)

| Métrica | Valor | Calificación |
|---|---|---|
| Líneas de código analizadas | 10,621 | — |
| Bugs | 3 | Confiabilidad **E** |
| Vulnerabilidades | 4 | Seguridad **C** |
| Code smells | 11 | Mantenibilidad A |
| Deuda técnica | 17 min | — |
| Duplicación | 1.5 % | — |
| Cobertura | 0.0 % | — |

## Diagnóstico

1. **No había red de seguridad.** Ninguna regla de negocio (bloqueo de cuenta, MFA, RBAC por niveles, soft delete, presupuesto con triggers) estaba cubierta por pruebas automatizadas. Los defectos corregidos en el historial de Git (por ejemplo `2fef4cd`, `ad3f3e9` y `80d7e43`) podían volver a aparecer sin que nadie lo notara.
2. **El análisis estático del frontend estaba roto** por una dependencia faltante, así que nadie ejecutaba ESLint.
3. **No se aplicaba ningún estándar de formato.** Prettier estaba configurado, pero casi ningún archivo cumplía el formato.
4. **Sonar reporta bugs y vulnerabilidades** que bajan las calificaciones de confiabilidad (E) y seguridad (C).
5. **Las advertencias no bloqueaban nada:** el código compilaba con 56 advertencias de calidad.

Con base en este diagnóstico se definieron los umbrales del [SQA Plan](SQA-Plan.md#7-métricas-y-umbrales-quality-gates).
