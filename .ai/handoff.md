# Stan przekazania

- TASK_ID: RELEASE-MAC-4.1-001
- STATUS: BLOCKED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe validation worker / task owner
- BRANCH: workspace-4.0
- HEAD: 610678fa41e2dd56dd475ce7bb030e3359fc1db0

## Stan

- Zmiany repozytorium ograniczają się do ścieżek z `ALLOWED_PATHS_JSON`.
- Testy końcowe: PASS, 156/156.
- Build końcowy: PASS, 0 ostrzeżeń, 0 błędów.
- Podpisany pakiet ARM64 istnieje w `/tmp/COMMA_Workspace_Build/COMMA Workspace 4.1.app` i przechodzi `codesign --verify --deep --strict`.
- Pakiet docelowy `/Users/Boris/Desktop/COMMA Workspace 4.1.app` nie istnieje, ponieważ sandbox zablokował kopiowanie.
- Aplikacja 4.0 na Pulpicie pozostała nienaruszona; porównanie stat i skrótu metadanych przed/po jest identyczne.
- Commit i push nie zostały wykonane.

## Blokady

1. Ponowić `./build_app.sh` w środowisku mającym prawo zapisu do `/Users/Boris/Desktop`, a następnie wykonać wszystkie kontrole pakietu docelowego i próbę uruchomienia.
2. Rozszerzyć `ALLOWED_PATHS_JSON` o `COMMA.App/Views/MainWindow.axaml.cs` i zmienić pozostający tam tytuł `v4.0.0` na `v4.1.0`; obecnie kod-behind nadpisuje tytuł XAML podczas działania.

## Następny krok

Po usunięciu obu blokad ponownie uruchomić pełne testy i build, utworzyć aplikację 4.1 na Pulpicie, zweryfikować Info.plist, wykonywalność, podpis oraz stabilny start, ponownie potwierdzić nienaruszenie aplikacji 4.0 i dopiero wtedy ustawić `STATUS: COMPLETED`.
