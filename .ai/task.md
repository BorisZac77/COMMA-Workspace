# Aktualne zadanie

- TASK_ID: PACKAGE-WINDOWS-WORKSPACE-5.0-022
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: fd681c2549269353d85ccdca7bc7497bcd591bf2
- SOURCE_CODE_COMMIT: a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package COMMA Workspace 5.0 for Windows
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Przygotować samodzielny pakiet Windows x64 z zatwierdzonego kodu COMMA Workspace 5.0 i umieścić go na Pulpicie jako:

`/Users/Boris/Desktop/COMMA Workspace 5.0 Windows x64.zip`

Pakiet ma zawierać jeden katalog główny:

`COMMA Workspace 5.0 Windows x64/`

## Wymagania wstępne

1. Przeczytaj w całości `AGENTS.md` i wszystkie pliki w `.ai`.
2. Potwierdź właściwy worktree, gałąź `workspace-4.0`, czysty status, relację historii i niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Potwierdź, że commit kodu 5.0 `a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed` jest przodkiem HEAD.
4. Nie zmieniaj kodu, testów, skryptów ani konfiguracji. Zmiany w repozytorium mogą dotyczyć wyłącznie raportu i handoffu.
5. Nie zmieniaj COMMA WMS, KOMI, `main`, gałęzi ani formatu danych PDF v4.

## Pakiet

1. Wykonaj Release publish `win-x64`, self-contained dla `COMMA.App/COMMA.App.csproj`.
2. Utwórz ZIP z jednym katalogiem głównym `COMMA Workspace 5.0 Windows x64/`.
3. W katalogu głównym musi znajdować się dokładnie jeden `COMMA.App.exe`.
4. Pakiet musi zawierać markery self-contained, w tym `COMMA.App.dll`, `COMMA.App.deps.json`, `COMMA.App.runtimeconfig.json`, `hostfxr.dll` i `coreclr.dll`.
5. Jeżeli docelowy ZIP już istnieje, nie nadpisuj go. Zweryfikuj jego integralność i strukturę; jeśli spełnia wszystkie wymagania, przyjmij go jako wynik zadania. Jeśli jest nieprawidłowy, ustaw `BLOCKED` i podaj dokładną przyczynę.
6. Nie twórz kolejnych kopii ani alternatywnych ZIP-ów.

## Walidacja i raport

1. Wykonaj `unzip -t`.
2. Zapisz pełną ścieżkę, rozmiar, SHA-256, liczbę wpisów, nazwę jedynego katalogu głównego i potwierdzenie dokładnie jednego `COMMA.App.exe`.
3. Nie uruchamiaj ponownie pełnych testów; kod wersji 5.0 został już zbudowany w zadaniu 020.
4. Sprawdź `git diff --check` i allowlistę.
5. Zaktualizuj `.ai/report.md` i `.ai/handoff.md`, odnotowując również potwierdzony przez użytkownika test aplikacji macOS 5.0.
6. Przy poprawnym ZIP ustaw `COMPLETED`, wykonaj commit i push. Nie uruchamiaj aplikacji Windows na MacBooku.
