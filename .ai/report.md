# Raport Codexa

- TASK_ID: ATTACHMENT-PREVIEW-PAGE-ORDER-009
- STATUS: COMPLETED
- STARTED_AT: 2026-09-01 23:01:00 +0200
- COMPLETED_AT: 2026-09-01 23:13:14 +0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 8aa66c6125b853bd94a9b0d306ee70f3519957ae
- HEAD_AFTER: 8aa66c6125b853bd94a9b0d306ee70f3519957ae

## Cel
Naprawić kolejność fizycznych stron załączników w podglądzie aplikacji po zmianie kolejności strzałkami, bez zmiany poprawnej kolejności PDF.

## Kontrole wstępne
- `pwd` i `git rev-parse --show-toplevel`: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- Gałąź: `workspace-4.0`.
- Worktree przed rozpoczęciem: czysty.
- HEAD kolejki: `8aa66c6125b853bd94a9b0d306ee70f3519957ae`.
- Rodzic HEAD: `33bd6a38935fc92bdb4ffe3a4e5b2a7b2eff41ed`, zgodny z `BASE_HEAD_BEFORE_QUEUE`.
- Commit kolejkujący zmieniał wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.
- `main`: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`, bez zmian.

## Diagnoza
Potwierdzono diagnozę z zadania. `ObservableCollection.Move` wywołuje `CollectionChanged` przed `OrderAttachmentManager.NormalizeOrder`. Podgląd przebudowywał wtedy plan stron przez `OrderBy(item => item.Order)`, czyli według jeszcze starych wartości `Order`, i utrwalał poprzednią kolejność. Normalizacja nie wywoływała kolejnego zdarzenia kolekcji.

## Wykonane zmiany
- `RebuildAttachmentPreviewPages` iteruje bezpośrednio po żywej kolekcji `ProductionCard.Attachments`, więc reaguje na faktyczną kolejność po `Move`.
- Dodano regresję dla jednej strony karty oraz załączników 1, 1 i 3 strony. Test przesuwa wielostronicowy Lolands na koniec, zachowuje fizyczny indeks `3 / 6` i sprawdza pełny plan stron podglądu: Jan, Jacobs, Lolands 1-3.
- Regresja sprawdza plan fizycznych stron wraz z indeksami stron PDF, widoczną kolejność kolekcji i końcową normalizację `Order`; nie polega wyłącznie na polach `Order`.
- Nie zmieniono generatora ani kompozytora PDF, zapisu, limitów ani interfejsu.

## Testy
- `dotnet test "COMMA Workspace 4.0.sln"` — PASS: 171/171 testów, 0 niepowodzeń, 0 pominiętych.
- Nowy test regresji w Release — PASS: 1/1.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS: 0 ostrzeżeń, 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki — wyłącznie z `ALLOWED_PATHS_JSON`.

## Problemy lub ryzyka
Pierwsza, dodatkowa próba pojedynczego testu bez ograniczenia liczby węzłów została zablokowana przez sandbox przy tworzeniu named pipe MSBuild (`SocketException (13): Permission denied`). Próba `--no-restore -m:1` ujawniła nieaktualne zasoby Debug (`WithDeveloperTools`). Po standardowym restore wykonanym przez wymaganą komendę pełny zestaw Debug przeszedł 171/171. Nie pozostała blokada walidacji.

## Podsumowanie
Podgląd załączników korzysta teraz z tej samej bieżącej kolejności kolekcji co widoczna lista. Po przesunięciu wielostronicowego załącznika na koniec zawartość zachowanej fizycznej strony odświeża się natychmiast. Nie wykonano commita ani pushu; HEAD pozostał bez zmian zgodnie z poleceniem użytkownika.
