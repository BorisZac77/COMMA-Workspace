# Aktualne zadanie

- TASK_ID: ATTACHMENT-REORDER-006
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 5f0a157db013b9b6a2d9ea3638821081f3dee217
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Fix attachment reordering controls
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Views/AttachmentsWindow.axaml", "COMMA.App/Views/AttachmentsWindow.axaml.cs", "COMMA.App/Services/Attachments/OrderAttachmentManager.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel
Naprawić zmianę kolejności załączników przyciskami strzałek góra/dół w oknie ZAŁĄCZNIKI. Użytkownik potwierdził, że błąd występuje w aplikacjach 4.0 i 4.1.

## Ważna kontrola HEAD
Commit kolejkujący to zadanie będzie bezpośrednim potomkiem `BASE_HEAD_BEFORE_QUEUE`. Nie wymagaj równości bieżącego HEAD z wartością bazową. Za prawidłowy stan początkowy uznaj bieżący commit kolejkujący, jeżeli `BASE_HEAD_BEFORE_QUEUE` jest jego bezpośrednim rodzicem, a zmiany w commicie kolejkującym dotyczą wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.

## Wymagania
1. Potwierdź właściwy katalog, repozytorium, gałąź, czysty worktree i prawidłową relację HEAD opisaną wyżej.
2. Potwierdź, że `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
3. Odtwórz problem dla co najmniej trzech załączników o różnych nazwach.
4. Ustal rzeczywistą przyczynę w przepływie ListBox → obsługa kliknięcia → kolekcja załączników. Nie zakładaj z góry, że sama metoda `Move` jest błędna.
5. Po kliknięciu ↑ wybrany załącznik ma przesunąć się dokładnie o jedną pozycję wyżej; po kliknięciu ↓ dokładnie o jedną pozycję niżej.
6. Po przesunięciu zaznaczenie ma pozostać na przeniesionym załączniku, a stany aktywności obu strzałek mają odpowiadać jego nowej pozycji.
7. Kolekcja i pola `Order` muszą mieć tę samą, znormalizowaną kolejność, używaną później w zapisie i PDF.
8. Zachowaj dotychczasowe dodawanie, usuwanie, limity i zawartość załączników bez innych zmian interfejsu.
9. Dodaj test regresji, który sprawdza faktyczną kolejność obiektów po ruchu w górę i w dół, pola `Order` oraz zachowanie na pierwszej i ostatniej pozycji. Jeśli środowisko testowe pozwala, obejmij też przepływ przycisków i utrzymanie zaznaczenia.
10. Wybierz najprostszą poprawkę mieszczącą się w allowliście.

## Walidacja
- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z `ALLOWED_PATHS_JSON`.

## Zakazy
- Nie zmieniaj `main`.
- Nie zmieniaj COMMA WMS ani KOMI Animation Lab.
- Nie dodawaj nowych funkcji poza naprawą kolejności załączników.
- Nie zmieniaj wyglądu pozostałych części aplikacji.
- Nie wykonuj resetu, rebase ani force push.
