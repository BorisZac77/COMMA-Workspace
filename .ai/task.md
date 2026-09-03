# Aktualne zadanie

- TASK_ID: WORKER-BLOCKED-RECOVERY-016
- STATUS: READY
- PROJECT: COMMA Workspace automation
- BRANCH: workspace-4.0
- BASE_HEAD_BEFORE_QUEUE: 3ff9879
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Add safe blocked-task recovery to Workspace worker
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md", ".ai/automation/worker.sh"]

## Cel

Usprawnić bezpieczny worker COMMA Workspace tak, aby zadanie zakończone przez Codexa statusem BLOCKED nie powodowało ciągłego logowania błędu „repository is dirty” i nie wymagało wielu ręcznych komend. Zachować rygorystyczną ochronę cudzych zmian i ograniczyć niekontrolowane ponowne uruchomienia Codexa zużywające kredyty.

## Wymagania

1. Przed działaniem przeczytaj w całości AGENTS.md oraz wszystkie pliki .ai. Potwierdź właściwy worktree, gałąź workspace-4.0, czysty status, relację historii i niezmieniony main = 4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a.
2. Zmień wyłącznie .ai/automation/worker.sh oraz raport i handoff.
3. Zachowaj domyślną zasadę: worker nie może automatycznie wykonywać zwykłego zadania na zastanym brudnym repozytorium.
4. Gdy uruchomiony przez worker Codex zakończy się poprawnie procesowo, ale .ai/handoff.md ma STATUS: BLOCKED:
   - zwaliduj, że wszystkie zmienione i nowe ścieżki mieszczą się w ALLOWED_PATHS_JSON oraz nie są ścieżkami blokowanymi;
   - zapisz poza repozytorium, w STATE_DIR, bezpieczny znacznik z TASK_ID, gałęzią, HEAD oraz deterministycznym fingerprintem dokładnego stanu zmian;
   - nie wykonuj commit ani push;
   - zakończ cykl kontrolowanym statusem i jednoznacznym komunikatem „blocked task preserved; waiting for approved recovery”.
5. W kolejnych cyklach, jeśli repozytorium jest brudne i istnieje dokładnie pasujący znacznik BLOCKED (TASK_ID, gałąź, HEAD i fingerprint bez zmian), nie uruchamiaj Codexa ponownie, nie zużywaj kolejnych kredytów i nie spamuj błędem. Zaloguj krótki stan oczekiwania i zakończ kodem 0.
6. Dodaj jawny tryb --resume-blocked. Może on wznowić zadanie dokładnie jeden raz tylko wtedy, gdy:
   - znacznik BLOCKED istnieje i pasuje do TASK_ID, gałęzi oraz HEAD;
   - lokalny .ai/task.md jest identyczny z wersją w HEAD, aby lokalna zmiana allowlisty nie mogła rozszerzyć uprawnień;
   - wszystkie zastane zmiany nadal przechodzą walidację względem zaufanej allowlisty zadania;
   - nie ma konfliktów ani ścieżek blokowanych.
   W razie niespełnienia któregokolwiek warunku zatrzymaj się bez zmian, resetu, stash, rebase lub force push.
7. Po udanym wznowieniu stosuj istniejący mechanizm: wymagaj STATUS: COMPLETED, ponownie waliduj ścieżki, stage tylko zwalidowane pliki, commit/push zgodnie z zadaniem i oznacz TASK_ID jako handled. Usuń znacznik BLOCKED dopiero po udanym push.
8. Rozszerz --self-test o deterministyczne przypadki: obce brudne repo nadal odrzucone; poprawny marker rozpoznany; zmiana HEAD/TASK_ID/fingerprint odrzucona; niezmieniony BLOCKED nie uruchamia Codexa; brak możliwości lokalnego rozszerzenia allowlisty.
9. Uruchom bash -n .ai/automation/worker.sh oraz .ai/automation/worker.sh --self-test. Nie uruchamiaj testów aplikacji i nie zmieniaj jej kodu.
10. Sprawdź git diff --check oraz allowlistę. Ustaw COMPLETED wyłącznie po pomyślnych testach, aby safe worker wykonał commit i push.

## Zakazy

- Nie zmieniaj COMMA.App, COMMA.Core, COMMA.App.Tests, COMMA WMS ani KOMI.
- Nie zmieniaj main.
- Nie używaj resetu, stash, rebase, cherry-pick ani force push.
- Nie zezwalaj na automatyczne wznowienie niezmienionego zadania BLOCKED.
- Nie twórz ZIP-a ani aplikacji.
