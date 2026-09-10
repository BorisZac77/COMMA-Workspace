# Aktualne zadanie

- TASK_ID: PDF-FOUR-GARMENT-WINDOWS-023
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: a7b513d9bc2636a8bfd8b43dd47fbe6fcd9e44ed
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Fix PDF four-garment layout and colour text
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App/Services/Pdf/HandwrittenSection.cs", "COMMA.App/Services/Pdf/PdfStyles.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Zgłoszenie A — PDF z czterema pozycjami

Na Windows COMMA Workspace 5.0 nie generuje PDF dla zlecenia 262454 HERNIK- HAFT, gdy na jednej stronie są dokładnie cztery różne pozycje odzieży, każda z pojedynczym rysunkiem i opisem pod nim. Podgląd strony jest widoczny, ale po GENERUJ PDF aplikacja wyświetla ogólny komunikat o błędzie. Obiecany plik Test-error.txt nie jest dostępny użytkownikowi, bo obecny kod ignoruje błąd jego zapisu.

## Zgłoszenie B — nieczytelna KOLORYSTYKA

Na przesłanym skanie karty HERNIK lista KOLORYSTYKA dla logo HERNIK (dwa długie wpisy) została zmniejszona do mikrodruku. Przyczyną jest połączenie automatycznego zmniejszania czcionki z ScaleToFit. Tekst ma być czytelny przy wydruku produkcyjnym.

## Cel

1. Usunąć przyczynę błędu wyłącznie dla układu czterech różnych pozycji na stronie.
2. Zapewnić rzetelną diagnostykę błędu PDF.
3. Przywrócić czytelną KOLORYSTYKĘ bez zmiany układu LOGOWANIE, nazw logo, wymiarów, rysunków ani pozostałych sekcji.

## Wymagania

1. Przed pracą przeczytaj AGENTS.md i wszystkie pliki .ai. Potwierdź worktree, branch workspace-4.0, czysty status oraz main dokładnie 4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a.
2. Najpierw dodaj regresję odtwarzającą stronę późniejszą PDF z czterema różnymi OrderGarmentItem, po jednym rysunku na pozycję i niepustych opisach pod każdym rysunkiem. Test ma uruchamiać rzeczywiste OrderPdfGenerator.Generate i potwierdzać niepusty PDF. Nie używaj GUI, dysku sieciowego ani plików klienta.
3. Jeżeli regresja ujawni błąd układu QuestPDF, popraw wyłącznie obliczenie/kompozycję wysokości dla czterech pól, tak aby suma: tytuł pozycji, tytuł rysunku, obraz, odstępy i opis zawsze mieściła się w przydzielonej komórce. Zachowaj istniejącą maksymalną wysokość rysunku 70 mm i nie zmieniaj układów 1–3 pozycji.
4. Jeśli regresja czterech pozycji przechodzi przed zmianą, nie wprowadzaj hipotetycznej zmiany geometrii; opisz to w raporcie.
5. Dla KOLORYSTYKI: wpisy, w tym dwa długie wpisy jak HERNIK, mają być drukowane fontem 10 pt. Usuń ScaleToFit i logikę zmniejszania fontu dla tej sytuacji; pozwól wartości zawinąć się w obrębie własnej komórki i zapewnij jej odpowiednią wysokość. Numer pozycji ma pozostać czytelny oraz wyrównany do pierwszej linii wartości.
6. Dodaj regresję generującą rzeczywisty PDF z dwoma długimi wpisami KOLORYSTYKI i potwierdzającą brak błędu układu przy 10 pt. Nie zmieniaj zasad liczby pól i nie zmniejszaj sekcji rysunków.
7. Nie zapisuj już Test-error.txt w folderze zapisu PDF. Przy błędzie zapisz raport UTF-8 z typem, komunikatem i stack trace w Environment.SpecialFolder.LocalApplicationData/COMMA Workspace/Logs, z unikalną nazwą i bez kasowania poprzednich raportów. Status UI ma wskazać pełną lokalną ścieżkę raportu albo, gdy nawet to się nie powiedzie, wyświetlić krótki typ i komunikat wyjątku. Nie wyświetlaj stack trace w UI.
8. Nie zmieniaj interfejsu, kolejności stron, danych PDF, załączników, formatu v4, brandingu, COMMA WMS, KOMI ani main.
9. Uruchom pełne dotnet test "COMMA Workspace 4.0.sln" dokładnie raz i Release build. Jeżeli sandbox zablokuje VSTest, nie ponawiaj go; zapisz dokładny wynik. Zawsze uruchom git diff --check i kontrolę allowlisty.
10. Ustaw COMPLETED tylko po pomyślnym buildzie i dostępnej walidacji. Commit/push tylko po spełnieniu warunków.

## Kryteria odbioru

- PDF dla 262454 HERNIK- HAFT z czterema różnymi pozycjami na jednej stronie ma powstać.
- Dwa długie opisy KOLORYSTYKI logo HERNIK mają być na wydruku czytelne w 10 pt, bez mikrodruku.
- Jeżeli środowisko Windows ujawni inny błąd, aplikacja ma podać realną ścieżkę do istniejącego raportu.

## Zakazy

- Nie twórz ZIP-a ani paczki macOS w tym zadaniu.
- Nie zmieniaj folderu wyjściowego PDF jako obejścia.
- Nie dodawaj retry ani ukrywania błędów.
- Nie wykonuj resetu, rebase ani force-push.
