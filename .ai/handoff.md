# Stan przekazania

- TASK_ID: ATTACHMENT-WINDOWS-CLOSE-BEFORE-MOVE-018
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: safe worker
- BRANCH: workspace-4.0
- HEAD: df2f3182914555cb7e0c9f709cd255022b390c5c

## Stan

Implementacja jest gotowa do commita i pushu przez safe worker. `ImportStream` zamyka `.part` po pełnym zapisie i flushu do dysku, zanim wykona przeniesienie. Test regresyjny sprawdza kolejność dispose-before-move przez kontrolowany seam wewnętrzny, niezależnie od systemu plików.

## Walidacja

- Release build: PASS — `dotnet build "COMMA Workspace 4.0.sln" --configuration Release --no-restore`, 0 ostrzeżeń, 0 błędów.
- Pełne testy: uruchomione raz, BLOCKED przez sandboxowy `SocketException (13): Permission denied` przy tworzeniu `NamedPipeServerStream`/`TcpListener`; nie ponawiano zgodnie z limitem zadania.
- `git diff --check`: PASS.
- Allowlista: PASS.

## Następny krok

Safe worker powinien wykonać commit o komunikacie `Close attachment temp file before move` i push. Potem należy utworzyć osobne zadanie dla pakietu Windows 4.1F oraz przeprowadzić rzeczywisty test tego samego PDF bezpośrednio z `Z:` na PC. Poprawka nie jest jeszcze potwierdzona na PC.
