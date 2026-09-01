# Aktualne zadanie

- TASK_ID: ATTACHMENT-PREVIEW-BINDING-008
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: dec95aeee80c3e4f1ec684b56d4a36ae444eae64
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Preserve live attachment list after refresh
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Views/AttachmentsWindow.axaml", "COMMA.App/Views/AttachmentsWindow.axaml.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel
Dokończyć naprawę widocznej kolejności załączników bez odłączania ListBox od żywej kolekcji. Poprzednia zmiana wymusza odświeżenie przez `card.Attachments.ToArray()`, co tworzy migawkę i może sprawić, że kolejne dodanie lub usunięcie załącznika w tym samym otwartym oknie nie będzie widoczne.

## Ważna kontrola HEAD
Commit kolejkujący będzie bezpośrednim potomkiem `BASE_HEAD_BEFORE_QUEUE`. Za prawidłowy stan początkowy uznaj commit kolejkujący, jeżeli wskazana baza jest jego bezpośrednim rodzicem, a commit kolejkujący zmienia wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.

## Wymagania
1. Potwierdź repozytorium, gałąź, czysty worktree, relację HEAD oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Usuń użycie migawki `card.Attachments.ToArray()` jako trwałego `ItemsSource`.
3. Po udanym ruchu wymuś deterministyczne przebudowanie widocznej kolejności, ale ostateczny `ItemsSource` musi nadal wskazywać żywą kolekcję `card.Attachments` lub równoważny obserwowalny widok zachowujący dalsze powiadomienia.
4. Po przesunięciu widoczny wiersz ma natychmiast zmienić miejsce, zaznaczenie ma pozostać na przeniesionym obiekcie, a stany strzałek mają być poprawne.
5. W tym samym nadal otwartym oknie, już po co najmniej jednym przesunięciu, kolejne DODAJ i USUŃ muszą natychmiast aktualizować widoczną listę. Nie wolno zostawić statycznej migawki.
6. Zachowaj istniejące limity, dane, pola `Order`, zapis i kolejność PDF bez zmian.
7. Popraw test kontraktu tak, aby zabraniał `ToArray()`, potwierdzał ponowne przypięcie żywej kolekcji oraz kolejność: Move → odświeżenie → żywe ItemsSource → przywrócenie zaznaczenia.
8. Zachowaj istniejący test trzech nazw i granic ruchu.
9. Nie dodawaj Avalonia Headless ani dużej infrastruktury.
10. Po zakończeniu ustaw dokładnie jeden wpis `- STATUS: COMPLETED` w `.ai/handoff.md`.

## Walidacja
- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS, jeżeli środowisko pozwala uruchomić VSTest; każdą blokadę sandboxa opisz dokładnie.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z allowlisty.

## Zakazy
- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie zmieniaj generatora PDF ani formatu zapisu.
- Nie wykonuj resetu, rebase ani force push.
