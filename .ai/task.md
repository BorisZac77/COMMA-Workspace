# Aktualne zadanie

- TASK_ID: PACKAGE-WINDOWS-4.1E-PC-TEST-017
- STATUS: READY
- PROJECT: COMMA Workspace 4.1E Windows test package
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 99317c5ceb8d285c11ec0f6f33ab2c628d781620
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package Windows attachment staging test build
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Przygotować świeży, samodzielny pakiet Windows x64 COMMA Workspace 4.1E wyłącznie do rzeczywistego testu poprawki importu załącznika z mapowanego dysku `Z:`. Pakiet musi zawierać implementację z commita `3ff9879`, która używa Windows Shell do lokalnego stagingu wybranego pliku, importuje kopię lokalną i ją sprząta.

## Wymagania

1. Przed działaniem przeczytaj w całości `AGENTS.md` i wszystkie pliki w `.ai`. Potwierdź właściwy worktree, gałąź `workspace-4.0`, czysty status, relację historii oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Potwierdź, że commit `3ff9879` jest przodkiem bieżącego HEAD i że bieżące źródła nadal zawierają `WindowsAttachmentSourceStager` używający Windows Shell `SHFileOperation`.
3. Nie zmieniaj żadnego kodu aplikacji, testów, projektów, pakietów ani konfiguracji. Dozwolone zmiany repozytorium to wyłącznie `.ai/report.md` i `.ai/handoff.md`.
4. Wykonaj świeży publish `COMMA.App` jako Release, `win-x64`, self-contained, do nowego katalogu tymczasowego poza repozytorium. Nie używaj starego katalogu publish.
5. Utwórz na Pulpicie dokładnie nowy pakiet:
   `/Users/Boris/Desktop/COMMA Workspace 4.1E Windows x64.zip`
   z jednym katalogiem głównym:
   `COMMA Workspace 4.1E Windows x64/`
   i plikiem:
   `COMMA Workspace 4.1E Windows x64/COMMA.App.exe`.
6. Nie nadpisuj ani nie usuwaj wcześniejszych pakietów, w szczególności wersji 4.1D. Jeżeli docelowy plik 4.1E już istnieje i nie można bezpiecznie potwierdzić, że pochodzi z tego zadania, zatrzymaj się jako BLOCKED zamiast go nadpisywać.
7. Zweryfikuj integralność ZIP przez `unzip -t`, dokładnie jeden katalog główny, obecność `COMMA.App.exe`, rozmiar, liczbę wpisów i SHA-256. Potwierdź, że opublikowany pakiet pochodzi z bieżącego HEAD i zawiera commit `3ff9879`.
8. Nie uruchamiaj aplikacji Windows na macOS i nie próbuj symulować dostępu do `Z:`. Nie wykonuj kolejnych eksperymentów z `FileStream`, `File.Copy`, retry ani komunikatami błędów.
9. Nie uruchamiaj pełnego `dotnet test` ponownie — testy implementacji przeszły wcześniej 177/177. W tym zadaniu wykonaj wyłącznie Release publish oraz walidację pakietu.
10. Zaktualizuj raport i handoff z dokładną ścieżką ZIP, rozmiarem, SHA-256, liczbą wpisów i wynikiem kontroli. Ustaw `COMPLETED` tylko jeśli pakiet jest gotowy i zweryfikowany; `NEXT_ACTOR: PC test`.
11. Po pomyślnym przygotowaniu pakietu safe worker ma wykonać commit i push wyłącznie raportu oraz handoffu.

## Kryterium odbioru na PC

1. Rozpakuj 4.1E do nowego lokalnego folderu na PC i uruchom `COMMA.App.exe`.
2. Otwórz zlecenie i wybierz `DODAJ`.
3. Wybierz ten sam `Vandeputte-10.pdf` bezpośrednio z mapowanego dysku `Z:`.
4. Sukces oznacza: załącznik zostaje dodany bez komunikatu o użyciu przez inny program i można go później otworzyć.
5. Do czasu wykonania tego testu nie wolno opisywać poprawki jako potwierdzonej na PC.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI.
- Nie zmieniaj kodu COMMA Workspace.
- Nie używaj resetu, stash, rebase, cherry-pick ani force push.
- Nie usuwaj i nie nadpisuj istniejących paczek.
- Nie twórz pakietu macOS.
