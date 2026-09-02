# Raport Codexa

- TASK_ID: ATTACHMENT-WINDOWS-RAW-IO-011
- STATUS: COMPLETED
- STARTED_AT: 2026-09-02 12:20:00 +0200
- COMPLETED_AT: 2026-09-02 12:48:12 +0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 084c0436530653de2b6f1911472bb867728b9f01
- HEAD_AFTER: 084c0436530653de2b6f1911472bb867728b9f01

## Cel
Usunąć surowy komunikat Windows z importu `Vandeputte-10.pdf`, objąć ograniczonym retry także błąd blokady podczas odczytu źródła i zachować atomowe sprzątanie nieudanego importu.

## Kontrole wstępne
- `pwd` i `git rev-parse --show-toplevel`: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- Gałąź: `workspace-4.0`.
- Worktree przed rozpoczęciem: czysty.
- HEAD: `084c0436530653de2b6f1911472bb867728b9f01`.
- `BASE_HEAD_BEFORE_QUEUE` (`0be9283cb79ca31baf3eab705ed8c90edd5c2e26`) jest przodkiem HEAD.
- `main`: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`, bez zmian.

## Diagnoza
- `OrderAttachmentManager.AddFile` pobierał `new FileInfo(filePath).Length` przed wejściem do chronionego importu. Ta dodatkowa operacja na wybranym pliku mogła przekazać surowy `IOException` do `AddFiles`.
- `OrderAttachmentContentStore.OpenSourceWithRetry` ponawiał wyłącznie samo otwarcie. `IOException` o kodzie Windows 32 lub 33 zgłoszony później przez `source.Read(...)` uruchamiał sprzątanie `.part`, ale nie retry ani polskiego komunikatu.
- Kopiowanie do `.part`, zapis skrótu, flush, atomowe przeniesienie, rejestracja magazynowej ścieżki, walidacja kopii i utworzenie metadanych odbywały się w poprawnej kolejności. Luka dotyczyła granic retry i prezentacji błędu, nie końcowej atomowości.

## Wykonane zmiany
- Usunięto wstępny odczyt `FileInfo.Length`. Limit pojedynczego pliku nadal jest egzekwowany podczas kopiowania, a limit sumy jest sprawdzany na faktycznej długości zakończonego importu. W razie przekroczenia zawartość jest usuwana przed utworzeniem metadanych.
- Ograniczone retry (maksymalnie 3 próby, dwa opóźnienia po 75 ms) obejmuje teraz pełną próbę: otwarcie źródła, odczyt i atomowy import. Ponawiane są tylko błędy sharing/lock Windows 32 i 33.
- Po trwałej blokadzie zwracany jest polski komunikat z nazwą pliku i poleceniem zamknięcia programu korzystającego z pliku. Pozostałe `IOException` mają odrębny polski komunikat o błędzie wejścia/wyjścia.
- `AddFiles` normalizuje każdy pozostały `IOException` przed przekazaniem treści do okna, więc surowy tekst systemowy nie jest prezentowany użytkownikowi.
- Dodano deterministyczną regresję `AddFiles`: kontrolowany strumień zapisuje fragment `.part`, następnie zgłasza dokładny `ERROR_SHARING_VIOLATION`; test weryfikuje 3 próby, pełną polską treść komunikatu i brak `.part`, magazynowej zawartości oraz metadanych.
- Nie zmieniono formatu karty, generatora PDF, limitów, kolejności, podglądu ani wyglądu interfejsu.

## Walidacja
- `AVALONIA_TELEMETRY_OPTOUT=1 dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń, 0 błędów; skompilowano również `COMMA.App.Tests`.
- `dotnet test "COMMA Workspace 4.0.sln"` — BLOCKED przez sandbox przed uruchomieniem testów: MSBuild nie może utworzyć named pipe (`System.Net.Sockets.SocketException (13): Permission denied`).
- Próba jednowęzłowa testów Release z wyłączoną telemetrią — test assembly zbudowana poprawnie, następnie BLOCKED przez sandbox: VSTest nie może otworzyć lokalnego `TcpListener` (`System.Net.Sockets.SocketException (13): Permission denied`). Żaden test nie został uruchomiony.
- Pierwsza próba jednowęzłowa bez opt-out telemetrii była dodatkowo zablokowana zapisem Avalonia poza workspace do `~/Library/Application Support/AvaloniaUI/BuildServices/buildtasks.log`; użycie oficjalnego `AVALONIA_TELEMETRY_OPTOUT=1` usunęło tę blokadę bez rozszerzania uprawnień.
- `git diff --check` — PASS przed aktualizacją raportu; powtórzony w kontroli końcowej.
- Zmienione ścieżki — wyłącznie z `ALLOWED_PATHS_JSON`.

## Problemy lub ryzyka
Sandbox uniemożliwia komunikację procesów VSTest zarówno przez named pipe, jak i lokalne gniazdo TCP. Kod produkcyjny i testy kompilują się w Release, ale testy wymagają ponownego uruchomienia przez safe validation worker w środowisku zezwalającym VSTest na lokalną komunikację.

## Podsumowanie
Retry obejmuje teraz błąd blokady występujący podczas właściwego odczytu, a nie tylko otwarcia źródła. Nieudana próba usuwa częściowy plik i stan magazynu, a okno otrzymuje polski komunikat zależny od rodzaju błędu. Nie wykonano commita ani pushu zgodnie z poleceniem użytkownika.
