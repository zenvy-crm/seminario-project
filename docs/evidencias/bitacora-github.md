# Bitácora de comandos — GitHub (publicación, Actions, protección de rama)

Comandos ejecutados desde el entorno con credenciales de GitHub, uno por uno.

### G1 — Fase 1 · Publicación — clonar el repositorio remoto (vacío)
- Fecha/hora: 2026-11-03 22:35:56 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git clone https://github.com/zenvy-crm/seminario-project /home/claude/seminario-project
```
```text
Cloning into '/home/claude/seminario-project'...
warning: You appear to have cloned an empty repository.
```

### G2 — Fase 1 · Publicación — traer el historial local desde el bundle
- Fecha/hora: 2026-11-03 22:36:01 (America/Guatemala)
- Codigo de salida: 128
```bash
$ git fetch /mnt/user-data/uploads/seminario-backend-service/docs/evidencias/.tmp-repo.bundle main:main
```
```text
fatal: refusing to fetch into branch 'refs/heads/main' checked out at '/home/claude/seminario-project'
```

### G3 — Fase 1 · Publicación — traer el historial local desde el bundle
- Fecha/hora: 2026-11-03 22:36:04 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git pull -q /mnt/user-data/uploads/seminario-backend-service/docs/evidencias/.tmp-repo.bundle main && git log --oneline -3
```
```text
0c8d137 chore: estado base del backend (Fases 2 y 3 del seminario) antes de la intervencion SQA
12949c8 chore: agregar config de launch para levantar zenvy-core en preview
4655183 docs: documentar validaciones de documentos y limitacion conocida de PDF
```

### G4 — Fase 1 · Publicación — push inicial a main
- Fecha/hora: 2026-11-03 22:36:15 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git push -u origin main
```
```text
fatal: expected 'acknowledgments', received 'packfile'
warning: push negotiation failed; proceeding anyway with push
To https://github.com/zenvy-crm/seminario-project
 * [new branch]      main -> main
