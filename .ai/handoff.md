# Stan przekazania

- TASK_ID: ATTACHMENT-PREVIEW-BINDING-008
- STATUS: READY
- LAST_ACTOR: ChatGPT
- NEXT_ACTOR: Automatic Codex worker
- BRANCH: workspace-4.0
- EXPECTED_HEAD_BEFORE_TASK: dec95aeee80c3e4f1ec684b56d4a36ae444eae64

## Stan
Widoczna kolejność jest odświeżana, ale bieżące użycie migawki `ToArray()` może odłączyć ListBox od późniejszych zmian kolekcji.

## Następny krok
Automatic Codex worker ma zachować natychmiastowy ruch widocznych wierszy i ponownie przypiąć żywą obserwowalną kolekcję, następnie wykonać testy i build.
