# Stan przekazania

- TASK_ID: ATTACHMENT-PREVIEW-REORDER-007
- STATUS: READY
- LAST_ACTOR: ChatGPT
- NEXT_ACTOR: Automatic Codex worker
- BRANCH: workspace-4.0
- EXPECTED_HEAD_BEFORE_TASK: db1316d8e8198fd854133a48ac730b1da0f95e0f

## Stan
Kolejność załączników jest poprawnie zmieniana w danych i PDF, lecz widoczna lista w otwartym oknie aplikacji pozostaje w starej kolejności.

## Następny krok
Automatic Codex worker ma zdiagnozować odświeżanie ListBox po `ObservableCollection.Move`, zastosować najmniejszą poprawkę, uruchomić pełne testy i build oraz zakończyć handoff statusem COMPLETED.
