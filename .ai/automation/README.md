# COMMA Workspace 4.0 — automatyczny worker Codexa

LaunchAgent `pl.comma.workspace4.codex-worker` co 60 sekund uruchamia jeden
bezpieczny cykl `worker.sh`. Worker obsługuje wyłącznie repozytorium
`/Users/Boris/RiderProjects/COMMA Workspace 4.0` i gałąź `workspace-4.0`.

Przed uruchomieniem Codexa worker wymaga czystego drzewa, pobiera `origin` i
akceptuje jedynie fast-forward. Zadanie jest uruchamiane tylko dla nowego
`TASK_ID` ze stanem `READY`. Codex działa nieinteraktywnie w sandboxie
`workspace-write` z polityką zgód `never`. Worker waliduje zmiany względem
`ALLOWED_PATHS_JSON`, blokuje sekrety i lokalne artefakty, a commit i push
wykonuje dopiero po pomyślnym zadaniu i stanie handoff `COMPLETED`.

## Instalacja i kontrola

```bash
bash .ai/automation/install.sh
launchctl print gui/$(id -u)/pl.comma.workspace4.codex-worker
tail -n 100 "$HOME/Library/Application Support/COMMA AI/Workspace4/worker.log"
```

Ręczny, bezpieczny cykl kontrolny:

```bash
bash .ai/automation/worker.sh
```

Stan obsłużonego zadania znajduje się w:

```text
~/Library/Application Support/COMMA AI/Workspace4/handled-task-id
```

## Zatrzymanie

```bash
bash .ai/automation/uninstall.sh
```

Skrypt zatrzymuje i wyłącza LaunchAgenta, ale pozostawia plist, kod automatu,
logi oraz stan do kontroli i ponownej instalacji.

## Kody wyjścia workera

- `0` — sukces albo brak nowego zadania,
- `20` — blokada bezpieczeństwa repozytorium,
- `30` — błąd wykonania Codexa,
- `40` — błąd metadanych lub walidacji zmian,
- `50` — błąd operacji Git, commita albo pushu.

Worker nie wykonuje automatycznego rozwiązywania konfliktów ani operacji
przepisujących historię. Odrzucony push pozostawia lokalny stan do ręcznej
kontroli.
