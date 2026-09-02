# Stan przekazania

- TASK_ID: ATTACHMENT-WINDOWS-NETWORK-LOCK-013
- STATUS: BLOCKED_VALIDATION
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe validation worker
- BRANCH: workspace-4.0

## Stan

Wdrożono 5-sekundowe okno ponawiania pełnej operacji otwarcia i kopiowania na Windows (21 prób, 20 × 250 ms) dla ERROR_SHARING_VIOLATION/LOCK_VIOLATION, z czyszczeniem częściowych plików. Zachowano dotychczasowe zachowanie macOS. Dodano deterministyczne testy wymaganych scenariuszy. Build Release przechodzi bez ostrzeżeń i błędów, a `git diff --check` przechodzi.

Pełny `dotnet test` nie rozpoczął wykonywania testów, ponieważ sandbox blokuje lokalny socket VSTest (`SocketException (13): Permission denied` przy `TcpListener.Start`).

## Następny krok

Bezpieczny worker ma uruchomić pełne `dotnet test "COMMA Workspace 4.0.sln"` w środowisku pozwalającym na komunikację VSTest. Po zaliczeniu pełnych testów może ustawić handoff `COMPLETED`, wykonać commit `Extend Windows network attachment retry` i push zgodnie z kolejką. Nie tworzyć jeszcze ZIP-a Windows.
