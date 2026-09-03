# Aktualne zadanie

- TASK_ID: PACKAGE-WINDOWS-4.1F-PC-TEST-019
- STATUS: READY
- PROJECT: COMMA Workspace 4.1F Windows test package
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 1e6a913f15427f54761776144d6c9201d283d292
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Package Windows close-before-move test build
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel

Przygotować świeży, samodzielny pakiet Windows x64 COMMA Workspace 4.1F do rzeczywistego testu importu załącznika bezpośrednio z mapowanego dysku `Z:`. Pakiet ma zawierać:
- Windows Shell staging z commita `3ff9879`;
- poprawkę dispose-before-move z commita `1e6a913f15427f54761776144d6c9201d283d292`.

Pakiet został utworzony ręcznie w interaktywnym Terminalu, ponieważ sandbox workera nie ma uprawnienia zapisu na Pulpicie. Użytkownik wykonał rzeczywisty test na PC i potwierdził, że dodawanie załącznika bezpośrednio z mapowanego dysku `Z:` działa. Worker ma teraz wyłącznie zweryfikować istniejący pakiet, zapisać metadane oraz potwierdzenie odbioru w raporcie i handoffie.

## Oczekiwany pakiet

- Ścieżka: `/Users/Boris/Desktop/COMMA Workspace 4.1F Windows x64.zip`
- Jeden katalog główny: `COMMA Workspace 4.1F Windows x64/`
- Plik wykonywalny: `COMMA Workspace 4.1F Windows x64/COMMA.App.exe`
- Publish: Release, `win-x64`, self-contained
- Źródło: dokładnie commit `1e6a913f15427f54761776144d6c9201d283d292`

## Wymagania końcowej walidacji

1. Nie nadpisuj istniejącego ZIP-a 4.1F; jeżeli istnieje, zatrzymaj się.
2. Sprawdź `unzip -t`, dokładnie jeden katalog główny, dokładnie jeden `COMMA.App.exe`, liczbę wpisów, rozmiar i SHA-256.
3. Nie uruchamiaj kolejnych testów ani nie zmieniaj kodu.
4. Safe worker ma wyłącznie zweryfikować istniejący pakiet, zaktualizować `.ai/report.md` i `.ai/handoff.md`, a następnie wykonać commit i push.
5. Zapisz wynik rzeczywistego testu użytkownika: import załącznika bezpośrednio z `Z:` na PC działa poprawnie; wcześniejszy komunikat o użyciu pliku przez inny program nie występuje.

## Kryterium odbioru na PC

1. Rozpakuj 4.1F do nowego lokalnego folderu na PC.
2. Uruchom `COMMA.App.exe`.
3. Otwórz zlecenie, kliknij `DODAJ` i wybierz ten sam PDF bezpośrednio z `Z:`.
4. Sukces: załącznik zostaje dodany bez komunikatu o użyciu przez inny program i można go otworzyć.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI.
- Nie zmieniaj kodu aplikacji, testów, konfiguracji ani pakietów NuGet.
- Nie twórz pakietu macOS.
- Nie używaj resetu, stash, rebase, cherry-pick ani force push.

## Wynik rzeczywistego testu PC

Użytkownik potwierdził 2026-09-03: „jest ok, dodawanie załączników na PC działa”. Kryterium odbioru dla importu bezpośrednio z mapowanego dysku `Z:` jest spełnione.
