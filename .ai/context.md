# COMMA Workspace 4.0 — kontekst dla Codexa

## Tożsamość projektu
- Repozytorium GitHub: `BorisZac77/COMMA-Workspace`
- Lokalny katalog roboczy wersji 4.0 na MacBooku: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`
- Gałąź rozwojowa: `workspace-4.0`
- `main` jest stabilną wersją 3.0 i nie wolno go zmieniać w ramach zadań 4.0.
- Worktree `/Users/Boris/RiderProjects/COMMA Workspace 3.0` należy do stabilnej wersji 3.0 i nie wolno w nim wykonywać zadań wersji 4.0.

## Zasady stałe
1. Przed pracą sprawdź: `pwd`, `git rev-parse --show-toplevel`, `git branch --show-current`, `git status --short`.
2. Jeśli katalog, repozytorium lub gałąź nie zgadzają się z powyższymi danymi — zatrzymaj się i opisz blokadę w `.ai/report.md`.
3. Jeśli repozytorium zawiera nieoczekiwane lokalne zmiany, nie nadpisuj ich i zatrzymaj zadanie.
4. Modyfikuj wyłącznie pliki dopuszczone w `.ai/task.md`.
5. Nie dodawaj funkcji, których nie ma w zadaniu.
6. Nie zmieniaj danych użytkownika, dokumentów klientów, sekretów ani lokalnych artefaktów.
7. Zawsze wykonaj testy i build wskazane w zadaniu.
8. Po pracy zaktualizuj `.ai/report.md` oraz `.ai/handoff.md`.
9. Commit i push wykonuj wyłącznie wtedy, gdy `.ai/task.md` wyraźnie na to zezwala.
