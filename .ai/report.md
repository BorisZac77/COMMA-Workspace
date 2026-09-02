# Raport Codexa

- TASK_ID: ATTACHMENT-WINDOWS-NETWORK-LOCK-013
- STATUS: COMPLETED
- STARTED_AT: 2026-09-02T16:10:00+0200
- COMPLETED_AT: 2026-09-02T16:39:23+0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 83cd0ff79b2352075b25648aeea7afb32b611c0e
- HEAD_AFTER: 5549ab44e7667d5d277343d023ff4686c88db801

## Cel

Naprawić import załącznika PDF na Windows z dysku sieciowego/mapowanego (zaobserwowane na ścieżce Z:), gdy krótkotrwała blokada systemowa ERROR_SHARING_VIOLATION trwa dłużej niż poprzednie około 0,15 sekundy.

## Wykonane zmiany

- Na Windows polityka ponawiania obejmuje 21 pełnych prób otwarcia i kopiowania źródła, z 20 odstępami po 250 ms (łącznie 5 sekund oczekiwania).
- Zachowano dotychczasową politykę macOS i pozostałych systemów: 3 próby co 75 ms.
- Ponawianie obejmuje ERROR_SHARING_VIOLATION i LOCK_VIOLATION zarówno przy otwieraniu, jak i podczas odczytu; nieudane kopiowanie usuwa plik docelowy i `.part` przed kolejną próbą.
- Dodano deterministyczne testy błędów otwarcia, błędu podczas częściowego odczytu, wyczerpania okna prób oraz czyszczenia plików częściowych.
- Nie zmieniono kolejności, limitów, metadanych, formatu PDF ani zachowania macOS.

## Walidacja

- `dotnet test "COMMA Workspace 4.0.sln"` uruchomione lokalnie poza ograniczonym sandboxem — PASS: 174/174 testów, 0 niepowodzeń, 0 pominiętych, 69,0 s.
- Build Release — PASS: 0 ostrzeżeń, 0 błędów.
- `git diff --check` — PASS.
- Zmienione ścieżki należą wyłącznie do allowlisty.
- `main` pozostał niezmieniony: `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Publikacja

- Commit: `5549ab44e7667d5d277343d023ff4686c88db801` — `Extend Windows network attachment retry`.
- Commit został wypchnięty na `origin/workspace-4.0`.

## Podsumowanie

Walidacja ukończona. Można przygotować nowy samodzielny pakiet ZIP Windows z tą poprawką.
