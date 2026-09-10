# Aktualne zadanie

- TASK_ID: PDF-FOUR-GARMENT-WINDOWS-023
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Fix four-garment PDF generation diagnostics
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Zgłoszenie

Na Windows COMMA Workspace 5.0 nie generuje PDF dla zlecenia 262454 HERNIK- HAFT, gdy na jednej stronie są dokładnie cztery różne pozycje odzieży, każda z pojedynczym rysunkiem i opisem pod nim. Podgląd strony jest widoczny, ale po GENERUJ PDF aplikacja wyświetla ogólny komunikat o błędzie. Obiecany plik `Test-error.txt` nie jest dostępny użytkownikowi, bo obecny kod ignoruje błąd jego zapisu.

## Cel

Usunąć przyczynę błędu wyłącznie dla układu czterech różnych pozycji na stronie oraz zapewnić, że w razie nieoczekiwanego błędu dokładna diagnostyka jest zapisana niezawodnie w lokalnym katalogu danych aplikacji Windows i wskazana użytkownikowi.

## Wymagania

1. Przed pracą przeczytaj AGENTS.md i wszystkie pliki .ai. Potwierdź worktree, branch `workspace-4.0`, czysty status oraz `main` dokładnie `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Najpierw dodaj regresję odtwarzającą stronę późniejszą PDF z czterema różnymi `OrderGarmentItem`, po jednym rysunku na pozycję i niepustych opisach pod każdym rysunkiem. Test ma uruchamiać rzeczywiste `OrderPdfGenerator.Generate` i potwierdzać niepusty PDF. Nie używaj GUI, dysku sieciowego ani plików klienta.
3. Jeżeli regresja ujawni błąd układu QuestPDF, popraw wyłącznie obliczenie/kompozycję wysokości dla czterech pól, tak aby suma: tytuł pozycji, tytuł rysunku, obraz, odstępy i opis zawsze mieściła się w przydzielonej komórce. Zachowaj istniejącą wielkość rysunku 70 mm jako maksimum i nie zmieniaj innych układów 1–3 pozycji.
4. Jeśli test przechodzi przed zmianą, nie wprowadzaj hipotetycznej zmiany geometrii. Zamiast tego ogranicz implementację do niezawodnej diagnostyki opisanej w pkt 5 i opisz w raporcie, że błąd wymaga ponownego testu na PC z widocznym wyjątkiem.
5. Nie zapisuj już `Test-error.txt` w folderze zapisu PDF. Przy błędzie zapisz raport UTF-8 z typem, komunikatem i stack trace w dedykowanym katalogu `Environment.SpecialFolder.LocalApplicationData/COMMA Workspace/Logs`, z unikalną nazwą i bez kasowania poprzednich raportów. Status UI ma wskazać pełną lokalną ścieżkę raportu albo — gdy nawet to się nie powiedzie — wyświetlić krótki typ i komunikat wyjątku. Nie wyświetlaj stack trace w UI.
6. Nie zmieniaj interfejsu, kolejności stron, danych PDF, załączników, formatu v4, brandingu, COMMA WMS, KOMI ani `main`.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz i Release build. Jeżeli sandbox zablokuje VSTest, nie ponawiaj go; zapisz dokładny wynik. Zawsze uruchom `git diff --check` i kontrolę allowlisty.
8. Ustaw COMPLETED tylko po pomyślnym buildzie i dostępnej walidacji. Commit/push tylko po spełnieniu warunków.

## Kryterium odbioru

Po instalacji nowej paczki Windows użytkownik ponownie generuje PDF dla 262454 HERNIK- HAFT z czterema różnymi pozycjami i opisami pod rysunkami. PDF ma powstać. Jeżeli środowisko Windows ujawni inny błąd, aplikacja ma podać realną ścieżkę do raportu, który faktycznie istnieje.

## Zakazy

- Nie twórz ZIP-a ani paczki macOS w tym zadaniu.
- Nie zmieniaj folderu wyjściowego PDF jako obejścia.
- Nie dodawaj retry ani ukrywania błędów.
- Nie wykonuj resetu, rebase ani force-push.
