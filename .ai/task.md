# Aktualne zadanie

- TASK_ID: PDF-FOUR-GARMENT-WINDOWS-024
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: df7234c7e797d7644df4f9f781f994d1efa1e815
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Fix four-garment PDF layout and colour text
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App/Services/Pdf/HandwrittenSection.cs", "COMMA.App/Services/Pdf/PdfStyles.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Kontynuacja zadania 023

Poprzedni przebieg pozostawił lokalne, niezatwierdzone zmiany wyłącznie w pięciu ścieżkach z allowlisty:
- `.ai/report.md`
- `.ai/handoff.md`
- `COMMA.App/ViewModels/MainViewModel.cs`
- `COMMA.App/Services/Pdf/HandwrittenSection.cs`
- `COMMA.App.Tests/OrderPdfGeneratorTests.cs`

Są to zmiany bieżącego zadania, nie cudze zmiany. Nie stashuj ich, nie resetuj, nie nadpisuj ani nie usuwaj. Przed rozpoczęciem potwierdź, że lokalny diff zawiera wyłącznie powyższe ścieżki; w takim przypadku kontynuuj pracę na tym diffie.

## Problem wymagający dokładniejszego odtworzenia

Na Windows COMMA Workspace 5.0 PDF dla zlecenia 262454 HERNIK- HAFT nie generuje się, gdy późniejsza strona ma dokładnie cztery różne pozycje odzieży, po jednym rysunku i opisie pod każdym. Pierwsza regresja użyła krótkich nazw/opisów i przeszła, więc nie odtworzyła obciążenia rzeczywistej strony.

## Cel

Utworzyć wiarygodną regresję dla układu czterech pozycji, która obejmuje zawijane tytuły i opisy; zmienić wyłącznie geometrię układu czterech pozycji, jeśli rzeczywisty generator PDF ujawni błąd. Zachować już przygotowane poprawki KOLORYSTYKI 10 pt i diagnostyki błędów.

## Wymagania

1. Przeczytaj w całości `AGENTS.md` i wszystkie pliki `.ai`. Potwierdź katalog, gałąź `workspace-4.0` i `main` dokładnie `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Nie zmieniaj COMMA WMS, KOMI, `main`, danych użytkownika, załączników, brandingu, interfejsu ani układów 1–3 pozycji.
3. Rozbuduj istniejącą regresję rzeczywistego `OrderPdfGenerator.Generate` dla późniejszej strony z czterema pozycjami: użyj czterech długich, realistycznych nazw odzieży, które zawijają się w tytule, oraz opisów pod rysunkami o różnej długości, w tym wielowierszowego opisu. Sprawdź, że druga strona ma dokładnie cztery pojedyncze rysunki i że PDF powstaje.
4. Najpierw uruchom wyłącznie tę ukierunkowaną regresję. Jeżeli przechodzi, nie wprowadzaj hipotetycznej zmiany geometrii. W raporcie powiedz jasno, że przypadek maksymalnego tekstu nadal nie odtwarza błędu zgłoszonego na PC i nie twierdź, że naprawiono problem Windows.
5. Jeżeli regresja ujawni błąd QuestPDF, popraw wyłącznie kompozycję/wysokość komórki dla czterech pozycji: tytuł pozycji, tytuł rysunku, obraz, odstępy i opis muszą mieścić się w komórce. Maksymalna wysokość rysunku 70 mm pozostaje bez zmian.
6. Zachowaj w całości już istniejącą lokalną poprawkę KOLORYSTYKI: wartość i numer 10 pt, bez `ScaleToFit` oraz bez automatycznego zmniejszania fontu, z zawijaniem wartości; zachowaj lokalną poprawkę logu błędu w `LocalApplicationData/COMMA Workspace/Logs`.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz w tym zadaniu, zapisując pełny wynik i kod wyjścia do pliku tymczasowego poza repozytorium, aby nie utracić końcowego wyniku. Następnie uruchom Release build, `git diff --check` i kontrolę allowlisty.
8. Ustaw `COMPLETED` tylko, gdy pełny test i build mają potwierdzony wynik PASS. Wtedy zaktualizuj report/handoff, wykonaj commit i push na `workspace-4.0`.
9. Jeśli test lub reprodukcja nie daje dowodu naprawy, ustaw `BLOCKED`; nie wykonuj commita/pushu ani pakietu Windows/macOS.

## Zakazy

- Nie twórz ZIP-a ani aplikacji macOS.
- Nie używaj obejścia typu zmiana folderu PDF, retry, ukrywanie błędów, reset, rebase lub force-push.
- Nie kasuj ani nie zastępuj istniejącego stash.
