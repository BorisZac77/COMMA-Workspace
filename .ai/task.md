# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-RAW-IO-011-VALIDATE
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: f690beb184133d814b744ad931d78829906b1bf4
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Validate Windows attachment IO fix
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Wykonać końcową walidację już opublikowanej poprawki importu załączników Windows z commitu `f690beb184133d814b744ad931d78829906b1bf4`. Nie zmieniaj kodu aplikacji ani zachowania funkcji.

## Wymagania

1. Potwierdź repozytorium, gałąź `workspace-4.0`, czysty worktree, relację HEAD i niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` w lokalnym środowisku workera. Jeśli nie powiedzie się z przyczyny niezależnej od testów, opisz dokładnie blokadę w raporcie i nie oznaczaj pełnej walidacji jako PASS.
3. Uruchom `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1`.
4. Wykonaj `git diff --check` oraz sprawdź, że zmienione są wyłącznie pliki z allowlisty.
5. Jeżeli testy i build przejdą, uzupełnij `.ai/report.md` i `.ai/handoff.md` końcowym statusem `COMPLETED`, z wynikami testów i builda, a następnie wykonaj jeden commit i push na `workspace-4.0`.
6. Jeżeli testy nie przejdą, nie wykonuj commit/push i opisz blokadę wyłącznie w tych dwóch plikach.

## Zakazy

- Nie zmieniaj kodu, `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
