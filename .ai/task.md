# Aktualne zadanie

- TASK_ID: PACKAGE-WINDOWS-4.1D-014
- STATUS: READY
- PROJECT: COMMA Workspace 4.1D
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 0d550501b028dc6b3b8caea5bd05eb4d1339dc05
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package validated Windows 4.1D build
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Utworzyć na Pulpicie nowy, samodzielny pakiet Windows x64 zawierający opublikowaną poprawkę importu załączników z dysków mapowanych Windows (commit `5549ab44e7667d5d277343d023ff4686c88db801`).

## Wymagania

1. Przed działaniem przeczytaj AGENTS.md i wszystkie pliki w `.ai`. Potwierdź właściwy worktree, gałąź `workspace-4.0`, czysty status, relację historii oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Nie zmieniaj kodu ani plików projektu. Dozwolone są wyłącznie `.ai/report.md` i `.ai/handoff.md`.
3. Potwierdź, że commit `5549ab44e7667d5d277343d023ff4686c88db801` jest przodkiem bieżącego HEAD.
4. W katalogu tymczasowym opublikuj `COMMA.App` dla `win-x64` jako self-contained Release. Nie usuwaj wcześniejszych paczek ani aplikacji.
5. Utwórz dokładnie `/Users/Boris/Desktop/COMMA Workspace 4.1D Windows x64.zip`. Archiwum ma zawierać jeden katalog główny `COMMA Workspace 4.1D Windows x64` z `COMMA.App.exe` i wszystkimi zależnościami.
6. Zweryfikuj archiwum poleceniem `unzip -t`, sprawdź obecność `COMMA.App.exe` oraz zapisz rozmiar ZIP-a i wynik kontroli w raporcie.
7. Nie twórz ani nie zmieniaj paczki macOS, nie uruchamiaj aplikacji Windows na Macu, nie modyfikuj `main`, COMMA WMS ani KOMI.
8. Ustaw `COMPLETED` w handoffie wyłącznie po udanym publish, kontroli ZIP-a i kontroli ścieżek. Następnie worker ma wykonać commit i push.

## Zakazy

- Nie usuwaj żadnych istniejących plików z Pulpitu.
- Nie zmieniaj kodu, plików projektu, konfiguracji ani poprzednich archiwów.
- Nie wykonuj resetu, rebase ani force push.
