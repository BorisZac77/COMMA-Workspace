# Aktualne zadanie

- TASK_ID: PDF-PERFORMANCE-026
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 537d0a0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Improve PDF generation performance
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Cel

Pracownicy zgłaszają, że generowanie PDF dla rozbudowanych kart produkcyjnych czasem trwa ponad minutę. Usprawnić wyłącznie rzeczywistą gorącą ścieżkę generatora PDF, wspólną dla macOS i Windows, bez zmiany treści, kolejności, liczby stron, jakości lub danych dokumentu.

## Wymagania

1. Przeczytaj `AGENTS.md` i wszystkie pliki `.ai`. Potwierdź worktree, gałąź `workspace-4.0`, czyste drzewo oraz `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Utwórz deterministyczny test/benchmark rozbudowanej karty, obejmujący wiele stron, pozycje, rysunki i opisy zgodne z obecnym modelem. Zmierz osobno przygotowanie dokumentu oraz zapis pliku. Pomiar umieść w raporcie, nie wprowadzaj kruchego limitu czasu do testów.
3. Zidentyfikuj faktyczną kosztowną operację. Wdrażaj wyłącznie optymalizację potwierdzoną tym pomiarem, np. eliminację wielokrotnego odczytu identycznego obrazu/danych. Nie używaj cache mogącego zwrócić nieaktualny PDF lub obraz.
4. Dodaj regresję potwierdzającą, że po optymalizacji PDF nadal powstaje, jest niepusty, ma oczekiwaną liczbę stron i poprawną kolejność pozycji.
5. Zachowaj bez zmian: układ 1–4 pozycji, limit rysunku 70 mm, poprawkę czterech pozycji z długimi tytułami, KOLORYSTYKĘ HERNIK 10 pt, logowanie błędów, UI, załączniki i format danych.
6. Uruchom regresje PDF dla: pary na pierwszej stronie, czterech pozycji z długimi tytułami, długiej KOLORYSTYKI HERNIK oraz rozbudowanej karty.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz, zapisując stdout, stderr i kod wyjścia do unikalnych plików w `/tmp` poza repozytorium. Następnie Release build, `git diff --check` i kontrolę allowlisty.
8. Ustaw `COMPLETED` i commit/push tylko po potwierdzonym PASS testów i builda. W przeciwnym razie ustaw `BLOCKED` i nie publikuj.
9. Nie twórz ZIP-a Windows ani aplikacji macOS w tym zadaniu.

## Zakazy

- Nie zmieniaj COMMA WMS, KOMI, `main`, brandingu, folderu wyjściowego PDF ani danych użytkownika.
- Nie obniżaj rozdzielczości/jakości, nie zmieniaj kompresji dla przyspieszenia.
- Nie stosuj resetu, rebase, force-push, retry ani ukrywania błędów.
