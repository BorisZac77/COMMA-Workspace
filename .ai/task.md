# Aktualne zadanie

- TASK_ID: WINDOWS-ZIP-4.1-005
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- EXPECTED_HEAD_AT_QUEUE_START: 0e1737d54c9ce80b171771aeadf2ec04daecc931
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Validate COMMA Workspace 4.1 Windows package
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel
Przygotować aktualny, samodzielny pakiet Windows x64 z commita `0e1737d54c9ce80b171771aeadf2ec04daecc931` bez jakichkolwiek zmian w kodzie aplikacji.

## Wymagania
1. Potwierdź właściwe repozytorium, gałąź, czysty worktree i dokładny HEAD.
2. Potwierdź, że `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Nie zmieniaj żadnego pliku źródłowego ani projektu.
4. Wykonaj:
   `dotnet publish COMMA.App -c Release -r win-x64 --self-contained true --no-restore -m:1 -o "/tmp/COMMA_Workspace_Windows_4_1/COMMA Workspace 4.1 Windows x64"`
   Jeżeli brak restore uniemożliwi publikację dla win-x64, wykonaj bezpieczny restore i ponów publikację.
5. Zweryfikuj, że wynik zawiera wykonywalny `COMMA.App.exe`, wymagane biblioteki oraz katalogi zasobów.
6. Utwórz ZIP:
   `/tmp/COMMA_Workspace_Windows_4_1/COMMA Workspace 4.1 Windows x64.zip`
   ZIP ma zawierać jeden katalog główny `COMMA Workspace 4.1 Windows x64` wraz z całą zawartością publikacji.
7. Sprawdź integralność ZIP-a poleceniem `unzip -t` oraz potwierdź obecność `COMMA.App.exe` w liście archiwum.
8. Nie zmieniaj ani nie usuwaj wcześniejszych pakietów na Pulpicie.
9. Spróbuj skopiować zweryfikowany ZIP na Pulpit jako:
   `/Users/Boris/Desktop/COMMA Workspace 4.1 Windows x64.zip`.
   Jeśli sandbox zablokuje zapis na Pulpicie, zachowaj zweryfikowany ZIP w podanej ścieżce `/tmp`, odnotuj blokadę, ale oznacz zadanie `COMPLETED`, ponieważ pakiet jest gotowy do prostego skopiowania poza sandboxem.
10. Po walidacji usuń repozytoryjne artefakty `bin` i `obj`, tak aby worktree pozostał czysty poza raportami.

## Walidacja
- `dotnet publish` — PASS.
- `unzip -t` — PASS.
- `COMMA.App.exe` obecny w ZIP.
- Zmienione ścieżki repozytorium wyłącznie `.ai/report.md` i `.ai/handoff.md`.
- `git diff --check` — PASS.

## Zakazy
- Nie zmieniaj `main`.
- Nie zmieniaj COMMA WMS ani KOMI Animation Lab.
- Nie modyfikuj kodu aplikacji, testów, projektów ani skryptów.
- Nie wykonuj resetu, rebase ani force push.
