# Aktualne zadanie

- TASK_ID: ATTACHMENT-PREVIEW-PAGE-ORDER-009
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 33bd6a38935fc92bdb4ffe3a4e5b2a7b2eff41ed
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Fix attachment preview page order
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/ViewModels/MainViewModel.Pages.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel
Naprawić kolejność stron załączników w podglądzie aplikacji po przesuwaniu załączników strzałkami. Lista w oknie załączników i wygenerowany PDF są poprawne, ale podgląd zachowuje poprzednią kolejność stron.

## Przypadek potwierdzony przez użytkownika
Karta ma 1 stronę, a załączniki w widocznej kolejności:
1. Jan Witdouck — 1 strona,
2. Jacobs — 1 strona,
3. Lolands — 3 strony.

Oczekiwany podgląd ma łącznie 6 stron:
- 1/6 — karta produkcyjna,
- 2/6 — Jan Witdouck,
- 3/6 — Jacobs,
- 4/6–6/6 — Lolands.

Obecnie na 3/6 nadal pojawia się Lolands, mimo że jest ostatnim załącznikiem. PDF końcowy ma prawidłową kolejność.

## Diagnoza do zweryfikowania
`ObservableCollection.Move` wysyła `CollectionChanged` zanim `OrderAttachmentManager.Move` zakończy `NormalizeOrder`. `RebuildAttachmentPreviewPages` reaguje natychmiast, ale sortuje po jeszcze starych wartościach `Order`, przez co odtwarza poprzednią kolejność. Następna normalizacja pól `Order` nie wysyła kolejnego zdarzenia kolekcji.

## Ważna kontrola HEAD
Commit kolejkujący będzie bezpośrednim potomkiem `BASE_HEAD_BEFORE_QUEUE`. Za prawidłowy stan początkowy uznaj commit kolejkujący, jeżeli wskazana baza jest jego bezpośrednim rodzicem, a commit kolejkujący zmienia wyłącznie `.ai/task.md`, `.ai/report.md` i `.ai/handoff.md`.

## Wymagania
1. Potwierdź repozytorium, gałąź, czysty worktree, relację HEAD oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Podgląd stron ma korzystać z faktycznej bieżącej kolejności żywej kolekcji `ProductionCard.Attachments` po `Move`, bez odtwarzania starej kolejności z chwilowo nieznormalizowanych pól `Order`.
3. Po każdym ruchu góra/dół podgląd ma natychmiast odpowiadać widocznej liście i kolejności końcowego PDF.
4. Zachowaj bieżący fizyczny numer strony, jeśli nadal istnieje, ale odśwież zawartość tej pozycji. W opisanym przypadku po przesunięciu Lolands na koniec strona 3/6 ma pokazać Jacobs, a Lolands dopiero od 4/6.
5. Dodanie, usunięcie, wczytanie zapisanej karty, liczba stron, zaznaczenie w oknie załączników i pola `Order` muszą pozostać poprawne.
6. Nie zmieniaj generatora ani kompozytora PDF, formatu zapisu, limitów załączników ani wyglądu interfejsu.
7. Dodaj test regresji z trzema załącznikami o liczbie stron 1, 1 i 3. Test ma wykazać prawidłową kolejność fizycznych stron podglądu po przesunięciu wielostronicowego PDF na koniec i ma nie polegać wyłącznie na sprawdzeniu pól `Order`.
8. Zachowaj istniejące testy kolejności listy, żywego `ItemsSource`, dodawania i usuwania.
9. Po zakończeniu ustaw dokładnie jeden wpis `- STATUS: COMPLETED` w `.ai/handoff.md`.

## Walidacja
- `dotnet test "COMMA Workspace 4.0.sln"` — wszystkie testy PASS, jeżeli środowisko pozwala uruchomić VSTest; każdą blokadę sandboxa opisz dokładnie.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1` — PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki wyłącznie z allowlisty.

## Zakazy
- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
