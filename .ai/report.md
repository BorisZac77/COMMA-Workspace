# Raport Codexa

- TASK_ID: WORKER-BLOCKED-RECOVERY-016
- STATUS: COMPLETED
- STARTED_AT: 2026-09-03T12:00:00+0200
- COMPLETED_AT: 2026-09-03T12:00:00+0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: d88d0f9facf99dda19741e6099f261b00f646964
- HEAD_AFTER: d88d0f9facf99dda19741e6099f261b00f646964

## Zrealizowany zakres

- Dodano zewnętrzny znacznik BLOCKED z TASK_ID, gałęzią, HEAD, deterministycznym SHA-256 stanu zmian oraz flagą jednorazowego wznowienia.
- Zwykły cykl rozpoznaje wyłącznie dokładnie pasujący znacznik i kończy się kodem 0 z komunikatem `blocked task preserved; waiting for approved recovery`, bez ponownego uruchamiania Codexa.
- Dodano `--resume-blocked`: wymaga zgodnego znacznika, niezmienionego `.ai/task.md` względem HEAD, zaufanej allowlisty z HEAD oraz walidacji wszystkich zachowanych zmian.
- Wznowienie oznacza znacznik jako wykorzystany przed uruchomieniem Codexa. Po powodzeniu usuwa go dopiero po pushu; po ponownym BLOCKED zachowuje stan bez automatycznej kolejnej próby.
- Walidacja zmian po Codexie korzysta z kopii zadania z HEAD, więc lokalna zmiana `ALLOWED_PATHS_JSON` nie może rozszerzyć uprawnień.

## Kontrole i walidacja

- Worktree: PASS — `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- Gałąź: PASS — `workspace-4.0`; status początkowy był czysty.
- `BASE_HEAD_BEFORE_QUEUE` `3ff9879` jest dostępny; `main` jest przodkiem HEAD — PASS.
- `main` niezmieniony: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a` — PASS.
- `bash -n .ai/automation/worker.sh` — PASS.
- `.ai/automation/worker.sh --self-test` — PASS, w tym odrzucenie obcego brudnego repo, dopasowanie i niedopasowanie znacznika TASK_ID/HEAD/fingerprint oraz zaufana allowlista.
- `git diff --check` — PASS.
- Zakres zmian mieści się w `ALLOWED_PATHS_JSON`: `.ai/automation/worker.sh`, `.ai/report.md`, `.ai/handoff.md`.

Nie wykonano commit ani push; wykona je safe worker po własnej walidacji.
