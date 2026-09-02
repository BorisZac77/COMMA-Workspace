# Raport Codexa

- TASK_ID: ATTACHMENT-WINDOWS-FILE-LOCK-010
- STATUS: COMPLETED
- STARTED_AT: 2026-09-02 12:03:00 +0200
- COMPLETED_AT: 2026-09-02 12:17:07 +0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 0fc28e98feb3e0066b4060e221de56bcb894a532
- HEAD_AFTER: 0fc28e98feb3e0066b4060e221de56bcb894a532

## Cel
Naprawić import załączników PDF/JPG/PNG na Windows, gdy plik jest współdzielony przez inny proces albo krótko zablokowany, bez osłabienia atomowości importu.

## Kontrole wstępne
- `pwd` i `git rev-parse --show-toplevel`: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- Gałąź: `workspace-4.0`.
- Worktree przed rozpoczęciem: czysty.
- HEAD kolejki: `0fc28e98feb3e0066b4060e221de56bcb894a532`.
- `BASE_HEAD_BEFORE_QUEUE` (`3b15ebd4ca0274527257f186629b2680afd3173d`) jest przodkiem HEAD.
- `main`: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`, bez zmian.

## Diagnoza
Potwierdzono diagnozę z zadania. `OrderAttachmentContentStore.ImportFile` otwierał źródło z `FileShare.Read`, co na Windows mogło kolidować z dostępem istniejącego procesu do zapisu lub usunięcia nawet wtedy, gdy ten proces zezwalał innym na odczyt. Brakowało też ograniczonego retry dla błędów Windows `ERROR_SHARING_VIOLATION` i `ERROR_LOCK_VIOLATION`.

## Wykonane zmiany
- Źródło jest otwierane wyłącznie do odczytu z `FileShare.ReadWrite | FileShare.Delete`, co zachowuje zgodność z procesami dopuszczającymi współdzielony odczyt, zapis i usunięcie.
- Dodano maksymalnie 3 próby otwarcia z dwoma opóźnieniami po 75 ms, wyłącznie dla kodów Windows 32 i 33. Pozostałe błędy zachowują dotychczasowe wyjątki i nie są ponawiane.
- Po trwałej blokadzie zwracany jest polski komunikat z nazwą pliku i sugestią zamknięcia programu korzystającego z pliku.
- Mechanizm importu do pliku `.part`, sprzątanie po błędzie oraz dodawanie wpisu magazynu dopiero po atomowym przeniesieniu pozostały bez zmian.
- Dodano deterministyczne testy z kontrolowaną fabryką strumienia: udany import po przejściowych blokadach, dokładne flagi współdzielenia, limit retry oraz brak plików, wpisu magazynu i metadanych po trwałej blokadzie.
- Nie zmieniono obsługi wielokrotnego wyboru, limitów, kolejności, podglądu, generatora PDF ani interfejsu poza treścią istniejącego błędu.

## Testy
- `dotnet test "COMMA Workspace 4.0.sln"` — PASS: 173/173 testy, 0 niepowodzeń, 0 pominiętych.
- `dotnet test "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS: 173/173 testy.
- Nowe testy regresji w Release — PASS: 2/2.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS: 0 ostrzeżeń, 0 błędów.
- `git diff --check` — PASS przed aktualizacją plików `.ai`; powtórzony w kontroli końcowej.
- Zmienione ścieżki — wyłącznie z `ALLOWED_PATHS_JSON`.

## Problemy lub ryzyka
Pierwsza dodatkowa próba testów bez `-m:1` została zablokowana przez sandbox przy tworzeniu named pipe MSBuild (`SocketException (13): Permission denied`). Pierwsza próba Debug z `--no-restore -m:1` ujawniła nieaktualne zasoby (`WithDeveloperTools`), a pierwszy wymagany build brak `COMMA.DrawingsGenerator/obj/project.assets.json`. Standardowy restore brakującego projektu z lokalnego cache usunął oba problemy. Następnie dokładna komenda testowa oraz wymagany build przeszły w całości; nie pozostała blokada walidacji.

## Podsumowanie
Import załączników toleruje współdzielony dostęp Windows i krótkie blokady, ale nie omija trwałej blokady wyłącznej. Trwały konflikt kończy się zrozumiałym komunikatem, a magazyn i kolekcja metadanych pozostają czyste. Nie wykonano commita ani pushu; HEAD pozostał bez zmian zgodnie z poleceniem użytkownika.
