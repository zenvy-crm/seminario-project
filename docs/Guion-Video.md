# Guion del video (4 a 5 minutos)

Grabar con OBS o Loom a 1080p o 720p como mínimo, con narración. Tener abiertos de antemano el repositorio, la pestaña Actions y SonarCloud.

| # | Bloque | Tiempo | Qué mostrar | Qué decir |
|---|---|---|---|---|
| 1 | Introducción | 0:00–0:40 | Portada de la presentación y el README con los badges | Nombre, curso y proyecto. Objetivo: que ningún cambio llegue a `main` sin pruebas, análisis estático y quality gates. Se reutilizó el portal de proveedores del Seminario (ASP.NET Core + React + SQL Server + MongoDB). |
| 2 | Demostración del pipeline | 0:40–2:40 | Terminal y GitHub | Ver el detalle abajo |
| 3 | Dashboard de calidad | 2:40–3:35 | SonarCloud → *Summary* | Quality Gate *Passed*; cobertura 84.5 %; 0 bugs, 0 vulnerabilidades, 0 code smells; duplicación 0.7 %; calificaciones A/A/A. |
| 4 | Comparativa | 3:35–4:15 | Diapositiva 5 o `docs/Reporte-Mejora.md` | Antes: 0 pruebas, 56 advertencias, 3 bugs, 4 vulnerabilidades, confiabilidad E. Después: 195 pruebas, 90.7 % de cobertura en el backend, 0 issues, A/A/A. |
| 5 | Conclusiones | 4:15–4:55 | Diapositiva 8 | Probar contra SQL Server real; el pipeline también tiene vulnerabilidades; un gate visible hace que se corrijan advertencias que existían desde antes; cómo se resolvió lo difícil de probar (MFA y rate limiting). |

## Detalle del bloque 2 (demostración, unos 2 min)

1. `git checkout -b demo/regla-rota` y hacer un cambio que rompa una regla de negocio. Por ejemplo, en `AuthService.cs` cambiar `MaxFailedLoginAttempts = 3` por `5`.
2. `git commit -am "demo: cambiar intentos de bloqueo"` y `git push -u origin demo/regla-rota`.
3. Abrir el pull request en GitHub y mostrar los 8 checks arrancando.
4. Mientras corre: build en paralelo; pruebas con SQL Server y MongoDB en contenedores.
5. Resultado: **`2 · Pruebas backend` en rojo**. Abrir el log y mostrar que fallaron `UT-AUTH-11` e `IT-AUTH-03` (las pruebas de regresión del bloqueo). El botón *Merge* queda bloqueado por la protección de la rama.
6. Revertir el cambio (`git revert HEAD` y push): los checks pasan a verde y el Quality Gate de SonarCloud aparece como *Passed* en el PR.

Si el tiempo no alcanza para esperar el pipeline en vivo, prepararlo antes y mostrar el run terminado. También se pueden usar los runs ya grabados: [41777597462](https://github.com/zenvy-crm/seminario-project/actions/runs/41777597462) (gate de compilación rechazando) y [41780174068](https://github.com/zenvy-crm/seminario-project/actions/runs/41780174068) (todo en verde).
