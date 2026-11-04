# Evidencias de ejecución

Todos los comandos del proyecto se ejecutaron **uno por uno** y quedaron registrados con fecha y hora (America/Guatemala), código de salida y salida de consola.

| Bitácora | Dónde se ejecutó | Contenido |
|---|---|---|
| [bitacora-comandos.md](bitacora-comandos.md) | Equipo de desarrollo (VM Linux con los repositorios montados) | Entorno, commits del estado base, `cloc`, `jscpd`, empaquetado del historial del backend y del frontend |
| [bitacora-github.md](bitacora-github.md) | Entorno con credenciales de GitHub | Publicación del repositorio, integración del frontend (`git subtree`), pruebas y lint locales del frontend, commits, push, PR y lectura de resultados de GitHub Actions |
| [linea-base/jscpd/](linea-base/jscpd/) | — | Reporte de duplicación de la línea base |

Las ejecuciones del pipeline (build, pruebas, análisis y reportes) son evidencia por sí mismas en la pestaña **Actions** del repositorio:

- Gate de compilación rechazando 55 issues: [run 41777597462](https://github.com/zenvy-crm/seminario-project/actions/runs/41777597462)
- Quality Gate de SonarCloud rechazando el PR (seguridad C y cobertura de código nuevo de 57.3 %): [run 41778220579](https://github.com/zenvy-crm/seminario-project/actions/runs/41778220579)
- Pipeline completo en verde: [run 41779244372](https://github.com/zenvy-crm/seminario-project/actions/runs/41779244372)
- Línea base: workflow [Linea base de calidad](https://github.com/zenvy-crm/seminario-project/actions/workflows/linea-base.yml)
