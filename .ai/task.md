# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-RAW-IO-011
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 0be9283cb79ca31baf3eab705ed8c90edd5c2e26
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Handle Windows attachment IO errors
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Attachments/OrderAttachmentManager.cs", "COMMA.App/Services/Attachments/OrderAttachmentContentStore.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel

Naprawić nadal występujący import pliku `Vandeputte-10.pdf` na PC Windows. Użytkownik usunął poprzedni folder aplikacji, wypakował świeże archiwum `COMMA Workspace 4.1B Windows x64.zip` do nowego katalogu Program Files i uruchomił `COMMA.App.exe` z tej kopii. Mimo to aplikacja nadal pokazuje surowy komunikat Windows: `The process cannot access the file because it is being used by another process.`

## Fakt do odtworzenia

Poprzednia zmiana `FileShare.ReadWrite | FileShare.Delete` oraz retry w `OrderAttachmentContentStore.OpenSourceWithRetry` nie przechwyciła błędu widocznego użytkownikowi. Nie zakładaj, że uruchomiono starą aplikację. Ustal, która operacja importu nadal przekazuje nieobsłużony wyjątek.

## Wymagania

1. Potwierdź repozytorium, gałąź, czysty worktree, relację HEAD oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Prześledź wszystkie operacje wejścia/wyjścia od wybranego pliku do utworzenia metadanych: odczyt długości, otwarcie źródła, kopiowanie, walidację, plik tymczasowy, przeniesienie i czyszczenie. Nie ograniczaj diagnozy tylko do pierwszego `FileStream`.
3. Dla błędów współdzielenia lub blokady występujących na dowolnym etapie odczytu wybranego pliku zastosuj ten sam ograniczony retry, o ile ponowienie jest bezpieczne i nie może doprowadzić do częściowego importu.
4. Żaden błąd blokady pliku nie może trafić do okna jako surowy angielski tekst systemowy. Komunikat ma być po polsku, zawierać nazwę pliku i rozróżniać blokadę od pozostałych błędów wejścia/wyjścia.
5. Jeśli Windows rzeczywiście utrzymuje trwałą blokadę wyłączną, nie próbuj jej obchodzić ani nie importuj niepełnych danych; komunikat ma wskazać użytkownikowi, że program korzystający z pliku musi zostać zamknięty.
6. Zachowaj działanie poprawnie współdzielonych plików; nie zmieniaj formatu karty, generatora PDF, limitów, kolejności, podglądu ani wyglądu interfejsu.
7. Zachowaj atomowość: po nieudanym imporcie nie może pozostać plik `.part`, magazynowa zawartość ani metadane.
8. Dodaj deterministyczne testy regresji obejmujące dokładnie błąd współdzielenia zgłoszony przez `AddFiles`, pełną ścieżkę obsługi komunikatu oraz czyszczenie. Nie polegaj na fizycznej blokadzie systemu operacyjnego.
9. Po zakończeniu ustaw dokładnie jeden wpis `- STATUS: COMPLETED` w `.ai/handoff.md`.

## Walidacja

- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS, jeżeli środowisko pozwala uruchomić VSTest; każdą blokadę sandboxa opisz dokładnie.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z allowlisty.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
