# Stan przekazania

- TASK_ID: ATTACHMENT-PREVIEW-REORDER-007
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe validation worker
- BRANCH: workspace-4.0
- HEAD_BEFORE_TASK: 72fea9f9c9b2f1f816c83aaefedbb6ce0d3331e8

## Stan
Poprawka odświeża widoczne źródło `AttachmentsList` z aktualnej migawki kolekcji natychmiast po udanym `Move`, następnie przywraca zaznaczenie na przeniesionym załączniku i aktualizuje stany strzałek. Kolejność danych i pola `Order` pozostają obsługiwane przez dotychczasowy manager.

## Walidacja
- Build Release: PASS, 0 ostrzeżeń, 0 błędów.
- Projekty i assembly testów: kompilacja PASS.
- Wykonanie VSTest: zablokowane przez sandbox (`SocketException (13): Permission denied` przy otwieraniu lokalnego TCP); wymaga ponowienia przez bezpiecznego workera.
- Commit i push nie zostały wykonane.

## Następny krok
Bezpieczny worker powinien uruchomić pełne `dotnet test "COMMA Workspace 4.0.sln"` poza ograniczeniem lokalnego IPC, zweryfikować końcowy diff i wykonać commit/push komunikatem `Refresh attachment preview after reordering`.
