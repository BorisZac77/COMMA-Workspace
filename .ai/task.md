# Aktualne zadanie

- TASK_ID: WORKSPACE-5-FIRST-PAGE-PAIR-LAYOUT-020
- STATUS: READY
- PROJECT: COMMA Workspace 5.0
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 11912aa4b353b399851733981195622a4d174c2c
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Start Workspace 5.0 with paired first-page garments
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "build_app.sh", "COMMA.App/COMMA.App.csproj", "COMMA.App/App.axaml", "COMMA.App/Views/MainWindow.axaml", "COMMA.App/Views/MainWindow.axaml.cs", "COMMA.App/Controls/GarmentPageSection.axaml.cs", "COMMA.App/Layout/OrderPageLayoutEngine.cs", "COMMA.App/Layout/GarmentViewDescriptionLayout.cs", "COMMA.App/Services/Pdf/OrderPdfGenerator.cs", "COMMA.App.Tests/ApplicationBrandingTests.cs", "COMMA.App.Tests/OrderPageLayoutEngineTests.cs", "COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs"]

## Cel

Rozpocząć COMMA Workspace 5.0 i poprawić pierwszą stronę karty dla dokładnie dwóch rodzajów odzieży z dokładnie jednym wybranym rzutem każdy. W tej jednej konfiguracji oba pola odzieży mają być ułożone obok siebie w dwóch równych kolumnach, aby pod każdym rysunkiem było więcej miejsca na jego opis.

## Wymagania wstępne

1. Przeczytaj w całości `AGENTS.md` i wszystkie pliki w `.ai`.
2. Potwierdź worktree `/Users/Boris/RiderProjects/COMMA Workspace 4.0`, gałąź `workspace-4.0`, czysty status, relację historii oraz `main` dokładnie `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Pracuj wyłącznie w ścieżkach z `ALLOWED_PATHS_JSON`. Nie zmieniaj COMMA WMS, KOMI, `main`, nazwy repozytorium, gałęzi ani katalogu lokalnego.

## Reguła układu 5.0

1. Zdefiniuj jedną wspólną, testowalną regułę układu, używaną przez podgląd, generator PDF oraz obliczanie geometrii opisu:
   - strona jest pierwszą stroną karty;
   - strona ma dokładnie dwa rozmieszczenia odzieży;
   - każde rozmieszczenie ma dokładnie jeden wybrany rzut.
2. Tylko przy spełnieniu wszystkich trzech warunków:
   - pierwsza odzież jest w lewej kolumnie;
   - druga odzież jest w prawej kolumnie;
   - kolumny mają równą szerokość i zachowują istniejącą przerwę;
   - oba pola wykorzystują pełną dostępną wysokość sekcji odzieży;
   - opis pozostaje bezpośrednio pod właściwym rysunkiem, wewnątrz jego kolumny.
3. Podgląd w aplikacji i wynikowy PDF muszą mieć ten sam układ.
4. Geometria używana przez edytor i walidację opisu musi odpowiadać rzeczywistej pełnej wysokości oraz połowie szerokości tej konfiguracji. Nie wolno dopuścić opisu, który później nie mieści się w PDF.
5. Wszystkie pozostałe przypadki zachowują dokładnie dotychczasowy układ:
   - dwie odzieże, gdy choć jedna ma więcej niż jeden rzut — nadal pionowo;
   - dwie odzieże na późniejszej stronie — nadal pionowo;
   - układy jednej, trzech i czterech odzieży — bez zmian;
   - paginacja, kolejność odzieży i rzutów — bez zmian.

## Nazwa i wersja aplikacji

1. Ustaw widoczną nazwę aplikacji na `COMMA Workspace 5.0`.
2. Ustaw tytuł okna na `COMMA Workspace — v5.0.0`.
3. Ustaw wersje projektu:
   - `Version` i `InformationalVersion`: `5.0.0`;
   - `AssemblyVersion` i `FileVersion`: `5.0.0.0`.
4. Zaktualizuj skrypt macOS tak, aby przyszły pakiet nazywał się `COMMA Workspace 5.0.app` i miał spójne nazwy oraz wersję `5.0.0`.
5. Nie zmieniaj wersji formatu danych, manifestu COMMA PDF v4, kompatybilności odczytu ani struktury istniejących kart. To jest wyłącznie wersja aplikacji.

## Testy

Dodaj lub zaktualizuj deterministyczne testy, które potwierdzają:

1. Wspólna reguła zwraca `true` wyłącznie dla pierwszej strony z dwiema odzieżami po jednym rzucie.
2. Geometria specjalnego układu ma pełną wysokość sekcji i połowę szerokości z uwzględnieniem przerwy; obie pozycje mają równą geometrię.
3. Podgląd tworzy dwie kolumny w specjalnym przypadku, a nie dwa wiersze.
4. PDF umieszcza dwie odzieże obok siebie i zachowuje pod każdym rysunkiem właściwy opis.
5. Negatywne przypadki pozostają pionowe i bez regresji.
6. Branding, tytuł okna, wersje projektu i przyszła nazwa macOS app są spójnie ustawione na 5.0.

## Walidacja i zakończenie

1. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln" --no-restore` dokładnie raz oraz Release build rozwiązania. Jeśli runner zostanie obiektywnie zablokowany przez sandbox, nie ponawiaj ślepo; zapisz dokładny wynik i wykonaj dostępne kompilacje Release.
2. Sprawdź `git diff --check` oraz ścisłą allowlistę.
3. Zaktualizuj `.ai/report.md` i `.ai/handoff.md`.
4. Przy pomyślnej dostępnej walidacji ustaw `COMPLETED`, wykonaj commit i push na `workspace-4.0`.
5. Nie twórz jeszcze ZIP-a Windows ani aplikacji macOS. Pakowanie nastąpi dopiero po przeglądzie wyniku tej zmiany.
