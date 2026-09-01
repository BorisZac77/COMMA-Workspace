# Aktualne zadanie

- TASK_ID: RELEASE-MAC-4.1-001
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Release COMMA Workspace 4.1 for macOS
- ALLOWED_PATHS_JSON: ["COMMA.App/App.axaml", "COMMA.App/COMMA.App.csproj", "COMMA.App/Views/MainWindow.axaml", "COMMA.App.Tests/ApplicationBrandingTests.cs", "build_app.sh", ".ai/report.md", ".ai/handoff.md"]

## Cel
Przygotować wersję COMMA Workspace 4.1.0 zawierającą aktualny kod gałęzi `workspace-4.0`, zbudować osobny pakiet macOS ARM64 i umieścić go na Pulpicie jako `COMMA Workspace 4.1.app`. Istniejąca aplikacja 4.0 ma pozostać bez zmian.

## Dozwolone zmiany w repozytorium
- `COMMA.App/App.axaml`
- `COMMA.App/COMMA.App.csproj`
- `COMMA.App/Views/MainWindow.axaml`
- `COMMA.App.Tests/ApplicationBrandingTests.cs`
- `build_app.sh`
- `.ai/report.md`
- `.ai/handoff.md`

Nie wolno zmieniać innych plików ani funkcjonalności aplikacji.

## Wymagania wersji
1. Zmień widoczną nazwę aplikacji na `COMMA Workspace 4.1`.
2. Ustaw tytuł głównego okna na `COMMA Workspace — v4.1.0`.
3. Ustaw metadane projektu:
   - `Version=4.1.0`
   - `AssemblyVersion=4.1.0.0`
   - `FileVersion=4.1.0.0`
   - `InformationalVersion=4.1.0`
4. Zaktualizuj test brandingu tak, aby jednoznacznie weryfikował wersję 4.1 we wszystkich powyższych miejscach.
5. Zaktualizuj `build_app.sh`, aby budował `COMMA Workspace 4.1.app` i wpisywał do Info.plist nazwę 4.1 oraz wersję 4.1.0.
6. Skrypt ma dotykać na Pulpicie wyłącznie celu `$HOME/Desktop/COMMA Workspace 4.1.app`. Nie wolno usuwać, zastępować, przenosić ani zmieniać `COMMA Workspace 4.0.app` ani innych aplikacji i plików.
7. Nie zmieniaj nazwy repozytorium, katalogu roboczego, solution ani gałęzi. Rozwój nadal odbywa się na `workspace-4.0`.

## Testy i budowanie
1. Wykonaj:
   - `dotnet test "COMMA Workspace 4.0.sln" -c Release`
   - `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore`
2. Uruchom zaktualizowany `build_app.sh` i utwórz samodzielny pakiet macOS ARM64.
3. Zweryfikuj na gotowym pakiecie:
   - istnieje `$HOME/Desktop/COMMA Workspace 4.1.app`,
   - istnieje wykonywalny `Contents/MacOS/COMMA.App`,
   - Info.plist ma `CFBundleName` i `CFBundleDisplayName` równe `COMMA Workspace 4.1`,
   - `CFBundleVersion` i `CFBundleShortVersionString` są równe `4.1.0`,
   - pakiet przechodzi `codesign --verify --deep --strict`,
   - aplikacja daje się uruchomić bez natychmiastowego błędu.
4. Potwierdź, że wcześniejsza aplikacja `$HOME/Desktop/COMMA Workspace 4.0.app`, jeśli istnieje, pozostała nienaruszona.
5. Usuń z repozytorium wyłącznie ignorowane artefakty builda utworzone przez zadanie, jeśli skrypt nie zrobi tego sam. Nie usuwaj danych użytkownika.

## Raport
1. Zaktualizuj `.ai/report.md` i `.ai/handoff.md`.
2. Podaj wyniki testów, builda, ścieżkę pakietu, metadane Info.plist, weryfikację podpisu, wynik uruchomienia i potwierdzenie zachowania aplikacji 4.0.
3. Ustaw `STATUS: COMPLETED` tylko jeśli osobna aplikacja 4.1 rzeczywiście istnieje na Pulpicie i przeszła kontrole. Jeśli sandbox blokuje zapis na Pulpicie, ustaw `STATUS: BLOCKED`, zachowaj bezpiecznie stan i dokładnie opisz blokadę.
4. Nie wykonuj samodzielnie operacji Git. Commit i push wykona worker po walidacji allowlisty.
