# Aktualne zadanie

- TASK_ID: ATTACHMENT-WINDOWS-CLOSE-BEFORE-MOVE-018
- STATUS: READY
- PROJECT: COMMA Workspace 4.1F attachment import fix
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: b785193d5e999ae80f7164794e77165b3d87da99
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Close attachment temp file before move
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", "COMMA.App/Services/Attachments/OrderAttachmentContentStore.cs", "COMMA.App.Tests/OrderAttachmentTests.cs"]

## Potwierdzona przyczyna

Rzeczywisty test pakietu 4.1E na PC nadal zakończył się komunikatem o pliku używanym przez inny program. Komunikat zawierał lokalną, losową nazwę stagingową `0652353ab244417aab3bc3a832f009d6.pdf`, więc kopiowanie z `Z:` przez Windows Shell zakończyło się powodzeniem.

Błąd jest dalej w `OrderAttachmentContentStore.ImportStream`: docelowy plik `.part` jest otwierany przez `using var destination` z `FileShare.None`, a `File.Move(temporaryPath, destinationPath)` jest wykonywany przed końcem zakresu tego `using`. Na Windows aplikacja próbuje więc zmienić nazwę własnego, nadal otwartego pliku i otrzymuje sharing violation (Win32 32). macOS dopuszcza zmianę nazwy otwartego pliku, dlatego problem nie występował na MacBooku. Zewnętrzny retry w `ImportFile` błędnie prezentuje tę wewnętrzną blokadę jako problem odczytu wybranego źródła.

## Cel

Zamknąć i zwolnić strumień docelowego pliku `.part` przed wywołaniem `File.Move`, zachowując dotychczasową integralność, haszowanie, limity i sprzątanie. Dodać deterministyczny test regresyjny potwierdzający kolejność dispose-before-move niezależnie od systemu operacyjnego.

## Wymagania

1. Przed działaniem przeczytaj w całości `AGENTS.md` i wszystkie pliki w `.ai`. Potwierdź właściwy worktree, gałąź `workspace-4.0`, czysty status, relację historii oraz niezmieniony `main` = `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
2. Potwierdź, że `BASE_HEAD_BEFORE_QUEUE` jest przodkiem bieżącego HEAD oraz że commit `3ff9879` ze stagingiem Windows nadal jest w historii.
3. W `OrderAttachmentContentStore.ImportStream` ogranicz zakres strumienia zapisującego `.part` tak, aby został bezwarunkowo zamknięty po pełnym zapisie i `Flush(flushToDisk: true)`, ale przed `File.Move(temporaryPath, destinationPath)`.
4. Zachowaj obliczanie długości i SHA-256, limity rozmiaru, atomowe przejście z `.part` do pliku docelowego, aktualizację `paths` dopiero po udanym przeniesieniu oraz sprzątanie po każdym błędzie.
5. Nie dodawaj żadnego retry, opóźnienia, `File.Copy`, `FileStream`-obejścia ani kolejnej warstwy stagingu. Nie zmieniaj istniejącego Windows Shell `SHFileOperation`, `AttachmentsWindow`, managera ani komunikatów błędów.
6. Dodaj deterministyczny test, który zawiedzie przy starej kolejności i potwierdzi, że operacja przeniesienia jest wywoływana dopiero po dispose strumienia docelowego. Test nie może zależeć od semantyki rename na macOS, realnego Windows, `Z:`, sieci ani UI. Jeżeli potrzebny jest seam testowy, dodaj minimalną wewnętrzną abstrakcję/delegat wyłącznie w `OrderAttachmentContentStore.cs`, bez zmiany publicznego API i bez wpływu na produkcyjny przepływ.
7. Zachowaj wszystkie istniejące testy retry bez rozszerzania mechanizmu retry; ich usuwanie lub przebudowa nie należy do tego zadania.
8. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln" --no-restore` dokładnie raz oraz `dotnet build "COMMA Workspace 4.0.sln" --configuration Release --no-restore`. Jeżeli test runner zostanie obiektywnie zablokowany przez znany sandboxowy `TcpListener`, nie próbuj ponownie i zapisz dokładny wynik; mimo to wykonaj Release build.
9. Sprawdź `git diff --check` oraz ścisłą allowlistę. Ustaw `COMPLETED` tylko przy pomyślnej dostępnej walidacji. Po sukcesie safe worker ma wykonać commit i push.
10. Nie twórz ZIP-a ani aplikacji macOS w tym zadaniu. Nowy pakiet Windows zostanie przygotowany dopiero po zakończeniu i ocenie tej poprawki.

## Kryterium odbioru implementacji

- Strumień zapisujący `.part` jest zamknięty przed `File.Move`.
- Test regresyjny potwierdza tę kolejność na każdym systemie.
- Pełne testy i Release build przechodzą albo pojedyncze uruchomienie testów jest dokładnie udokumentowane jako obiektywnie zablokowane przez sandbox.
- Nie zmieniono stagingu Windows Shell ani żadnego przepływu poza magazynem zawartości załącznika.

## Następny krok po zakończeniu

Po udanym commicie i pushu przygotować osobne zadanie pakietowe 4.1F, a następnie ponownie sprawdzić na PC ten sam PDF bezpośrednio z `Z:`. Do czasu rzeczywistego testu nie opisywać poprawki jako potwierdzonej na PC.

## Zakazy

- Nie zmieniaj `main`, COMMA WMS ani KOMI.
- Nie wychodź poza `ALLOWED_PATHS_JSON`.
- Nie zmieniaj UI, formatu danych, PDF, folderów biblioteki ani pakietów.
- Nie używaj resetu, stash, rebase, cherry-pick ani force push.
- Nie twórz kolejnego ZIP-a w tym zadaniu.
