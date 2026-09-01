# Stan przekazania

- TASK_ID: DRAWING-DESCRIPTION-LAYOUT-004
- STATUS: READY
- LAST_ACTOR: ChatGPT
- NEXT_ACTOR: Automatic Codex worker
- BRANCH: workspace-4.0
- EXPECTED_HEAD_BEFORE_TASK: 6ec58939ef6992bdb64d964e53d0e23bd3816826

## Stan
Poprzednia implementacja zachowuje LOGOWANIE/KOLORYSTYKA i ogranicza rysunki do 70 mm, ale ręczny pełny test ujawnił dwie regresje układu opisu: historyczny przypadek drugiej strony z jednym rysunkiem ma 10 linii zamiast 3.

## Następny krok
Automatic Codex worker ma naprawić wyłącznie obliczenie pojemności opisu, zachować limit rysunków 70 mm, wykonać wskazane testy i build oraz opublikować commit dopiero po pomyślnej walidacji.
