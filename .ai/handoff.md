# Stan przekazania

- TASK_ID: ATTACHMENT-REORDER-006
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe worker
- BRANCH: workspace-4.0
- HEAD: f2441572c5c721a4f366cb8f02710c4b94e93b0b

## Stan
Naprawa kolejności załączników jest gotowa. Obsługa obu strzałek po udanym `Move` ustawia jawnie nowy indeks zaznaczenia, dzięki czemu przeniesiony załącznik pozostaje wybrany, a aktywność strzałek odpowiada jego pozycji. Test regresji sprawdza kolejność trzech różnych obiektów, pola `Order` oraz pierwszą i ostatnią pozycję.

## Walidacja
- Testy: PASS, 169/169.
- Build Release: PASS, 0 ostrzeżeń i 0 błędów.
- `git diff --check`: PASS.
- Zmienione ścieżki należą wyłącznie do `ALLOWED_PATHS_JSON`.
- Commit i push nie zostały wykonane.

## Następny krok
Safe worker powinien sprawdzić diff i wyniki walidacji, a następnie wykonać commit `Fix attachment reordering controls` oraz push zgodnie z automatycznym przepływem zadania.
