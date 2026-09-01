# Stan przekazania

- TASK_ID: ATTACHMENT-PREVIEW-BINDING-008
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe validation worker
- BRANCH: workspace-4.0
- HEAD: b8b8a063421a56d803a5ac9909ff0fa63adb5d0b

## Stan
Naprawa jest zaimplementowana. Po przesunięciu ListBox jest przebudowywany, po czym `ItemsSource` ponownie wskazuje żywą kolekcję `card.Attachments`, a zaznaczenie wraca na przeniesiony obiekt. Test kontraktowy wymusza tę kolejność i zabrania migawki `ToArray()`.

## Walidacja
- Build Release: PASS, 0 ostrzeżeń i 0 błędów.
- VSTest: BLOCKED przez sandbox (`TcpListener` kończy się `SocketException (13): Permission denied`) po pomyślnej kompilacji wszystkich projektów.
- Commit/push: niewykonane zgodnie z poleceniem użytkownika.

## Następny krok
Bezpieczny worker powinien uruchomić pełny `dotnet test "COMMA Workspace 4.0.sln"` w środowisku pozwalającym VSTest otworzyć lokalne gniazdo, zweryfikować diff i wykonać commit/push zgodnie ze swoim procesem.
