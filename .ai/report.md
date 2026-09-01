# Raport Codexa

- TASK_ID: ATTACHMENT-PREVIEW-REORDER-007
- STATUS: COMPLETED
- STARTED_AT: 2026-09-01T22:27:00+0200
- COMPLETED_AT: 2026-09-01T22:39:33+0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 72fea9f9c9b2f1f816c83aaefedbb6ce0d3331e8
- HEAD_AFTER: 72fea9f9c9b2f1f816c83aaefedbb6ce0d3331e8

## Cel
Naprawić natychmiastowe odświeżanie widocznej kolejności załączników w otwartym oknie ZAŁĄCZNIKI po użyciu strzałek góra/dół.

## Kontrole wstępne
- `pwd` i `git rev-parse --show-toplevel` wskazały `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- Bieżąca gałąź: `workspace-4.0`.
- Worktree przed rozpoczęciem był czysty.
- HEAD `72fea9f9c9b2f1f816c83aaefedbb6ce0d3331e8` ma bezpośredniego rodzica `db1316d8e8198fd854133a48ac730b1da0f95e0f`.
- Commit kolejkujący zmienia wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.
- `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Diagnoza
- `OrderAttachmentManager.Move` poprawnie wykonuje `ObservableCollection.Move` i normalizuje pola `Order`; istniejąca regresja z trzema różnie nazwanymi załącznikami potwierdza poprawną kolejność danych w obu kierunkach i zachowanie na krawędziach.
- `AttachmentsList` pobiera kolekcję przez `ItemsSource="{Binding Attachments}"`, lecz w zgłoszonym scenariuszu otwartego okna jego istniejące kontenery nie zmieniają widocznego położenia po powiadomieniu `Move`.
- Dotychczasowe ustawienie `SelectedIndex` zmieniało indeks zaznaczenia, ale nie wymuszało przebudowy widocznego źródła elementów, więc lista mogła nadal przedstawiać starą kolejność.

## Wykonane zmiany
- Po udanym przesunięciu lista otrzymuje świeżą migawkę `card.Attachments`, co deterministycznie przebudowuje widoczną kolejność bez zamykania okna.
- Po ponownym przypięciu źródła zaznaczenie jest przywracane przez tożsamość przeniesionego obiektu; `UpdateButtonStates` oblicza stany strzałek z jego nowej widocznej pozycji.
- Warstwa danych, pola `Order`, zapis i generator PDF pozostały bez zmian.
- Dodano test kontraktu widoku sprawdzający powiązanie XAML, kolejność `Move` → odświeżenie, ponowne przypięcie migawki i przywrócenie zaznaczenia. Projekt nie zawiera Avalonia Headless, więc nie dodawano nowej infrastruktury testowej.

## Testy
- `dotnet test COMMA.App.Tests/COMMA.App.Tests.csproj --filter FullyQualifiedName~OrderAttachmentTests --no-restore -m:1` z wyłączoną telemetrią i serwerem MSBuild: projekty oraz assembly testów zbudowane poprawnie; uruchomienie przerwane przez sandbox, gdy VSTest próbował otworzyć lokalne gniazdo TCP (`SocketException (13): Permission denied`).
- `dotnet test "COMMA Workspace 4.0.sln" -m:1` z tymi samymi bezpiecznymi ustawieniami: restore aktualny, wszystkie projekty i assembly testów zbudowane; uruchomienie testów zablokowane przez identyczny zakaz lokalnego gniazda TCP VSTest.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1`: PASS, 0 ostrzeżeń, 0 błędów.
- `git diff --check`: PASS.
- Kontrola zmienionych ścieżek względem `ALLOWED_PATHS_JSON`: PASS; zmieniono wyłącznie `.ai/report.md`, `.ai/handoff.md`, `COMMA.App/Views/AttachmentsWindow.axaml.cs` i `COMMA.App.Tests/OrderAttachmentTests.cs`.

## Problemy lub ryzyka
- Sandbox uniemożliwia procesowi VSTest otwarcie lokalnego gniazda TCP, dlatego testy skompilowały się, ale nie mogły zostać wykonane. Bezpieczny worker powinien ponowić pełne `dotnet test "COMMA Workspace 4.0.sln"` w środowisku ze standardowym lokalnym IPC.
- Nie wykonywano interaktywnego uruchomienia okna GUI w sandboxie; przypadek trzech nazw jest objęty istniejącym testem danych oraz nowym testem kontraktu widoku.

## Podsumowanie
Widoczna lista załączników jest po każdym udanym przesunięciu przebudowywana z aktualnej kolejności kolekcji, a zaznaczenie i stany strzałek odnoszą się do przeniesionego elementu. Nie wykonano commit ani push.
