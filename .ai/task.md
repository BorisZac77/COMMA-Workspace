# Aktualne zadanie

- TASK_ID: ATTACHMENT-PREVIEW-REORDER-007
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: db1316d8e8198fd854133a48ac730b1da0f95e0f
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Refresh attachment preview after reordering
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Views/AttachmentsWindow.axaml", "COMMA.App/Views/AttachmentsWindow.axaml.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel
Naprawić natychmiastowe odświeżanie widocznej kolejności załączników w oknie ZAŁĄCZNIKI po użyciu strzałek góra/dół. Użytkownik potwierdził, że kolejność jest poprawnie zapisywana i widoczna później w PDF, ale lista/podgląd w otwartym oknie aplikacji nadal pokazuje starą kolejność.

## Ważna kontrola HEAD
Commit kolejkujący to zadanie będzie bezpośrednim potomkiem `BASE_HEAD_BEFORE_QUEUE`. Nie wymagaj równości bieżącego HEAD z wartością bazową. Za prawidłowy stan początkowy uznaj bieżący commit kolejkujący, jeżeli `BASE_HEAD_BEFORE_QUEUE` jest jego bezpośrednim rodzicem, a zmiany w commicie kolejkującym dotyczą wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.

## Wymagania
1. Potwierdź właściwy katalog, repozytorium, gałąź, czysty worktree i prawidłową relację HEAD opisaną wyżej.
2. Potwierdź, że `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Odtwórz problem dla co najmniej trzech załączników o różnych nazwach w otwartym oknie ZAŁĄCZNIKI.
4. Ustal rzeczywistą przyczynę: kolekcja `Attachments` i PDF zmieniają kolejność, lecz widoczny `ListBox` nie odświeża pozycji. Sprawdź przepływ `ObservableCollection.Move` → powiadomienie kolekcji → `ItemsSource`/widok ListBox → zaznaczenie.
5. Po kliknięciu ↑ lub ↓ widoczny wiersz ma natychmiast przesunąć się dokładnie o jedną pozycję, bez zamykania i ponownego otwierania okna.
6. Zaznaczenie ma pozostać na przeniesionym załączniku, a stany obu strzałek mają odpowiadać jego nowej widocznej pozycji.
7. Zachowaj poprawne zachowanie warstwy danych: kolejność kolekcji i pola `Order` nadal muszą być zgodne z PDF i zapisem.
8. Nie zmieniaj generatora PDF, formatu danych, dodawania/usuwania załączników, limitów ani wyglądu innych części aplikacji.
9. Zastosuj najprostszą, deterministyczną poprawkę odświeżenia widoku. Nie dodawaj nowego frameworka testowego ani dużej infrastruktury.
10. Rozszerz regresję tak, aby obejmowała również zachowanie wymagane przez widok. Jeżeli bieżący projekt nie umożliwia uruchomienia Avalonia Headless, wydziel minimalną logikę możliwą do testowania lub dodaj najprostszy sensowny test kontraktu bez rozbudowy infrastruktury.
11. Po zakończeniu ustaw w `.ai/handoff.md` dokładnie jeden wpis `- STATUS: COMPLETED`, aby bezpieczny worker mógł wykonać commit i push.

## Walidacja
- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z `ALLOWED_PATHS_JSON`.

## Zakazy
- Nie zmieniaj `main`.
- Nie zmieniaj COMMA WMS ani KOMI Animation Lab.
- Nie zmieniaj generatora PDF ani formatu zapisu.
- Nie dodawaj funkcji poza naprawą odświeżania widocznej kolejności załączników.
- Nie wykonuj resetu, rebase ani force push.