branch 'main' set up to track 'origin/main'.
```

### G5 — Fase 1 · Publicación — verificar repo remoto
- Fecha/hora: 2026-11-03 22:36:20 (America/Guatemala)
- Codigo de salida: 1
```bash
$ gh repo view zenvy-crm/seminario-project --json name,visibility,defaultBranchRef,url
```
```text
HTTP 403: GitHub GraphQL is not available from Claude Code sessions; use the REST API (gh api repos/{owner}/{repo}/...). For review threads, auto-merge, and draft/ready-for-review use the CCR routes on api.github.com: GET /repos/{owner}/{repo}/pulls/{n}/ccr/review_threads, POST /repos/{owner}/{repo}/pulls/{n}/ccr/comments/{comment_id}/resolve (or /unresolve), PUT or DELETE /repos/{owner}/{repo}/pulls/{n}/ccr/auto_merge, POST /repos/{owner}/{repo}/pulls/{n}/ccr/ready_for_review, POST /repos/{owner}/{repo}/pulls/{n}/ccr/convert_to_draft. (https://api.github.com/graphql)
```

### G6 — Fase 1 · Publicación — verificar repo remoto
- Fecha/hora: 2026-11-03 22:36:23 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api repos/zenvy-crm/seminario-project --jq '{name: .full_name, visibility, default_branch, url: .html_url}'
```
```text
{"default_branch":"main","name":"zenvy-crm/seminario-project","url":"https://github.com/zenvy-crm/seminario-project","visibility":"public"}
```

### G7 — Fase 1 · Línea base — rama de trabajo
- Fecha/hora: 2026-11-03 22:39:52 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git checkout -b sqa/linea-base
```
```text
Switched to a new branch 'sqa/linea-base'
```

### G8 — Fase 1 · Línea base — volver a main (workflow_dispatch exige que el workflow exista en la rama por defecto)
- Fecha/hora: 2026-11-03 22:39:57 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git checkout main && git branch -d sqa/linea-base
```
```text
Switched to branch 'main'
M	docs/evidencias/bitacora-comandos.md
Your branch is up to date with 'origin/main'.
Deleted branch sqa/linea-base (was 0c8d137).
```

### G9 — Fase 1 · Línea base — agregar workflow y evidencias
- Fecha/hora: 2026-11-03 22:39:57 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .github/workflows/linea-base.yml docs/evidencias && git status --short
```
```text
A  .github/workflows/linea-base.yml
M  docs/evidencias/bitacora-comandos.md
A  docs/evidencias/bitacora-github.md
A  docs/evidencias/linea-base/jscpd/jscpd-report.json
```

### G10 — Fase 1 · Línea base — commit
- Fecha/hora: 2026-11-03 22:40:03 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: workflow de medicion de linea base de calidad + evidencias iniciales' && git log --oneline -1
```
```text
8bde041 ci: workflow de medicion de linea base de calidad + evidencias iniciales
```

### G11 — Fase 1 · Línea base — push
- Fecha/hora: 2026-11-03 22:40:03 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git push origin main 2>&1 | grep -v 'acknowledgments\|negotiation'
```
```text
To https://github.com/zenvy-crm/seminario-project
   0c8d137..8bde041  main -> main
```

### G12 — Fase 1 · Línea base — disparar workflow sobre el commit base 0c8d137
- Fecha/hora: 2026-11-03 22:40:10 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api -X POST repos/zenvy-crm/seminario-project/actions/workflows/linea-base.yml/dispatches -f ref=main -f 'inputs[ref]=0c8d13704a0de754e7ef9de7e7dc72d2bdf3c825' && echo disparado
```
```text
disparado
```

### G13 — Fase 1 · Línea base — descargar reportes del run 41774594963
- Fecha/hora: 2026-11-03 22:41:30 (America/Guatemala)
- Codigo de salida: 1
```bash
$ rm -rf /tmp/lb && gh run download 41774594963 -R zenvy-crm/seminario-project -n linea-base -D /tmp/lb && ls -la /tmp/lb && cat /tmp/lb/advertencias-por-regla.md && wc -l /tmp/lb/advertencias-unicas.txt && jq '[.[].FilePath] | unique | length' /tmp/lb/format-report.json && jq '[.[].FileChanges[]] | length' /tmp/lb/format-report.json
```
```text
error downloading linea-base: Get "https://productionresultssa17.blob.core.windows.net/actions-results/0b4403ec-cbac-407b-b5b9-126e46f0957d/workflow-job-run-fbd4f6e7-78ea-5683-88e5-0c386a7a5634/artifacts/0798342034981039eb0f03c6824d8ccf5cad04576b1af606d6a378ae645a3fea.zip?rscd=attachment%3B+filename%3D%22linea-base.zip%22&rsct=application%2Fzip&se=2026-11-04T04%3A51%3A30Z&sig=GhHsKqCWWZQ4eRi%2BFaJ%2FdDhN%2F6egNSRpgJmrsfGgY8c%3D&ske=2026-11-04T07%3A22%3A46Z&skoid=ca7593d4-ee42-46cd-af88-8b886a2f84eb&sks=b&skt=2026-11-04T03%3A22%3A46Z&sktid=398a6654-997b-47e9-b12b-9515b896b4de&skv=2025-11-05&sp=r&spr=https&sr=b&st=2026-11-04T04%3A41%3A25Z&sv=2025-11-05": Forbidden
```

### G14 — Fase 1 · Línea base — leer resultados del log del job 126391909558
- Fecha/hora: 2026-11-03 22:41:40 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api repos/zenvy-crm/seminario-project/actions/jobs/126391909558/logs > /tmp/job.log && grep -E 'TOTAL_ADVERTENCIAS|Build succeeded|Warning\(s\)|Error\(s\)|Formatted code file|FORMAT|error WHITESPACE|warning [A-Z]+[0-9]+' /tmp/job.log | sed 's/^[0-9TZ:.-]* //' | grep -oE 'TOTAL_ADVERTENCIAS=[0-9]+|[0-9]+ Warning\(s\)|[0-9]+ Error\(s\)|warning [A-Z]+[0-9]+|error [A-Z]+[0-9]*' | sort | uniq -c | sort -rn | head -40
```
```text
     56 warning CA1305
     12 warning CA1848
     12 warning CA1311
     12 warning CA1304
     10 warning CA1862
      4 warning CA1873
      2 warning CA1859
      2 warning CA1805
      2 warning CA1707
      1 TOTAL_ADVERTENCIAS=56
      1 56 Warning(s)
      1 0 Error(s)
```

### G15 — Fase 1 · Línea base — advertencias únicas por regla
- Fecha/hora: 2026-11-03 22:41:44 (America/Guatemala)
- Codigo de salida: 0
```bash
$ grep -oE '[^ ]+\.cs\([0-9]+,[0-9]+\): warning [A-Z]+[0-9]+' /tmp/job.log | sed 's#.*/seminario-backend-service/##' | sort -u | tee /tmp/adv-unicas.txt | grep -oE 'warning [A-Z]+[0-9]+' | sort | uniq -c | sort -rn; echo TOTAL=$(wc -l < /tmp/adv-unicas.txt)
```
```text
     28 warning CA1305
      6 warning CA1848
      6 warning CA1311
      6 warning CA1304
      5 warning CA1862
      2 warning CA1873
      1 warning CA1859
      1 warning CA1805
      1 warning CA1707
TOTAL=56
```

### G16 — Fase 1 · Línea base — resultado de dotnet format
- Fecha/hora: 2026-11-03 22:41:48 (America/Guatemala)
- Codigo de salida: 0
```bash
$ grep -E 'FORMAT_RC|Archivos con problemas|format-report|Formatted|warning IDE|error IDE|error WHITESPACE|error FINALNEWLINE|error CHARSET|error IMPORTS' /tmp/job.log | sed 's/^[0-9TZ:.-]* //' | grep -oE '(error|warning) [A-Z]+[0-9]*' | sort | uniq -c | sort -rn | head; grep -c 'error WHITESPACE' /tmp/job.log; grep -E 'Proyectos de prueba' /tmp/job.log | tail -1 | sed 's/^[0-9TZ:.-]* //'
```
```text
0
[36;1mecho "Proyectos de prueba: **$N** · cobertura: **0 %** (no hay pruebas que ejecutar)" >> "$GITHUB_STEP_SUMMARY"[0m
```

### G17 — Fase 3 · SonarCloud — verificar que existe el secreto SONAR_TOKEN
- Fecha/hora: 2026-11-03 22:42:26 (America/Guatemala)
- Codigo de salida: 1
```bash
$ gh api repos/zenvy-crm/seminario-project/actions/secrets --jq '.secrets[] | {name, created_at}'
```
```text
{"message":"Access to this GitHub Actions path is not permitted through this proxy.","documentation_url":"https://docs.anthropic.com/en/docs/claude-code/github-actions"}gh: Access to this GitHub Actions path is not permitted through this proxy. (HTTP 403)
```

### G18 — Fase 1 · Monorepo — integrar el frontend con su historial (git subtree en /frontend)
- Fecha/hora: 2026-11-03 22:43:50 (America/Guatemala)
- Codigo de salida: 1
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' subtree add --prefix=frontend /tmp/frontend.bundle HEAD -m 'chore: integrar frontend (React + Vite) al monorepo con su historial' && git log --oneline -3 && ls frontend
```
```text
fatal: working tree has modifications.  Cannot add.
```

### G19 — Fase 1 · Monorepo — integrar el frontend con su historial (git subtree en /frontend)
- Fecha/hora: 2026-11-03 22:43:57 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' subtree add --prefix=frontend /tmp/frontend.bundle HEAD -m 'chore: integrar frontend (React + Vite) al monorepo con su historial' && git log --oneline -3 && ls frontend
```
```text
git fetch /tmp/frontend.bundle HEAD
From /tmp/frontend.bundle
 * branch            HEAD       -> FETCH_HEAD
Added dir 'frontend'
826501b chore: integrar frontend (React + Vite) al monorepo con su historial
0e97f0a chore: estado base del frontend (Fases 2 y 3) antes de la intervencion SQA
8bde041 ci: workflow de medicion de linea base de calidad + evidencias iniciales
_ds
index.html
package.json
pnpm-lock.yaml
src
tsconfig.json
tsconfig.node.json
vite.config.ts
```

### G20 — Fase 1 · Línea base — agregar medición del frontend al workflow
- Fecha/hora: 2026-11-03 22:44:35 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .github/workflows/linea-base.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: medir linea base del frontend (ESLint, Prettier, tsc)' && git log --oneline -1
```
```text
248dc57 ci: medir linea base del frontend (ESLint, Prettier, tsc)
```

### G21 — Fase 1 · Línea base — push
- Fecha/hora: 2026-11-03 22:44:40 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git push origin main 2>&1 | grep -v 'acknowledgments\|negotiation'
```
```text
To https://github.com/zenvy-crm/seminario-project
   8bde041..248dc57  main -> main
```

### G22 — Fase 1 · Línea base — disparar medición (backend 0c8d137, frontend 826501b)
- Fecha/hora: 2026-11-03 22:44:42 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api -X POST repos/zenvy-crm/seminario-project/actions/workflows/linea-base.yml/dispatches -f ref=main && echo disparado
```
```text
disparado
```

### G23 — Fase 1 · Línea base — corregir ref del frontend (checkout exige SHA completo)
- Fecha/hora: 2026-11-03 22:53:59 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .github/workflows/linea-base.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: usar SHA completo para la linea base del frontend' && git push -q origin main 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
83d5e6a ci: usar SHA completo para la linea base del frontend
```

### G24 — Fase 1 · Línea base — volver a disparar la medición
- Fecha/hora: 2026-11-03 22:54:05 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api -X POST repos/zenvy-crm/seminario-project/actions/workflows/linea-base.yml/dispatches -f ref=main && echo disparado
```
```text
disparado
```

### G25 — Fase 2 · Pruebas — rama de trabajo
- Fecha/hora: 2026-11-03 22:54:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git checkout -b sqa/pruebas-y-pipeline
```
```text
Switched to a new branch 'sqa/pruebas-y-pipeline'
```

### G26 — Fase 2 · Frontend — instalar dependencias (pnpm)
- Fecha/hora: 2026-11-03 22:54:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ pnpm install --frozen-lockfile 2>&1 | tail -15
```
```text
+ eslint-config-prettier 9.1.2
+ eslint-plugin-react 7.37.5
+ eslint-plugin-react-hooks 4.6.2
+ prettier 3.9.6
+ typescript 5.9.3
+ vite 5.4.21

╭ Warning ─────────────────────────────────────────────────────────────────────╮
│                                                                              │
│   Ignored build scripts: esbuild@0.21.5.                                     │
│   Run "pnpm approve-builds" to pick which dependencies should be allowed     │
│   to run scripts.                                                            │
│                                                                              │
╰──────────────────────────────────────────────────────────────────────────────╯
Done in 4.8s using pnpm v10.28.0
```

### G27 — Fase 1 · Línea base frontend — ESLint
- Fecha/hora: 2026-11-03 22:54:24 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec eslint src --ext .ts,.tsx 2>&1 | tail -25
```
```text

Oops! Something went wrong! :(

ESLint: 8.57.1

ESLint couldn't find the plugin "eslint-plugin-react-refresh".

(The package "eslint-plugin-react-refresh" was not found when loaded as a Node module from the directory "/home/claude/seminario-project/frontend".)

It's likely that the plugin isn't installed correctly. Try reinstalling by running the following:

    npm install eslint-plugin-react-refresh@latest --save-dev

The plugin "eslint-plugin-react-refresh" was referenced from the config file in ".eslintrc.json".

If you still can't figure out the problem, please stop by https://eslint.org/chat/help to chat with the team.
```

### G28 — Fase 1 · Línea base frontend — Prettier
- Fecha/hora: 2026-11-03 22:54:35 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec prettier --check 'src/**/*.{ts,tsx,json,css}' 2>&1 | tail -40; echo ARCHIVOS_SIN_FORMATO=$(npx -y pnpm@9 exec prettier --list-different 'src/**/*.{ts,tsx,json,css}' | wc -l)
```
```text
Checking formatting...
[warn] src/api/authApi.ts
[warn] src/api/documentsApi.ts
[warn] src/api/fileValidator.ts
[warn] src/api/operacionesApi.ts
[warn] src/api/profileApi.ts
[warn] src/api/reportesApi.ts
[warn] src/api/usersApi.ts
[warn] src/App.tsx
[warn] src/auth/AuthContext.tsx
[warn] src/auth/ProtectedRoute.tsx
[warn] src/components/common/Badges.tsx
[warn] src/components/common/ConfirmDialog.tsx
[warn] src/components/common/StateViews.tsx
[warn] src/components/common/ToastProvider.tsx
[warn] src/components/layout/Header.tsx
[warn] src/components/layout/MainLayout.tsx
[warn] src/components/layout/Sidebar.tsx
[warn] src/components/users/UserForm.tsx
[warn] src/pages/auth/Login.tsx
[warn] src/pages/auth/Mfa.tsx
[warn] src/pages/auth/Register.tsx
[warn] src/pages/dashboard/Dashboard.tsx
[warn] src/pages/errors/Forbidden.tsx
[warn] src/pages/errors/NotFound.tsx
[warn] src/pages/operaciones/Operaciones.tsx
[warn] src/pages/ordenes/OrdenCompraPage.tsx
[warn] src/pages/profile/Profile.tsx
[warn] src/pages/reportes/AuditoriaAnulacionesPage.tsx
[warn] src/pages/reportes/comunes.tsx
[warn] src/pages/reportes/RankingProveedoresPage.tsx
[warn] src/pages/reportes/ReporteOrdenesPage.tsx
[warn] src/pages/users/UserCreate.tsx
[warn] src/pages/users/UserDetail.tsx
[warn] src/pages/users/UserEdit.tsx
[warn] src/pages/users/UsersList.tsx
[warn] src/theme/Blueprint.tsx
[warn] src/theme/industryTheme.ts
[warn] src/types/index.ts
[warn] Code style issues found in 38 files. Run Prettier with --write to fix.
ARCHIVOS_SIN_FORMATO=38
```

### G29 — Fase 1 · Línea base frontend — Prettier ignorando fin de línea (para separar CRLF de formato real)
- Fecha/hora: 2026-11-03 22:54:53 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec prettier --end-of-line auto --list-different 'src/**/*.{ts,tsx,json,css}' | tee /tmp/prettier-real.txt; echo ARCHIVOS_CON_FORMATO_REAL=$(wc -l < /tmp/prettier-real.txt)
```
```text
src/api/authApi.ts
src/api/documentsApi.ts
src/api/fileValidator.ts
src/api/operacionesApi.ts
src/api/profileApi.ts
src/api/reportesApi.ts
src/api/usersApi.ts
src/App.tsx
src/auth/AuthContext.tsx
src/auth/ProtectedRoute.tsx
src/components/common/Badges.tsx
src/components/common/ConfirmDialog.tsx
src/components/common/StateViews.tsx
src/components/common/ToastProvider.tsx
src/components/layout/Header.tsx
src/components/layout/MainLayout.tsx
src/components/layout/Sidebar.tsx
src/components/users/UserForm.tsx
src/pages/auth/Login.tsx
src/pages/auth/Mfa.tsx
src/pages/auth/Register.tsx
src/pages/dashboard/Dashboard.tsx
src/pages/errors/Forbidden.tsx
src/pages/errors/NotFound.tsx
src/pages/operaciones/Operaciones.tsx
src/pages/ordenes/OrdenCompraPage.tsx
src/pages/profile/Profile.tsx
src/pages/reportes/AuditoriaAnulacionesPage.tsx
src/pages/reportes/comunes.tsx
src/pages/reportes/RankingProveedoresPage.tsx
src/pages/reportes/ReporteOrdenesPage.tsx
src/pages/users/UserCreate.tsx
src/pages/users/UserDetail.tsx
src/pages/users/UserEdit.tsx
src/pages/users/UsersList.tsx
src/theme/Blueprint.tsx
src/theme/industryTheme.ts
src/types/index.ts
ARCHIVOS_CON_FORMATO_REAL=38
```

### G30 — Fase 1 · Línea base frontend — TypeScript estricto
- Fecha/hora: 2026-11-03 22:54:57 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec tsc --noEmit -p tsconfig.json 2>&1 | tail -20; echo TSC_RC=${PIPESTATUS[0]}
```
```text
TSC_RC=0
```

### G31 — Fase 2 · Frontend — agregar herramientas de prueba y el plugin de ESLint faltante
- Fecha/hora: 2026-11-03 22:55:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 add -D vitest@2.1.9 @vitest/coverage-v8@2.1.9 jsdom@25.0.1 @testing-library/react@16.1.0 @testing-library/dom@10.4.0 @testing-library/jest-dom@6.6.3 @testing-library/user-event@14.5.2 eslint-plugin-react-refresh@0.4.16 2>&1 | tail -15
```
```text
 ERR_PNPM_UNEXPECTED_STORE  Unexpected store location

The dependencies at "/home/claude/seminario-project/frontend/node_modules" are currently linked from the store at "/root/.local/share/pnpm/store/v10".

pnpm now wants to use the store at "/root/.local/share/pnpm/store/v3" to link dependencies.

If you want to use the new store location, reinstall your dependencies with "pnpm install".

You may change the global store location by running "pnpm config set store-dir <dir> --global".
(This error may happen if the node_modules was installed with a different major version of pnpm)
```

### G32 — Fase 2 · Frontend — reinstalar con pnpm 9 (misma versión que el pipeline)
- Fecha/hora: 2026-11-03 22:55:23 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 install --frozen-lockfile 2>&1 | tail -4
```
```text
+ typescript 5.9.3
+ vite 5.4.21

Done in 4.9s using pnpm v9.15.9
```

### G33 — Fase 2 · Frontend — agregar herramientas de prueba y el plugin de ESLint faltante
- Fecha/hora: 2026-11-03 22:55:29 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 add -D vitest@2.1.9 @vitest/coverage-v8@2.1.9 jsdom@25.0.1 @testing-library/react@16.1.0 @testing-library/dom@10.4.0 @testing-library/jest-dom@6.6.3 @testing-library/user-event@14.5.2 eslint-plugin-react-refresh@0.4.16 2>&1 | tail -15
```
```text
Packages: +126
++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
Progress: resolved 551, reused 379, downloaded 126, added 126, done

devDependencies:
+ @testing-library/dom 10.4.0 (10.4.2 is available)
+ @testing-library/jest-dom 6.6.3 (7.0.1 is available)
+ @testing-library/react 16.1.0 (16.3.3 is available)
+ @testing-library/user-event 14.5.2 (14.6.8 is available)
+ @vitest/coverage-v8 2.1.9 (5.1.0 is available)
+ eslint-plugin-react-refresh 0.4.16 (0.5.7 is available)
+ jsdom 25.0.1 (30.1.3 is available)
+ vitest 2.1.9 (5.1.0 is available)

Done in 3.7s using pnpm v9.15.9
```

### G34 — Fase 2 · Frontend — ejecutar pruebas con cobertura
- Fecha/hora: 2026-11-03 22:57:40 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 test:coverage 2>&1 | tail -60
```
```text
    116|     expect(post).toHaveBeenCalledWith(
    117|       "/users/7/documents",

Caused by: Error: Error al validar el archivo
 ❯ Object.upload src/api/documentsApi.ts:20:13

⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯[1/4]⎯

 FAIL  src/api/fileValidator.test.ts > validador de archivos > FE-VAL-02 acepta PDF y JPEG por su firma real
AssertionError: expected { valid: false, …(1) } to deeply equal { valid: true }

- Expected
+ Received

  Object {
-   "valid": true,
+   "error": "Error al validar el archivo",
+   "valid": false,
  }

⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯[2/4]⎯

 FAIL  src/api/fileValidator.test.ts > validador de archivos > FE-VAL-03 rechaza un PNG renombrado y un archivo sin firma conocida
AssertionError: expected 'Error al validar el archivo' to match /PDF o JPEG/

- Expected: 
/PDF o JPEG/

+ Received: 
"Error al validar el archivo"

 ❯ src/api/fileValidator.test.ts:40:23
     38|     const png = await validateFileType(new File([PNG], "falso.pdf"));
     39|     expect(png.valid).toBe(false);
     40|     expect(png.error).toMatch(/PDF o JPEG/);
       |                       ^
     41| 
     42|     const texto = await validateFileType(new File(["hola"], "a.pdf"));

⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯[3/4]⎯

 FAIL  src/api/fileValidator.test.ts > validador de archivos > FE-VAL-04 si la lectura del archivo falla devuelve un error controlado
Error: arrayBuffer does not exist
 ❯ src/api/fileValidator.test.ts:51:8
     49|   it("FE-VAL-04 si la lectura del archivo falla devuelve un error cont…
     50|     const roto = new File([PDF], "a.pdf");
     51|     vi.spyOn(roto, "arrayBuffer").mockRejectedValue(new Error("io"));
       |        ^
     52|     await expect(validateFileType(roto)).resolves.toEqual({
     53|       valid: false,

⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯[4/4]⎯

 Test Files  2 failed | 3 passed (5)
      Tests  4 failed | 38 passed (42)
   Start at  22:57:44
   Duration  8.25s (transform 223ms, setup 1.22s, collect 1.69s, tests 732ms, environment 2.33s, prepare 434ms)

JUNIT report written to /home/claude/seminario-project/frontend/coverage/junit.xml
 ELIFECYCLE  Command failed with exit code 1.
```

### G35 — Fase 2 · Frontend — ejecutar pruebas con cobertura (con polyfill de Blob.arrayBuffer para jsdom)
- Fecha/hora: 2026-11-03 22:57:57 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 test:coverage 2>&1 | tail -45
```
```text
⚠️ React Router Future Flag Warning: Relative route resolution within Splat routes is changing in v7. You can use the `v7_relativeSplatPath` future flag to opt-in early. For more information, see https://reactrouter.com/v6/upgrading/future#v7_relativesplatpath.

 ✓ src/auth/auth.test.tsx (12 tests) 149ms
 ✓ src/components/common/common.test.tsx (7 tests) 481ms
 ✓ src/api/axios.test.ts (4 tests) 16ms
 ✓ src/api/fileValidator.test.ts (9 tests) 17ms

 Test Files  5 passed (5)
      Tests  42 passed (42)
   Start at  22:58:01
   Duration  8.28s (transform 305ms, setup 1.14s, collect 1.61s, tests 702ms, environment 2.45s, prepare 465ms)

JUNIT report written to /home/claude/seminario-project/frontend/coverage/junit.xml
 % Coverage report from v8
-------------------|---------|----------|---------|---------|-------------------
File               | % Stmts | % Branch | % Funcs | % Lines | Uncovered Line #s 
-------------------|---------|----------|---------|---------|-------------------
All files          |   95.73 |     95.1 |   98.27 |   95.73 |                   
 api               |   99.06 |    95.77 |     100 |   99.06 |                   
  authApi.ts       |     100 |      100 |     100 |     100 |                   
  axios.ts         |   93.33 |     87.5 |     100 |   93.33 | 29-30             
  documentsApi.ts  |     100 |     87.5 |     100 |     100 | 20                
  fileValidator.ts |     100 |    88.88 |     100 |     100 | 27                
  ...acionesApi.ts |     100 |      100 |     100 |     100 |                   
  profileApi.ts    |     100 |      100 |     100 |     100 |                   
  reportesApi.ts   |     100 |      100 |     100 |     100 |                   
  usersApi.ts      |     100 |      100 |     100 |     100 |                   
 auth              |     100 |    94.87 |     100 |     100 |                   
  AuthContext.tsx  |     100 |    93.93 |     100 |     100 | 39-41             
  ...ctedRoute.tsx |     100 |      100 |     100 |     100 |                   
  RoleRoute.tsx    |     100 |      100 |     100 |     100 |                   
 components/common |   87.68 |    93.93 |   94.11 |   87.68 |                   
  Badges.tsx       |     100 |      100 |     100 |     100 |                   
  ...irmDialog.tsx |     100 |       50 |     100 |     100 | 22                
  ...rBoundary.tsx |   76.05 |    88.88 |     100 |   76.05 | 62-80             
  StateViews.tsx   |     100 |      100 |     100 |     100 |                   
  ...tProvider.tsx |     100 |      100 |      75 |     100 |                   
-------------------|---------|----------|---------|---------|-------------------

=============================== Coverage summary ===============================
Statements   : 95.73% ( 426/445 )
Branches     : 95.1% ( 136/143 )
Functions    : 98.27% ( 57/58 )
Lines        : 95.73% ( 426/445 )
================================================================================
```

### G36 — Fase 2 · Frontend — ESLint (ahora que el plugin existe)
- Fecha/hora: 2026-11-03 22:58:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 lint 2>&1 | tail -30
```
```text


/home/claude/seminario-project/frontend/src/api/profileApi.ts
  5:20  warning  'userId' is defined but never used. Allowed unused args must match /^_/u  @typescript-eslint/no-unused-vars

/home/claude/seminario-project/frontend/src/auth/AuthContext.tsx
  83:6  warning  React Hook useCallback has a missing dependency: 'persist'. Either include it or remove the dependency array  react-hooks/exhaustive-deps
  92:6  warning  React Hook useCallback has a missing dependency: 'persist'. Either include it or remove the dependency array  react-hooks/exhaustive-deps

/home/claude/seminario-project/frontend/src/components/users/UserForm.tsx
  36:77  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

/home/claude/seminario-project/frontend/src/pages/auth/Login.tsx
   4:61  warning  'MLink' is defined but never used. Allowed unused vars must match /^_/u  @typescript-eslint/no-unused-vars
   5:10  warning  'Link' is defined but never used. Allowed unused vars must match /^_/u   @typescript-eslint/no-unused-vars
  20:37  error    Unexpected any. Specify a different type                                 @typescript-eslint/no-explicit-any
  36:17  error    Unexpected any. Specify a different type                                 @typescript-eslint/no-explicit-any

/home/claude/seminario-project/frontend/src/pages/auth/Mfa.tsx
  11:37  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

/home/claude/seminario-project/frontend/src/pages/operaciones/Operaciones.tsx
  132:21  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

/home/claude/seminario-project/frontend/src/pages/users/UserCreate.tsx
  18:43  error  Unexpected any. Specify a different type  @typescript-eslint/no-explicit-any

✖ 11 problems (6 errors, 5 warnings)

 ELIFECYCLE  Command failed with exit code 1.
```

### G37 — Fase 2 · Commit — pruebas unitarias, de integración y de regresión
- Fecha/hora: 2026-11-03 22:58:29 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .gitignore frontend/package.json frontend/pnpm-lock.yaml frontend/vitest.config.ts frontend/src seminario-backend-service/Program.cs coverlet.runsettings seminario.sln tests && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'test: suite de pruebas unitarias, de integracion y de regresion (backend y frontend)' -m '- Backend: xUnit + Moq (unitarias), WebApplicationFactory + Testcontainers SQL Server/MongoDB (integracion).
- Casos de regresion marcados con Trait Category=Regression.
- Frontend: Vitest + Testing Library con umbral de cobertura 60 %.
- coverlet.runsettings: formatos cobertura/opencover, excluye migraciones generadas.' && git log --oneline -1 && git show --stat HEAD | tail -25
```
```text
85c3140 test: suite de pruebas unitarias, de integracion y de regresion (backend y frontend)
 frontend/src/api/fileValidator.test.ts             |   57 +
 frontend/src/auth/auth.test.tsx                    |  220 ++++
 frontend/src/components/common/common.test.tsx     |  113 ++
 frontend/src/test/setup.ts                         |   21 +
 frontend/vitest.config.ts                          |   35 +
 seminario-backend-service/Program.cs               |    3 +
 seminario.sln                                      |   31 +
 tests/Seminario.IntegrationTests/AuthFlowTests.cs  |  227 ++++
 .../Seminario.IntegrationTests/DocumentosTests.cs  |  103 ++
 .../Infrastructure/ApiFactory.cs                   |  217 ++++
 .../OrdenesCompraTests.cs                          |  243 +++++
 .../ReportesYSaludTests.cs                         |  140 +++
 .../Seminario.IntegrationTests.csproj              |   32 +
 .../UsuariosYPerfilTests.cs                        |  102 ++
 tests/Seminario.UnitTests/Api/ApiLayerTests.cs     |  285 +++++
 .../Seminario.UnitTests/Helpers/CapturingLogger.cs |   31 +
 tests/Seminario.UnitTests/Helpers/TestData.cs      |   37 +
 .../Infrastructure/AuthInfrastructureTests.cs      |  108 ++
 .../Infrastructure/FileStorageServiceTests.cs      |  171 +++
 .../Infrastructure/MfaServiceTests.cs              |  123 +++
 .../Seminario.UnitTests/Seminario.UnitTests.csproj |   35 +
 .../Services/AuthServiceTests.cs                   |  322 ++++++
 .../Services/ProfileServiceTests.cs                |  125 +++
 .../Services/UserServiceTests.cs                   |  216 ++++
 30 files changed, 4418 insertions(+), 16 deletions(-)
```

### G38 — Fase 3 · Commit — pipeline CI/CD (build, test, analyze, report)
- Fecha/hora: 2026-11-03 22:59:22 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .github/workflows/ci.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: pipeline CI/CD con etapas build, test, analyze y report' -m 'Quality gates: pruebas, cobertura backend >= 60 %, cobertura frontend >= 60 %, ESLint/Prettier, dotnet format y Quality Gate de SonarCloud.' && git log --oneline -3
```
```text
9c32b0e ci: pipeline CI/CD con etapas build, test, analyze y report
85c3140 test: suite de pruebas unitarias, de integracion y de regresion (backend y frontend)
83d5e6a ci: usar SHA completo para la linea base del frontend
```

### G39 — Fase 3 · Push de la rama
- Fecha/hora: 2026-11-03 22:59:22 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git push -u origin sqa/pruebas-y-pipeline 2>&1 | grep -v 'acknowledgments\|negotiation'
```
```text
remote: 
remote: Create a pull request for 'sqa/pruebas-y-pipeline' on GitHub by visiting:        
remote:      https://github.com/zenvy-crm/seminario-project/pull/new/sqa/pruebas-y-pipeline        
remote: 
To https://github.com/zenvy-crm/seminario-project
 * [new branch]      sqa/pruebas-y-pipeline -> sqa/pruebas-y-pipeline
branch 'sqa/pruebas-y-pipeline' set up to track 'origin/sqa/pruebas-y-pipeline'.
```

### G40 — Fase 3 · Abrir pull request
- Fecha/hora: 2026-11-03 22:59:31 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api repos/zenvy-crm/seminario-project/pulls -f title='SQA: pruebas automatizadas y pipeline CI/CD con quality gates' -f head=sqa/pruebas-y-pipeline -f base=main -F body=@/tmp/pr-body.md --jq '{number, html_url}'
```
```text
{"html_url":"https://github.com/zenvy-crm/seminario-project/pull/1","number":1}
```

### G41 — Fase 2 · Frontend — verificar compilación TypeScript (tsc -b) localmente
- Fecha/hora: 2026-11-03 23:00:43 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec tsc -b 2>&1 | tail -30; echo TSC_RC=${PIPESTATUS[0]}
```
```text
TSC_RC=0
```

### G42 — Fase 2 · Frontend — pruebas tras el ajuste de tipos
- Fecha/hora: 2026-11-03 23:01:01 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 test 2>&1 | grep -E 'Test Files|Tests '
```
```text
 Test Files  5 passed (5)
      Tests  42 passed (42)
```

### G43 — Fase 2 · Commit — corregir compilación de las pruebas (using global de xUnit, tipos de espías)
- Fecha/hora: 2026-11-03 23:01:13 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add tests frontend/src/api/apis.test.ts && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'test: using global de Xunit y tipos de espias compatibles con tsc' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
7bbe927 test: using global de Xunit y tipos de espias compatibles con tsc
```

### G44 — Fase 2 · Commit — corregir UT-TKN-01 (tipo del claim de rol) y formato de 2 archivos de prueba
- Fecha/hora: 2026-11-03 23:05:29 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add -A tests .github/workflows/ci.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'test: corregir lectura del claim de rol y formato segun dotnet format' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
5d7fdef test: corregir lectura del claim de rol y formato segun dotnet format
```

### G45 — Fase 1 · Línea base — agregar análisis SonarCloud del código sin intervenir
- Fecha/hora: 2026-11-03 23:05:43 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .github/workflows/linea-base.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: linea base en SonarCloud sobre el codigo sin intervenir' && git push -q origin main 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
d10aafc ci: linea base en SonarCloud sobre el codigo sin intervenir
```

### G46 — Fase 1 · Línea base — disparar análisis (incluye SonarCloud)
- Fecha/hora: 2026-11-03 23:05:52 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api -X POST repos/zenvy-crm/seminario-project/actions/workflows/linea-base.yml/dispatches -f ref=main && echo disparado
```
```text
disparado
```

### G47 — Fase 3 · Commit — estándares de código y gate de analizadores (warnings = error en CI)
- Fecha/hora: 2026-11-03 23:10:12 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add .editorconfig seminario-backend-service/seminario-backend-service.csproj frontend/.prettierrc docs/SQA-Plan.md && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'ci: estandares de codigo (.editorconfig) y analizadores .NET como quality gate' -m '- AnalysisLevel latest-recommended + EnforceCodeStyleInBuild en la API.
- TreatWarningsAsErrors cuando ContinuousIntegrationBuild=true (pipeline).
- Prettier: endOfLine auto para convivir con checkouts Windows.
- docs/SQA-Plan.md' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
201ca24 ci: estandares de codigo (.editorconfig) y analizadores .NET como quality gate
```

### G48 — Fase 3 · Evidencia — el gate de compilación rechaza el build: 55 errores de analizadores
- Fecha/hora: 2026-11-03 23:11:23 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api repos/zenvy-crm/seminario-project/actions/jobs/126401466243/logs | grep -oE '[^ /]+\.cs\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+' | sort -u | awk '{print $NF}' | sort | uniq -c | sort -rn; echo 'Job: https://github.com/zenvy-crm/seminario-project/actions/runs/41777597462/job/126401466243'
```
```text
     28 CA1305
      6 CA1848
      6 CA1311
      6 CA1304
      5 CA1862
      2 CA1873
      1 CA1859
      1 CA1805
Job: https://github.com/zenvy-crm/seminario-project/actions/runs/41777597462/job/126401466243
```

### G49 — Fase 4 · Commit — corregir los 55 issues de los analizadores .NET
- Fecha/hora: 2026-11-03 23:11:29 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add seminario-backend-service && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'fix: corregir los 55 issues de los analizadores de calidad .NET' -m '- CA1305: conversiones a texto con CultureInfo.InvariantCulture (ids, fechas, codigo MFA, Retry-After).
- CA1848/CA1873: logs de alto rendimiento con [LoggerMessage] (Infrastructure/Logging/LogMessages.cs).
- CA1304/CA1311/CA1862: busqueda de usuarios sin ToLower() sobre columnas (la intercalacion CI de SQL Server ya ignora mayusculas) y comparacion de rol con StringComparison.
- CA1859: Dictionary concreto en DbSeeder. CA1805: inicializacion redundante en ApiErrorResponse.
- CA1707 en migraciones: marcadas como generated_code en .editorconfig.' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
82f6578 fix: corregir los 55 issues de los analizadores de calidad .NET
```

### G50 — Fase 4 · Frontend — ESLint tras corregir any, imports sin uso y dependencias de hooks
- Fecha/hora: 2026-11-03 23:11:58 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 lint --max-warnings 0 2>&1 | tail -20; echo LINT_RC=${PIPESTATUS[0]}
```
```text

> industry-frontend@0.1.0 lint /home/claude/seminario-project/frontend
> eslint src --ext .ts,.tsx --max-warnings 0

LINT_RC=0
```

### G51 — Fase 4 · Frontend — aplicar Prettier (estándar de formato) a src/
- Fecha/hora: 2026-11-03 23:12:09 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec prettier --write 'src/**/*.{ts,tsx,json,css}' 2>&1 | grep -vc unchanged; npx -y pnpm@9 format:check 2>&1 | tail -2
```
```text
43
Checking formatting...
All matched files use Prettier code style!
```

### G52 — Fase 4 · Frontend — verificación local: build + lint + pruebas
- Fecha/hora: 2026-11-03 23:12:24 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 build 2>&1 | tail -4 && npx -y pnpm@9 lint --max-warnings 0 >/dev/null && echo LINT_OK && npx -y pnpm@9 test:coverage 2>&1 | grep -E 'Tests |All files|Lines'
```
```text
- Using dynamic import() to code-split the application
- Use build.rollupOptions.output.manualChunks to improve chunking: https://rollupjs.org/configuration-options/#output-manualchunks
- Adjust chunk size limit for this warning via build.chunkSizeWarningLimit.
✓ built in 7.17s
LINT_OK
      Tests  42 passed (42)
File               | % Stmts | % Branch | % Funcs | % Lines | Uncovered Line #s 
All files          |   96.37 |     95.1 |   98.24 |   96.37 |                   
Lines        : 96.37% ( 585/607 )
```

### G53 — Fase 4 · Commit — frontend: 11 issues de ESLint corregidos y formato Prettier en 43 archivos
- Fecha/hora: 2026-11-03 23:13:11 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add frontend && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'fix(frontend): corregir issues de ESLint y aplicar formato Prettier' -m '- .eslintrc referenciaba eslint-plugin-react-refresh sin instalarlo: ESLint no podia ejecutarse (agregado en el commit de pruebas).
- 6 errores no-explicit-any: tipos explicitos para location.state, errores de axios y esquemas zod.
- 3 imports/parametros sin uso y 2 dependencias faltantes de useCallback (persist).
- Prettier aplicado a src/ (38 archivos no cumplian el formato).' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
389e6e5 fix(frontend): corregir issues de ESLint y aplicar formato Prettier
```

### G54 — Fase 1 · Línea base — relanzar análisis SonarCloud (Automatic Analysis ya desactivado)
- Fecha/hora: 2026-11-03 23:13:31 (America/Guatemala)
- Codigo de salida: 0
```bash
$ gh api -X POST repos/zenvy-crm/seminario-project/actions/workflows/linea-base.yml/dispatches -f ref=main && echo disparado
```
```text
disparado
```

### G55 — Fase 4 · Commit — ordenar usings de Program.cs (dotnet format IMPORTS) y publicar issues de Sonar en el log
- Fecha/hora: 2026-11-03 23:16:09 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add -A seminario-backend-service/Program.cs .github/workflows/ci.yml && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'fix: ordenar usings de Program.cs segun .editorconfig; ci: listar issues de SonarCloud en el log' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
af4d57f fix: ordenar usings de Program.cs segun .editorconfig; ci: listar issues de SonarCloud en el log
```

### G56 — Fase 4 · Actualizar la rama con main (trae el job de línea base de SonarCloud)
- Fecha/hora: 2026-11-03 23:23:04 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git fetch -q origin main && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' merge --no-edit origin/main 2>&1 | tail -3
```
```text
Merge made by the 'ort' strategy.
 .github/workflows/linea-base.yml | 36 ++++++++++++++++++++++++++++++++++++
 1 file changed, 36 insertions(+)
```

### G57 — Fase 4 · Frontend — verificación local tras corregir issues de Sonar
- Fecha/hora: 2026-11-03 23:24:05 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec prettier --write src/test/setup.ts src/pages/reportes/comunes.tsx src/pages/users/UsersList.tsx >/dev/null && npx -y pnpm@9 lint --max-warnings 0 >/dev/null && echo LINT_OK && npx -y pnpm@9 format:check | tail -1 && npx -y pnpm@9 exec tsc -b && echo TSC_OK && npx -y pnpm@9 test 2>&1 | grep -E 'Tests '
```
```text
LINT_OK
All matched files use Prettier code style!
TSC_OK
      Tests  42 passed (42)
```

### G58 — Fase 4 · Frontend — prueba de regresión del regex corregido
- Fecha/hora: 2026-11-03 23:24:56 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx -y pnpm@9 exec prettier --write src/pages/reportes/comunes.test.ts >/dev/null && npx -y pnpm@9 lint --max-warnings 0 >/dev/null && echo LINT_OK && npx -y pnpm@9 exec tsc -b && npx -y pnpm@9 test 2>&1 | grep -E 'comunes|Tests |FAIL'
```
```text
LINT_OK
 ✓ src/pages/reportes/comunes.test.ts (3 tests) 23ms
      Tests  45 passed (45)
```

### G59 — Fase 4 · Commit — corregir bugs, vulnerabilidades y code smells de SonarCloud
- Fecha/hora: 2026-11-03 23:26:00 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add -A .github seminario-backend-service sql frontend tests docs/Linea-Base-Calidad.md && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'fix: corregir bugs, vulnerabilidades y code smells reportados por SonarCloud' -m 'Bugs (linea base: 3)
- Middleware de errores: WriteAsJsonAsync con context.RequestAborted (S8949).
- comunes.tsx: regex de zona horaria con precedencia explicita (S5850) + prueba FE-REG-01.
- sql/04: UPDATE con WHERE explicito sobre la tabla en lugar de una CTE con TOP.

Vulnerabilidades (linea base: 4; pipeline: 4)
- Cookie del refresh token siempre HttpOnly + Secure, tambien al borrarla (S2092/S3330).
- Stress test: Random.Shared para datos sinteticos (S2245).
- Workflows: acciones fijadas por SHA (S7637) y permisos por job (S8264).

Code smells
- await app.RunAsync() (S6966), AddAuthorizationBuilder (ASP0025), bool? en filtros de query (S6964),
  expresiones de coleccion (IDE0028), throw expression (IDE0270), Number.parseInt (S7773), CA1862 documentado en EF.

Sonar: cobertura del frontend acotada al alcance del SQA Plan (paginas, tema y App fuera).' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
3d27301 fix: corregir bugs, vulnerabilidades y code smells reportados por SonarCloud
```

### G60 — Fase 4 · Commit — quitar dependencia sin uso en AuthController
- Fecha/hora: 2026-11-03 23:26:11 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git add seminario-backend-service/Controllers/AuthController.cs && git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'refactor: quitar IWebHostEnvironment sin uso en AuthController' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
1b418d2 refactor: quitar IWebHostEnvironment sin uso en AuthController
```

### G61 — Fase 4 · Medición final — duplicación (jscpd) backend
- Fecha/hora: 2026-11-03 23:32:45 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes jscpd@4 seminario-backend-service --format csharp --ignore '**/Migrations/**,**/bin/**,**/obj/**,**/.vs/**' --min-tokens 50 --reporters console 2>&1 | grep -E 'csharp|Total' | tail -3
```
```text
│ Format │ Files analyzed │ Total lines │ Total tokens │ Clones found │ Duplicated lines │ Duplicated tokens │
│ csharp │ 96             │ 5769        │ 51896        │ 10           │ 134 (2.32%)      │ 1419 (2.73%)      │
│ Total: │ 96             │ 5769        │ 51896        │ 10           │ 134 (2.32%)      │ 1419 (2.73%)      │
```

### G62 — Fase 4 · Medición final — duplicación (jscpd) frontend
- Fecha/hora: 2026-11-03 23:32:55 (America/Guatemala)
- Codigo de salida: 0
```bash
$ npx --yes jscpd@4 frontend/src --min-tokens 50 --reporters console 2>&1 | grep -E 'Total' | tail -1
```
```text
│ Total:     │ 80             │ 8407        │ 73183        │ 7            │ 184 (2.19%)      │ 1635 (2.23%)      │
```

### G63 — Fase 4 · Documentación — mover el README técnico del backend a docs/ (el README raíz pasa a ser el del proyecto)
- Fecha/hora: 2026-11-03 23:34:30 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git mv README.md docs/README-backend.md && git status --short | head -3
```
```text
R  README.md -> docs/README-backend.md
 M seminario-backend-service/Application/DTOs/Operaciones/OrdenCompraDtos.cs
 M seminario-backend-service/Infrastructure/Services/OrdenCompraService.cs
```

### G64 — Fase 4 · Commit — documentación SQA, últimos code smells y bitácoras
- Fecha/hora: 2026-11-03 23:35:15 (America/Guatemala)
- Codigo de salida: 0
```bash
$ git -c user.name='Alder Solis' -c user.email='alder.solis@bowpi.com' commit -q -m 'docs: SQA Plan, matriz de pruebas, linea base, reporte de mejora, manual tecnico, README con badges' -m '- refactor: expresiones de coleccion en OrdenCompraService/OrdenCompraDtos (ultimos 5 IDE0028 de SonarCloud).
- docs/evidencias: bitacoras de comandos (local y GitHub).
- El README tecnico del backend pasa a docs/README-backend.md.' && git push -q 2>&1 | grep -v 'acknowledgments\|negotiation'; git log --oneline -1
```
```text
5a60764 docs: SQA Plan, matriz de pruebas, linea base, reporte de mejora, manual tecnico, README con badges
```

