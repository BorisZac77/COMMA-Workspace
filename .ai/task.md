# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-NETWORK-LOCK-013
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 554596af6a1f226963418a0e950820ef7d1debde
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Extend Windows network attachment retry
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Attachments/OrderAttachmentContentStore.cs", "COMMA.App/Services/Attachments/OrderAttachmentManager.cs", "COMMA.App/Views/AttachmentsWindow.axaml.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Zgłoszony problem

Na rzeczywistym komputerze Windows pakiet 4.1C nadal nie może dodać pliku `Vandeputte-10.pdf`. Plik jest wybierany z biblioteki na dysku mapowanym `Z:`, a aplikacja pokazuje komunikat, że jest używany przez inny program. Poprzednia poprawka rozpoznaje ERROR_SHARING_VIOLATION, lecz wykonuje tylko trzy próby co 75 ms, czyli kończy się po około 0,15 sekundy.

## Cel

Zapewnić skuteczny, ograniczony czasowo import plików PDF/JPG/PNG na Windows z dysków lokalnych i mapowanych, kiedy blokada udostępniania jest przejściowa i może trwać kilka sekund. Nie próbować obchodzić trwałej blokady systemowej i nie zmieniać innych funkcji.

## Wymagania

1. Przed zmianami wykonaj wszystkie kontrole z AGENTS.md i `.ai/context.md`; potwierdź czysty worktree, `workspace-4.0`, relację HEAD oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Przeanalizuj cały przepływ od `IStorageFile`/wyboru pliku przez `AttachmentsWindow`, `OrderAttachmentManager` i `OrderAttachmentContentStore`. Zmień tylko elementy konieczne do usunięcia zgłoszonej przyczyny.
3. Ponawiaj całą próbę otwarcia i skopiowania źródła po ERROR_SHARING_VIOLATION/LOCK_VIOLATION, także gdy błąd nastąpi w trakcie odczytu. Każda nieudana próba ma usuwać częściowy plik.
4. Okno ponawiania ma być ograniczone, ale wyraźnie dłuższe niż obecne 0,15 s: łącznie co najmniej 5 sekund i nie więcej niż 10 sekund. Po wyczerpaniu prób pokaż obecny czytelny komunikat po polsku.
5. Nie duplikuj załącznika, nie pozostawiaj plików `.part`, nie zmieniaj limitów, kolejności, metadanych, formatu PDF ani działania macOS.
6. Dodaj deterministyczne testy:
   - kilka kolejnych błędów udostępniania przy otwieraniu, potem sukces,
   - błąd udostępniania podczas częściowego odczytu, potem sukces,
   - wyczerpanie całego okna prób i poprawny komunikat,
   - brak pozostałości `.part` i tylko jeden poprawny załącznik po sukcesie.
   Testy nie mogą realnie czekać 5–10 sekund; użyj wstrzykiwanego mechanizmu oczekiwania/zegara.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"`, build Release oraz `git diff --check`.
8. Zaktualizuj `.ai/report.md` i `.ai/handoff.md` dokładnym wynikiem. Ustaw handoff `COMPLETED` wyłącznie po zaliczeniu wszystkich testów i builda.
9. Po poprawnej walidacji worker może wykonać commit i push zgodnie z `AUTO_COMMIT_PUSH`.

## Zakazy

- Nie twórz jeszcze ZIP-a Windows; osobny pakiet testowy powstanie dopiero po opublikowaniu i walidacji poprawki.
- Nie zmieniaj `main`, COMMA WMS ani KOMI Animation Lab.
- Nie wykonuj resetu, rebase ani force push.
- Nie dodawaj nowych funkcji interfejsu.
