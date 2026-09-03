# Aktualne zadanie

- TASK_ID: PACKAGE-MACOS-WORKSPACE-5.0-021
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package COMMA Workspace 5.0 for macOS
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Zbudować podpisaną, samodzielną aplikację macOS arm64 z bieżącego kodu wersji 5.0 i umieścić ją na Pulpicie użytkownika jako:

`/Users/Boris/Desktop/COMMA Workspace 5.0.app`

Aplikacja służy do ręcznego testu nowego układu dwóch rodzajów odzieży po jednym rzucie na pierwszej stronie.

## Wymagania wstępne

1. Przeczytaj w całości `AGENTS.md` i wszystkie pliki w `.ai`.
2. Potwierdź worktree `/Users/Boris/RiderProjects/COMMA Workspace 4.0`, gałąź `workspace-4.0`, czysty status oraz `main` dokładnie `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Potwierdź, że commit `a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed` jest bieżącym HEAD albo jego przodkiem.
4. Nie zmieniaj kodu, testów, skryptów ani konfiguracji. Dozwolone zmiany repozytorium to wyłącznie raport i handoff.
5. Jeżeli docelowa ścieżka `/Users/Boris/Desktop/COMMA Workspace 5.0.app` już istnieje, nie usuwaj jej i nie nadpisuj; ustaw `BLOCKED` i podaj dokładny stan.

## Budowanie i kopiowanie

1. Zbuduj `COMMA.App` jako Release, `osx-arm64`, self-contained.
2. Utwórz bundle dokładnie `COMMA Workspace 5.0.app`, z ikoną i wykonywalnym `Contents/MacOS/COMMA.App`.
3. `Info.plist` musi zawierać:
   - `CFBundleName` i `CFBundleDisplayName`: `COMMA Workspace 5.0`;
   - `CFBundleVersion` i `CFBundleShortVersionString`: `5.0.0`;
   - istniejący identyfikator `com.comma.workspace`.
4. Podpisz ad-hoc przez `codesign --force --deep --sign -`.
5. Skopiuj wyłącznie nowo utworzony bundle do `/Users/Boris/Desktop/COMMA Workspace 5.0.app`.
6. Nie usuwaj ani nie podmieniaj wcześniejszych wersji aplikacji, backupów ani innych plików na Pulpicie.
7. Jeżeli sandbox zablokuje zapis na Pulpicie, pozostaw kompletny bundle w stałej ścieżce:
   `/tmp/COMMA_Workspace_5.0_Handoff/COMMA Workspace 5.0.app`
   i ustaw `BLOCKED` z dokładną przyczyną. Nie wykonuj kolejnych prób kopiowania.

## Walidacja

1. Potwierdź istnienie i wykonywalność `Contents/MacOS/COMMA.App`.
2. Odczytaj i zweryfikuj wszystkie wymagane wartości `Info.plist`.
3. Wykonaj `codesign --verify --deep --strict` na finalnym bundle.
4. Potwierdź marker self-contained w zawartości publikacji.
5. Sprawdź `git diff --check` oraz allowlistę.
6. Nie uruchamiaj ponownie pełnych testów — kod został już zbudowany w zadaniu 020.
7. Nie twórz ZIP-a Windows ani ZIP-a macOS.
8. Zaktualizuj `.ai/report.md` i `.ai/handoff.md`. Przy powodzeniu ustaw `COMPLETED`, wykonaj commit/push. Przy blokadzie Pulpitu ustaw `BLOCKED` i zachowaj bundle tymczasowy do pojedynczego ręcznego kopiowania.
