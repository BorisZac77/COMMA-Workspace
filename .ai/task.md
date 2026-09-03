# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-LOCAL-STAGE-015
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: e0a41bffcb3857d6d847acaedc7ad2ec2168c696
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Stage Windows network attachments locally
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Attachments/WindowsAttachmentSourceStager.cs", "COMMA.App/Views/AttachmentsWindow.axaml.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Cel

Naprawić import załącznika z mapowanego dysku Windows (np. `Z:\\...`) bez ręcznego kopiowania przez użytkownika. Potwierdzone: ten sam PDF można ręcznie skopiować z `Z:` do lokalnego katalogu w Eksploratorze Windows, więc aplikacja ma przed importem wykonać automatyczne lokalne staging-copy za pomocą mechanizmu Windows Shell, a następnie importować wyłącznie lokalną kopię.

## Wymagania

1. Przed działaniem przeczytaj `AGENTS.md` i wszystkie pliki w `.ai`. Potwierdź właściwy worktree, gałąź `workspace-4.0`, czysty status, relację historii oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Nie zmieniaj `COMMA.App/Services/Attachments/OrderAttachmentContentStore.cs`: poprzedni mechanizm retry nie rozwiązał problemu na PC. Nie dodawaj kolejnej pętli retry ani nie zmieniaj komunikatu jako substytutu naprawy.
3. Dodaj mały, testowalny `WindowsAttachmentSourceStager`. Tylko na Windows, dla ścieżki źródłowej użytej po wyborze pliku, ma on utworzyć unikalną lokalną kopię tymczasową z zachowaniem rozszerzenia przy użyciu Windows Shell file operation / natywnego mechanizmu kopiowania Windows zgodnego z ręcznym kopiowaniem w Eksploratorze — nie przez `FileStream`, `File.Copy` ani `OrderAttachmentContentStore`.
4. W `AttachmentsWindow.axaml.cs` użyj staging-copy wyłącznie dla Windows, zanim istniejący manager zaimportuje załącznik. Po udanym lub nieudanym imporcie usuń wyłącznie utworzoną kopię stagingową. Na macOS i innych systemach zachowaj obecny przepływ bez zmian.
5. Nie zmieniaj interfejsu, kolejności załączników, generowania PDF, formatu danych, folderów biblioteki ani pakietów.
6. Dodaj deterministyczne testy: brak stagingu poza Windows; delegowanie Windows Shell copy dla pliku z zachowaniem rozszerzenia; unikalny docelowy plik lokalny; sprzątanie stagingu po sukcesie i błędzie importu. Testy nie mogą wymagać realnego `Z:`, UI dialogu ani sieci.
7. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"` dokładnie raz oraz Release build. Jeżeli test runner zostanie zablokowany przez znany sandboxowy `TcpListener`, nie próbuj ponownie i zapisz dokładny wynik; mimo to skompiluj testy i build. Nie twórz ZIP-a w tym zadaniu.
8. Sprawdź `git diff --check` oraz allowlistę. Ustaw `COMPLETED` tylko przy pomyślnej walidacji dostępnej w środowisku i wykonaj commit/push. Jeśli walidacja jest obiektywnie zablokowana, opisz blokadę w raporcie i handoffie, bez rozszerzania zakresu.

## Kryterium odbioru na PC

Po opublikowaniu użytkownik wybiera ten sam `Vandeputte-10.pdf` bezpośrednio z `Z:`. Aplikacja ma dodać go bez komunikatu o użyciu przez inny program. Do czasu tego testu ręczne skopiowanie pliku z `Z:` do lokalnego folderu, a następnie dodanie lokalnej kopii, pozostaje sprawdzonym obejściem.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI.
- Nie wykonuj resetu, rebase, force push ani nie usuwaj istniejących paczek.
- Nie twórz Windows ZIP ani macOS app w tym zadaniu.
