# Aktualne zadanie

- TASK_ID: PDF-PAIRED-FIRST-PAGE-VALIDATION-025
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 2a63b847b34271f1dc29f5e1dc27c1a1211bb0a7
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Validate paired page layout with four-garment PDF fix
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App/Services/Pdf/HandwrittenSection.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Kontynuacja

W lokalnym drzewie istnieje dozwolony, niezatwierdzony diff z zadań 023 i 024 w ścieżkach allowlisty. Obejmuje:
- poprawkę KOLORYSTYKI 10 pt i logów błędów PDF;
- poprawkę układu wyłącznie dla czterech pozycji z zawijanymi tytułami;
- regresje PDF.

Nie stashuj, nie resetuj, nie kasuj ani nie nadpisuj tego diffu. Potwierdź najpierw, że lokalne zmiany ograniczają się do allowlisty, a następnie pracuj na nich.

## Problem walidacyjny

Po celowanej poprawce czterech pozycji pełny `dotnet test` miał wynik 183 PASS / 1 FAIL. Nieudany jest istniejący test:
`Pdf_PairedFirstPageSingleViewGarmentsUseEqualSideBySideColumns`.

Dotyczy on pierwszej strony z dokładnie dwiema pozycjami po jednym rysunku — innego układu niż naprawiane cztery pozycje. Test raportuje odchylenie wyrównania 2,915 pt względem limitu 1,25 pt. Nie wolno uznać czteropozycyjnej poprawki za wydaną, dopóki nie zostanie wyjaśnione, czy to prawdziwa regresja, błąd testu czy niestabilność pomiaru.

## Cel

Rzetelnie zbadać i naprawić albo poprawnie ustabilizować walidację pierwszej strony z dwiema pozycjami, bez cofania poprawek czterech pozycji, KOLORYSTYKI i diagnostyki. Dbać o oba systemy: macOS i Windows korzystają ze wspólnego generatora PDF.

## Wymagania

1. Przeczytaj `AGENTS.md` i wszystkie pliki `.ai`; potwierdź worktree, `workspace-4.0` i `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Najpierw uruchom wyłącznie test `Pdf_PairedFirstPageSingleViewGarmentsUseEqualSideBySideColumns` i zbierz faktyczne współrzędne/wymiary wynikowego PDF. Nie zmieniaj kodu przed tą diagnozą.
3. Jeżeli test przejdzie przy ponownym uruchomieniu, potraktuj to jako niestabilność: nie poluzowuj arbitralnie progu. Ustal stabilną, semantyczną asercję geometrii równych kolumn, opartą na rzeczywistych granicach, która wykrywa nierówne kolumny, ale nie zależy od kruchego błędu ekstrakcji/zaokrągleń renderera.
4. Jeżeli test ponownie nie przejdzie, ustal, czy pierwsza strona dwóch pozycji jest faktycznie nierówna. Napraw wyłącznie ten układ w `OrderPdfGenerator.cs`, jeśli jest wadliwy. Nie dotykaj układów 1, 3 ani 4 pozycji, limitu rysunku 70 mm, danych zlecenia ani interfejsu.
5. Nie zwiększaj progu liczbowego bez uzasadnienia geometrią PDF. Nie wyciszaj i nie usuwaj testu.
6. Obowiązkowo uruchom regresje: dwie pozycje na pierwszej stronie, cztery pozycje z długimi tytułami, długie wpisy HERNIK KOLORYSTYKI.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz w tym zadaniu, zapisując stdout, stderr i kod wyjścia do unikalnych plików w `/tmp` poza repozytorium. Następnie uruchom Release build, `git diff --check` oraz kontrolę allowlisty.
8. Jeśli pełne testy i build są PASS, ustaw `COMPLETED`, uaktualnij report/handoff, wykonaj commit i push. Jeżeli nie, ustaw `BLOCKED`, nie commituj i nie twórz pakietu.
9. Nie twórz ZIP-a Windows ani aplikacji macOS w tym zadaniu.

## Zakazy

- Nie zmieniaj COMMA WMS, KOMI, `main`, brandingu, formatów danych lub załączników.
- Nie stosuj resetu, rebase, force-push, obejść folderem zapisu, retry błędów biznesowych ani ukrywania błędów.
