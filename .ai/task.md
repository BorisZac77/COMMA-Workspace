# Aktualne zadanie

- TASK_ID: DRAWING-DESCRIPTION-LAYOUT-004
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- EXPECTED_HEAD_AT_QUEUE_START: 6ec58939ef6992bdb64d964e53d0e23bd3816826
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Preserve description capacity with 70 mm drawings
- ALLOWED_PATHS_JSON: ["COMMA.App/Layout/GarmentViewDescriptionLayout.cs", "COMMA.App/Services/Pdf/PdfStyles.cs", "COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs", ".ai/report.md", ".ai/handoff.md"]

## Cel
Naprawić wyłącznie regresję układu opisu wykrytą po zadaniu CARD-DATA-DRAWING-HEIGHT-003. Zachować:
- maksymalną wysokość każdego rysunku 70 mm dla 1, 2, 3 i 4 rzutów,
- proporcje obrazów,
- wspólną geometrię podglądu i PDF,
- czcionkę opisów 10 pt,
- wcześniej działający limit/układ opisu dla drugiej strony z jednym rysunkiem: przykład Plopsa ma pozostać trzema pełnymi liniami w lewej komórce.

## Potwierdzone wyniki testów
Pełny test uruchomiony poza sandboxem po commicie 6ec5893:
- 167 testów,
- 165 PASS,
- 2 FAIL,
- 0 pominiętych.

Nieprzechodzące testy:
1. `GarmentViewDescriptionLayoutTests.PlopsaSecondPageFrontDescriptionUsesThreeFullLeftCellLines`
2. `OrderPdfGeneratorTests.PlopsaSecondPageFrontDescriptionUsesThreeLinesInsideLeftCell`

W drugim teście oczekiwano 3 linii, a otrzymano 10.

## Wymagania implementacyjne
1. Ustal dokładną przyczynę, dla której zmniejszenie obrazu do 70 mm zwiększyło dopuszczalną pojemność opisu z 3 do 10 linii.
2. Napraw przyczynę w geometrii/obliczeniu pojemności opisu. Nie rozwiązuj problemu przez zmianę oczekiwań testów z 3 na 10, usunięcie testów ani specjalny warunek dla nazwy Plopsa.
3. Zwolnione miejsce po zmniejszeniu obrazu nie może samoczynnie zwiększać historycznego limitu opisu. Jeśli trzeba, rozdziel limit renderowanej wysokości obrazu od referencyjnej przestrzeni używanej do walidacji tekstu.
4. PDF i podgląd mają pozostać zgodne.
5. Nie zmieniaj naprawy kopiowania LOGOWANIE/KOLORYSTYKA w `MainViewModel.cs`.
6. Nie zmieniaj układu stron, ramek, nagłówków, czcionek ani innych funkcji.

## Testy regresyjne
1. Najpierw uruchom dwa wskazane testy.
2. Zachowaj testy limitu 70 mm dla wszystkich wariantów 1–4 rzutów.
3. Jeśli istniejące testy nie wyrażają jasno rozdzielenia limitu obrazu od pojemności opisu, dodaj minimalny test regresyjny.
4. Następnie uruchom pełne `dotnet test "COMMA Workspace 4.0.sln" --no-restore -m:1`.
5. Uruchom `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1`.
6. Uruchom `git diff --check`.
7. Sprawdź, że wszystkie zmienione ścieżki należą do `ALLOWED_PATHS_JSON`.
8. Potwierdź, że `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

Jeżeli VSTest ponownie zostanie zablokowany przez sandbox na lokalnym `TcpListener`, nie oznaczaj testów jako PASS. Udokumentuj blokadę, ale nadal wykonaj analizę, minimalną naprawę, build oraz kontrole statyczne.

## Zakazy
- Nie zmieniaj gałęzi `main`.
- Nie zmieniaj COMMA WMS ani KOMI Animation Lab.
- Nie uruchamiaj `build_app.sh`, nie zapisuj niczego na Pulpit i nie podmieniaj aplikacji 4.1.
- Nie dodawaj nowych funkcji.
- Nie wykonuj resetu, rebase ani force push.

## Git
Nie wykonuj commit ani push bezpośrednio. Bezpieczny worker zrobi to tylko po walidacji allowlisty i statusie `COMPLETED`.
