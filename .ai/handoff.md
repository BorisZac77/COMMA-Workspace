# Stan przekazania

- TASK_ID: WORKER-BLOCKED-RECOVERY-016
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe worker
- BRANCH: workspace-4.0
- HEAD: d88d0f9facf99dda19741e6099f261b00f646964

## Stan

Worker zachowuje zwalidowane zmiany zadania BLOCKED w repozytorium i zapisuje w `STATE_DIR` atomowy znacznik dokładnego stanu. Kolejne zwykłe cykle nie uruchamiają Codexa ani nie raportują brudnego repozytorium, gdy znacznik nadal pasuje.

`--resume-blocked` jest świadomą, jednorazową akcją. Odrzuca stan ze zmienionym HEAD, TASK_ID, fingerprintem, `.ai/task.md`, konfliktem, ścieżką blokowaną lub ścieżką poza zaufaną allowlistą z HEAD. Po udanym pushu znacznik jest usuwany i TASK_ID zostaje oznaczony jako handled.

## Walidacja

- `bash -n .ai/automation/worker.sh`: PASS.
- `.ai/automation/worker.sh --self-test`: PASS.
- `git diff --check`: PASS.
- `main`: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a` — niezmieniony.

## Następny krok

Safe worker powinien zwalidować allowlistę, utworzyć commit i wykonać push zgodnie z metadanymi zadania. Nie wykonano ręcznie commita ani pushu.
