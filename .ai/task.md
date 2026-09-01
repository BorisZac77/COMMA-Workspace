# Aktualne zadanie

- TASK_ID: RELEASE-MAC-4.1-RECOVERY-002
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Complete COMMA Workspace 4.1 runtime branding
- ALLOWED_PATHS_JSON: ["COMMA.App/Views/MainWindow.axaml.cs", "COMMA.App.Tests/ApplicationBrandingTests.cs", ".ai/report.md", ".ai/handoff.md"]

## Cel
Dokończyć branding runtime wersji COMMA Workspace 4.1 po bezpiecznym checkpoincie. Nie twórz pakietu na Pulpicie w sandboxie; końcowe uruchomienie istniejącego skryptu odbędzie się poza workerem.

## Dozwolone zmiany
- `COMMA.App/Views/MainWindow.axaml.cs`
- `COMMA.App.Tests/ApplicationBrandingTests.cs`
- `.ai/report.md`
- `.ai/handoff.md`

Nie wolno zmieniać innych plików ani funkcjonalności.

## Wymagania
1. W `COMMA.App/Views/MainWindow.axaml.cs` zmień tytuł nadpisywany w czasie działania z `COMMA Workspace — v4.0.0` na `COMMA Workspace — v4.1.0`.
2. Rozszerz istniejący test brandingu tak, aby sprawdzał także tytuł runtime w `MainWindow.axaml.cs` i wykluczał pozostawienie tam wersji 4.0.
3. Nie zmieniaj działania aplikacji poza numerem wersji w tytule.
4. Nie uruchamiaj `build_app.sh` i nie próbuj pisać na Pulpit; worker nie ma do tego uprawnień.

## Szybka walidacja
1. Uruchom testy dotyczące `ApplicationBrandingTests`.
2. Uruchom `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore`.
3. Uruchom `git diff --check`.
4. W raporcie odnotuj wcześniejszy pełny wynik checkpointu: 156/156 PASS oraz aktualny wynik szybkiej walidacji. Nie przedstawiaj wcześniejszego wyniku jako nowego przebiegu.
5. Ustaw `STATUS: COMPLETED` w raporcie i handoff tylko jeśli zmiana, test brandingu i build przejdą poprawnie.

## Następny etap poza workerem
Po commicie i pushu właściciel uruchomi istniejący `build_app.sh` w zwykłym Terminalu, aby bezpiecznie utworzyć `/Users/Boris/Desktop/COMMA Workspace 4.1.app`. Skrypt ma pozostawić wersję 4.0 bez zmian.

## Git
Nie wykonuj commit ani push. Bezpieczny worker zrobi to po walidacji allowlisty.
