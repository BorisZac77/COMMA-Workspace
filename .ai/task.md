# Aktualne zadanie

- TASK_ID: PDF-PAIRED-FIRST-PAGE-AND-PERFORMANCE-025
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: edff323924507622efe68ef31c939f8c0c02584b
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Stabilize PDF layout and generation performance
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App/Services/Pdf/HandwrittenSection.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Kontynuacja

W lokalnym drzewie istnieje dozwolony, niezatwierdzony diff z zadań 023 i 024 w ścieżkach allowlisty. Obejmuje poprawkę KOLORYSTYKI 10 pt, logów błędów PDF, układu czterech pozycji oraz regresje. Nie stashuj, nie resetuj, nie kasuj ani nie nadpisuj tego diffu. Najpierw potwierdź, że lokalne zmiany ograniczają się do allowlisty, a następnie pracuj na nich.

## A — walidacja dwóch pozycji na pierwszej stronie

Pełne testy po poprawce czterech pozycji: 183 PASS / 1 FAIL. Nieudany istniejący test:
`Pdf_PairedFirstPageSingleViewGarmentsUseEqualSideBySideColumns`.

1. Najpierw uruchom wyłącznie ten test i zbierz faktyczne współrzędne/wymiary wynikowego PDF. Nie zmieniaj kodu przed diagnozą.
2. Jeżeli test przejdzie przy ponownym uruchomieniu, nie poluzowuj arbitralnie progu. Ustal stabilną, semantyczną asercję równych kolumn opartą na rzeczywistych granicach PDF, która wykrywa nierówne kolumny, ale nie zależy od kruchego błędu ekstrakcji lub zaokrągleń.
3. Jeżeli test ponownie nie przejdzie, ustal czy pierwsza strona dwóch pozycji jest faktycznie nierówna. Napraw wyłącznie ten układ w `OrderPdfGenerator.cs`, jeśli jest wadliwy. Nie dotykaj układów 1, 3 ani 4 pozycji ani limitu rysunku 70 mm.
4. Nie zwiększaj progu liczbowego bez uzasadnienia geometrią PDF. Nie wyciszaj ani nie usuwaj testu.

## B — zbyt długie generowanie rozbudowanych kart

Pracownicy zgłaszają, że generowanie PDF dla rozbudowanych kart bywa bardzo długie, czasem ponad minutę. Celem jest skrócenie czasu generowania na macOS i Windows bez zmiany treści, kolejności, liczby stron, jakości, danych, załączników ani zachowania interfejsu.

1. Najpierw utwórz deterministyczny scenariusz testowy rozbudowanej karty: wiele pozycji/stron, opisy i rysunki zgodne z obecnym publicznym modelem. Zmierz osobno czas przygotowania dokumentu oraz zapisania pliku. Raportuj wynik diagnostycznie; nie wprowadzaj kruchego limitu czasowego jako testu CI.
2. Przeanalizuj kod na gorące ścieżki. Dopuszczalne są tylko poprawki oparte na pomiarze, np. uniknięcie wielokrotnego odczytu tego samego obrazu/danych lub nadmiarowych obliczeń. Nie stosuj cache, który może pokazać nieaktualny PDF lub obraz; nie zmieniaj rozdzielczości, kompresji, limitu 70 mm, kolejności stron ani semantyki błędów.
3. Dodaj regresję zachowującą równoważność wyniku: PDF powstaje, jest niepusty, ma oczekiwaną liczbę stron oraz ten sam porządek pozycji. W raporcie podaj pomiar przed/po lub wprost napisz, że pomiar nie wykazał bezpiecznej optymalizacji.
4. Zadbaj, aby rozwiązanie było wspólne dla macOS i Windows; nie dodawaj zachowania zależnego od platformy.

## Obowiązkowe regresje

- dwie pozycje na pierwszej stronie;
- cztery pozycje z długimi tytułami;
- długie wpisy HERNIK KOLORYSTYKI;
- scenariusz rozbudowanej karty.

## Walidacja

1. Przeczytaj `AGENTS.md` i wszystkie pliki `.ai`; potwierdź worktree, `workspace-4.0` i `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz w tym zadaniu, zapisując stdout, stderr i kod wyjścia do unikalnych plików w `/tmp` poza repozytorium. Następnie Release build, `git diff --check` i kontrola allowlisty.
3. Jeśli pełne testy i build są PASS, ustaw `COMPLETED`, uaktualnij report/handoff, wykonaj commit i push. Jeżeli nie, ustaw `BLOCKED`, nie commituj i nie twórz pakietu.
4. Nie twórz ZIP-a Windows ani aplikacji macOS w tym zadaniu.

## Zakazy

- Nie zmieniaj COMMA WMS, KOMI, `main`, brandingu, interfejsu, formatów danych lub załączników.
- Nie stosuj resetu, rebase, force-push, obejść folderem zapisu, retry błędów biznesowych, ukrywania błędów ani automatycznego otwierania PDF.
