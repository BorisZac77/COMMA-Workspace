# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-FILE-LOCK-010
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 3b15ebd4ca0274527257f186629b2680afd3173d
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Improve Windows attachment file sharing
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Attachments/OrderAttachmentContentStore.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel

Naprawić dodawanie załączników PDF/JPG/PNG na Windows, gdy wybrany plik jest chwilowo używany przez inny proces do odczytu. Na Mac problem nie występuje. Dla pliku `Vandeputte-10.pdf` aplikacja Windows wyświetliła ogólny błąd systemowy: `The process cannot access the file because it is being used by another process.`

## Diagnoza do weryfikacji

`OrderAttachmentContentStore.ImportFile` otwiera plik źródłowy z `FileShare.Read`. Uzgodnić tryb współdzielenia pliku i krótką, ograniczoną próbę ponownego odczytu dla przejściowych blokad Windows. Nie wolno omijać trwałej blokady wyłącznej ani importować niekompletnej zawartości.

## Wymagania

1. Potwierdź repozytorium, gałąź, czysty worktree, relację HEAD oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Import odczytuje plik źródłowy w trybie bezpiecznie zgodnym z innymi procesami czytającymi plik na Windows, również gdy proces zezwala na współdzielony odczyt/zapis/usunięcie.
3. Dla przejściowego błędu blokady pliku zastosuj krótki, ograniczony retry; nie stosuj nieograniczonego oczekiwania.
4. Jeśli plik nadal jest zablokowany wyłącznie, zwróć zrozumiały komunikat po polsku z nazwą pliku oraz sugestią zamknięcia programu korzystającego z niego. Zachowaj pozostałe komunikaty błędów i obsługę wielokrotnego wyboru.
5. Zachowaj atomowość importu: po błędzie nie może pozostać plik tymczasowy, wpis magazynu ani metadane załącznika.
6. Nie zmieniaj formatu karty, generatora PDF, limitów załączników, kolejności, podglądu ani interfejsu poza treścią błędu przekazaną do istniejącego okna.
7. Dodaj testy regresji dla trybu współdzielenia/retry lub wyodrębnionej logiki otwarcia pliku, w tym zachowanie atomowego sprzątania po trwałym błędzie dostępu. Testy mają być deterministyczne i niezależne od blokad systemu operacyjnego.
8. Po zakończeniu ustaw dokładnie jeden wpis `- STATUS: COMPLETED` w `.ai/handoff.md`.

## Walidacja

- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS, jeżeli środowisko pozwala uruchomić VSTest; każdą blokadę sandboxa opisz dokładnie.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z allowlisty.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
