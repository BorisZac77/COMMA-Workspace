# Aktualne zadanie

- TASK_ID: INTEGRATION-001
- STATUS: READY
- PROJECT: COMMA Workspace 4.0
- BRANCH: workspace-4.0
- CODE_CHANGES: FORBIDDEN
- COMMIT: ALLOWED_FOR_HANDOFF_ONLY
- PUSH: ALLOWED_FOR_HANDOFF_ONLY

## Cel
Potwierdzić, że kanał ChatGPT → GitHub → Codex w Riderze → GitHub → ChatGPT działa.

## Dozwolone zmiany
- `.ai/report.md`
- `.ai/handoff.md`

## Instrukcja
1. Wykonaj kontrole wstępne z `.ai/context.md`.
2. Nie zmieniaj kodu aplikacji.
3. Sprawdź, czy można odczytać pliki `.ai/context.md`, `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.
4. Wpisz wyniki kontroli do `.ai/report.md`.
5. Ustaw w `.ai/handoff.md` stan zadania na `COMPLETED` albo `BLOCKED`.
6. Jeśli stan to `COMPLETED`, wykonaj commit zawierający wyłącznie `.ai/report.md` i `.ai/handoff.md` z komunikatem `Complete ChatGPT Codex handoff check`, a następnie push na `origin/workspace-4.0`.
