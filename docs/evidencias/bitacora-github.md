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

