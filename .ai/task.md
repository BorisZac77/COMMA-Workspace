# Aktualne zadanie

- TASK_ID: PACKAGE-WORKSPACE-5.0-027
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 351572f936b772c9e17b96c63696340173f72f9b
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package COMMA Workspace 5.0 for macOS and Windows
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Po opublikowanej walidacji PDF przygotować dwa osobne artefakty do testu:
- `/Users/Boris/Desktop/COMMA Workspace 5.0.app` dla macOS ARM64;
- `/Users/Boris/Desktop/COMMA Workspace 5.0 Windows x64.zip` zawierający samodzielną aplikację Windows x64.

Nie wolno naruszyć istniejących aplikacji lub ZIP-ów 4.x.

## Wymagania

1. Przeczytaj `AGENTS.md` i wszystkie pliki `.ai`; potwierdź worktree, gałąź `workspace-4.0`, czyste drzewo, aktualny HEAD równy bazie albo jej potomkowi oraz `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Nie zmieniaj kodu, plików projektu, wersji ani `build_app.sh`. W repozytorium modyfikuj tylko report/handoff.
3. Zbuduj macOS przez istniejące `./build_app.sh`. Przed i po potwierdź, że żaden artefakt 4.x na Pulpicie nie został usunięty ani nadpisany. Zweryfikuj:
   - istnieje dokładnie `/Users/Boris/Desktop/COMMA Workspace 5.0.app`;
   - `Info.plist` ma nazwę i wersję 5.0.0;
   - wykonywalny `Contents/MacOS/COMMA.App` istnieje;
   - `codesign --verify --deep --strict` przechodzi;
   - aplikacja uruchamia się bez natychmiastowego błędu.
4. Zbuduj Windows przez `dotnet publish COMMA.App -c Release -r win-x64 --self-contained true` do unikalnego katalogu w `/tmp`, bez modyfikowania repozytorium. Utwórz ZIP:
   `/Users/Boris/Desktop/COMMA Workspace 5.0 Windows x64.zip`.
   W ZIP-ie ma być katalog główny `COMMA Workspace 5.0 Windows x64` z `COMMA.App.exe`.
5. Zweryfikuj ZIP przez `unzip -t`, sprawdź obecność `COMMA.App.exe`, odczytaj rozmiar i SHA-256. Nie uruchamiaj pliku Windows na macOS.
6. Po utworzeniu artefaktów posprzątaj tylko utworzone katalogi tymczasowe w `/tmp`. Nie usuwaj Pulpitu poza dokładnymi, nowymi docelowymi artefaktami 5.0.
7. Zaktualizuj report/handoff pełnym wynikiem: ścieżki, rozmiar ZIP-a, SHA-256, weryfikacja macOS/Windows, stan artefaktów 4.x.
8. Uruchom `git diff --check` i kontrolę allowlisty. Jeśli wszystko jest poprawne, ustaw `COMPLETED`, commit/push wyłącznie report/handoff. W przeciwnym razie `BLOCKED`, bez commita/pushu.

## Zakazy

- Nie twórz ani nie nadpisuj artefaktów 4.x.
- Nie modyfikuj COMMA WMS, KOMI, `main`, źródeł lub skryptu builda.
- Nie używaj reset, rebase, force-push ani obejść błędów.
