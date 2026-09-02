# Aktualne zadanie

- TASK_ID: PACKAGE-WINDOWS-4.1C-012
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 1e9dcaa231f4210af4a33be64cd610b1deeefe37
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package Windows 4.1C
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Utworzyć na Pulpicie MacBooka nowe archiwum Windows:
`/Users/Boris/Desktop/COMMA Workspace 4.1C Windows x64.zip`.

W archiwum ma znaleźć się samodzielna aplikacja Windows x64, zbudowana z bieżącego `workspace-4.0`, zawierająca zweryfikowaną poprawkę importu załączników Windows z commitu `f690beb184133d814b744ad931d78829906b1bf4`.

## Wymagania

1. Potwierdź repozytorium, gałąź, czysty worktree, relację HEAD i niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Zbuduj samodzielny pakiet Windows: `dotnet publish COMMA.App -c Release -r win-x64 --self-contained true`.
3. Umieść zawartość w folderze wewnątrz ZIP-a nazwanym dokładnie `COMMA Workspace 4.1C Windows x64`.
4. Nie modyfikuj ani nie usuwaj wcześniejszych archiwów ani aplikacji użytkownika. Utwórz lub zastąp wyłącznie docelowy ZIP 4.1C.
5. Zweryfikuj archiwum poleceniem `unzip -t` oraz potwierdź obecność `COMMA.App.exe`.
6. Nie zmieniaj kodu ani danych aplikacji.
7. Zaktualizuj raport i handoff, podając docelową ścieżkę, wynik publikacji, wynik `unzip -t`, obecność pliku wykonywalnego oraz status `COMPLETED`.
8. Wykonaj końcowy `git diff --check`; jeżeli wszystkie kontrole przejdą, commit i push wyłącznie plików raportowych zgodnie z `AUTO_COMMIT_PUSH`.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
