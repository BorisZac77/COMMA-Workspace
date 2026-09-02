# Raport Codexa

- TASK_ID: ATTACHMENT-WINDOWS-NETWORK-LOCK-013
- STATUS: BLOCKED_VALIDATION
- STARTED_AT: 2026-09-02T16:10:00+0200
- COMPLETED_AT: 2026-09-02T16:25:49+0200
- REPOSITORY_ROOT: /Users/Boris/RiderProjects/COMMA Workspace 4.0
- BRANCH: workspace-4.0
- HEAD_BEFORE: 83cd0ff79b2352075b25648aeea7afb32b611c0e
- HEAD_AFTER: 83cd0ff79b2352075b25648aeea7afb32b611c0e

## Cel

Naprawić import załącznika PDF na Windows z dysku sieciowego/mapowanego (zaobserwowane na ścieżce Z:), gdy krótkotrwała blokada systemowa ERROR_SHARING_VIOLATION trwa dłużej niż obecne około 0,15 sekundy.

## Kontrole wstępne

- `pwd` i `git rev-parse --show-toplevel`: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`.
- `git branch --show-current`: `workspace-4.0`.
- Początkowy `git status --short`: czysty.
- HEAD `83cd0ff79b2352075b25648aeea7afb32b611c0e`; baza kolejki `554596af6a1f226963418a0e950820ef7d1debde` jest jego przodkiem.
- `main`: niezmieniony `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Wykonane zmiany

- Przeanalizowano przepływ `IStorageFile.TryGetLocalPath()` → `AttachmentsWindow` → `OrderAttachmentManager` → `OrderAttachmentContentStore`.
- Na Windows rozszerzono politykę ponawiania do 21 pełnych prób otwarcia i skopiowania źródła z 20 odstępami po 250 ms (łącznie 5 sekund oczekiwania).
- Zachowano dotychczasową politykę macOS i pozostałych systemów: 3 próby co 75 ms.
- Ponawianie nadal obejmuje błędy ERROR_SHARING_VIOLATION i LOCK_VIOLATION zarówno przy otwieraniu, jak i podczas odczytu; nieudane kopiowanie usuwa plik docelowy i `.part` przed kolejną próbą.
- Dodano wstrzykiwaną konfigurację liczby prób i oczekiwania do deterministycznych testów bez rzeczywistego czekania.
- Dodano testy: pięć kolejnych błędów otwarcia i sukces, częściowy odczyt z blokadą i sukces, wyczerpanie 5-sekundowego okna z polskim komunikatem oraz brak `.part` i dokładnie jeden poprawny załącznik po sukcesie.
- `OrderAttachmentManager` i `AttachmentsWindow` nie wymagały zmian.

## Testy

- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore --maxcpucount:1 --disable-build-servers -p:UsedAvaloniaProducts=` — PASS, 0 ostrzeżeń, 0 błędów.
- `git diff --check` — PASS.
- Próba trzech nowych testów — zestawy skompilowane, ale VSTest nie rozpoczął wykonywania: `System.Net.Sockets.SocketException (13): Permission denied` podczas `TcpListener.Start`.
- Pełne `dotnet test "COMMA Workspace 4.0.sln" -c Release --no-restore --maxcpucount:1 --disable-build-servers -p:UsedAvaloniaProducts=` — BLOCKED z tego samego powodu infrastrukturalnego; przebieg testów przerwany przed wykonaniem testów.

## Problemy lub ryzyka

- Ograniczony sandbox nie zezwala VSTest na utworzenie lokalnego socketu komunikacyjnego. Nie użyto podwyższonych uprawnień ani niedozwolonego trybu dostępu.
- Pełny zestaw testów musi wykonać bezpieczny worker poza tym ograniczeniem. Do tego czasu zadania nie można oznaczyć jako `COMPLETED` ani automatycznie commitować/pushować.

## Podsumowanie

Implementacja i deterministyczne testy są gotowe, build Release oraz kontrola diffu przechodzą. Zadanie pozostaje `BLOCKED_VALIDATION`, ponieważ runner testów nie może uruchomić się w bieżącym sandboxie.
