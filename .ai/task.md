# Aktualne zadanie

- TASK_ID: INTEGRATION-003
- STATUS: READY
- PROJECT: COMMA Workspace 4.0
- BRANCH: workspace-4.0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Complete automatic worker end-to-end test
- ALLOWED_PATHS_JSON: [".ai/report.md", ".ai/handoff.md"]

## Cel
Potwierdzić pełny automatyczny obieg ChatGPT → GitHub → LaunchAgent → codex exec → GitHub → ChatGPT bez ręcznego uruchamiania Codexa w Riderze.

## Dozwolone zmiany
- `.ai/report.md`
- `.ai/handoff.md`

Zmiany w kodzie aplikacji i innych plikach są zabronione.

## Instrukcja
1. Wykonaj kontrole wstępne z `.ai/context.md`.
2. Potwierdź, że zadanie ma identyfikator `INTEGRATION-003` i zostało uruchomione przez automatyczny worker.
3. Nie zmieniaj kodu aplikacji i nie uruchamiaj builda ani testów aplikacji — to test transportu.
4. Zaktualizuj `.ai/report.md`:
   - ustaw `STATUS: COMPLETED`,
   - zapisz czas rozpoczęcia i zakończenia,
   - zapisz katalog, gałąź i HEAD,
   - potwierdź odczyt `AGENTS.md`, kontekstu i zadania,
   - potwierdź brak zmian poza dwoma dozwolonymi plikami.
5. Zaktualizuj `.ai/handoff.md`:
   - `STATUS: COMPLETED`,
   - `LAST_ACTOR: Automatic Codex worker`,
   - `NEXT_ACTOR: ChatGPT`.
6. Nie wykonuj samodzielnie operacji Git. Commit i push wykona worker po walidacji allowlisty.
