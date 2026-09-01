# Aktualne zadanie

- TASK_ID: INTEGRATION-002
- STATUS: READY
- PROJECT: COMMA Workspace 4.0
- BRANCH: workspace-4.0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Install automatic ChatGPT Codex worker
- ALLOWED_PATHS_JSON: [".ai/automation/", ".ai/report.md", ".ai/handoff.md"]

## Cel
Zainstalować na MacBooku bezpieczny automat odbierający zadania zapisane przez ChatGPT na GitHubie i uruchamiający Codexa lokalnie bez ręcznego kopiowania poleceń do Ridera.

## Dozwolone zmiany w repozytorium
- `.ai/automation/`
- `.ai/report.md`
- `.ai/handoff.md`

Nie zmieniaj kodu aplikacji.

## Dozwolone lokalne elementy poza repozytorium
- `~/Library/LaunchAgents/pl.comma.workspace4.codex-worker.plist`
- `~/Library/Application Support/COMMA AI/Workspace4/`
- lokalny `.git/info/exclude`

## Wymagania

### 1. Kontrola środowiska
Sprawdź i zapisz w raporcie:
- `codex --version`
- rzeczywistą składnię z `codex exec --help`
- dostępność `git`, `launchctl`, `python3`
- aktywne uwierzytelnienie Codex CLI bez odczytywania lub kopiowania plików z tokenami.

Nie odczytuj, nie kopiuj i nie zapisuj żadnych sekretów ani `auth.json`.

### 2. Pliki automatu
Utwórz w `.ai/automation/`:
- `worker.sh` — pojedynczy bezpieczny cykl odbioru zadania,
- `install.sh` — idempotentna instalacja i aktywacja LaunchAgenta,
- `uninstall.sh` — bezpieczne zatrzymanie automatu bez usuwania kodu lub danych projektu,
- `README.md` — krótka instrukcja działania, kontroli stanu i zatrzymania.

Skrypty muszą działać z dokładnym repo:
`/Users/Boris/RiderProjects/COMMA Workspace 4.0`
oraz wyłącznie z gałęzią:
`workspace-4.0`.

### 3. Zasady worker.sh
Worker ma:
1. Używać blokady, aby nigdy nie działały dwa procesy równocześnie.
2. Zapisywać logi i stan wyłącznie w `~/Library/Application Support/COMMA AI/Workspace4/`.
3. Kończyć pracę bez zmian, jeśli repozytorium jest nieczyste, aktywna jest inna gałąź albo wystąpi konflikt/divergencja.
4. Wykonywać wyłącznie `git fetch` i bezpieczny fast-forward; nigdy rebase, reset, force push ani automatyczne rozwiązywanie konfliktów.
5. Uruchamiać zadanie tylko, gdy `.ai/task.md` ma `STATUS: READY` i nowy `TASK_ID`.
6. Używać `codex exec` w trybie `workspace-write`, bez YOLO/danger-full-access i bez interaktywnych zgód. Dopasuj dokładne flagi do lokalnego `codex exec --help`.
7. Polecić Codexowi przeczytanie `AGENTS.md`, `.ai/context.md` i `.ai/task.md`, wykonanie zadania, testów oraz aktualizację raportu i handoff.
8. Po zakończeniu sprawdzić wszystkie zmienione i nowe śledzone pliki względem `ALLOWED_PATHS_JSON`. Prefiks zakończony `/` oznacza dozwolony katalog; pozostałe wpisy oznaczają dokładne pliki.
9. Zawsze blokować pliki i katalogi zawierające sekrety lub artefakty lokalne, w szczególności: `.codex`, `.env`, `auth.json`, `output`, `bin`, `obj`, klucze i certyfikaty.
10. Nie commitować ani nie pushować, jeśli zmieniono cokolwiek spoza allowlisty, testy/zadanie nie powiodły się, handoff nie ma `STATUS: COMPLETED` lub `AUTO_COMMIT_PUSH` nie wynosi `YES`.
11. Jeśli walidacja przejdzie, dodać wyłącznie dozwolone pliki, wykonać commit z `COMMIT_MESSAGE` i push wyłącznie na `origin/workspace-4.0`.
12. Jeśli push zostanie odrzucony, zatrzymać się bez force push i zachować stan do ręcznej kontroli.
13. Oznaczać TASK_ID jako obsłużony dopiero po udanym pushu.
14. Zwracać czytelny kod wyjścia i log bez ujawniania sekretów.

### 4. LaunchAgent
- Uruchamiaj worker co 60 sekund.
- Nie uruchamiaj nowego procesu, jeżeli poprzedni nadal działa.
- Automat ma działać po ponownym zalogowaniu do macOS.
- Instalacja ma być idempotentna.
- Przed aktywacją zasiej lokalny stan bieżącym TASK_ID `INTEGRATION-002`, żeby automat nie uruchomił ponownie zadania instalacyjnego.

### 5. Testy
Przed instalacją:
- sprawdź składnię skryptów,
- przetestuj parsowanie pól zadania i allowlisty,
- przetestuj blokowanie niedozwolonej ścieżki,
- przetestuj ochronę przed ponownym wykonaniem tego samego TASK_ID,
- przetestuj zachowanie na nieczystym repozytorium bez modyfikowania kodu aplikacji.

Po instalacji:
- potwierdź stan LaunchAgenta,
- wykonaj bezpieczny cykl smoke-test, który nie uruchomi ponownie INTEGRATION-002,
- potwierdź, że repozytorium jest czyste przed utworzeniem końcowego raportu.

## Zakończenie
1. Zaktualizuj `.ai/report.md` i `.ai/handoff.md`.
2. Commit ma obejmować wyłącznie dozwolone ścieżki.
3. Wykonaj push wyłącznie `workspace-4.0`.
4. W raporcie podaj: wersję Codexa, zainstalowane ścieżki, wyniki testów, stan LaunchAgenta, końcowy SHA oraz komendy bezpiecznego sprawdzenia i zatrzymania automatu.
