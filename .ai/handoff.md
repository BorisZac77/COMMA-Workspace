# Stan przekazania

- TASK_ID: DRAWING-DESCRIPTION-LAYOUT-004
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: Safe validation/commit worker
- BRANCH: `workspace-4.0`
- HEAD_BEFORE_TASK: `4de1142ecd4217a222b5163abf8627e963fc28f5`
- HEAD_AFTER_TASK: `4de1142ecd4217a222b5163abf8627e963fc28f5`

## Stan
Pojemność opisu dla układu 1–2 rzutów pozostaje oparta na historycznej rezerwie wysokości obrazu, podczas gdy rysunek jest renderowany z limitem 70 mm. Przypadek Plopsa na drugiej stronie ponownie kończy się na trzech pełnych liniach w lewej komórce. Podgląd i PDF pozostają zgodne, a proporcje obrazów i czcionka opisu 10 pt nie zostały zmienione.

Dodano minimalny test regresyjny, który bezpośrednio potwierdza rozdzielenie limitu renderowania 70 mm od wysokości referencyjnej używanej do pojemności tekstu.

## Walidacja
- Testy celowane: PASS, 3/3.
- Pełne testy: PASS, 168/168.
- Build Release: PASS, 0 ostrzeżeń, 0 błędów.
- `git diff --check`: PASS.
- `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Następny krok
Bezpieczny worker może zweryfikować allowlistę i utworzyć commit `Preserve description capacity with 70 mm drawings`. Codex nie wykonał commit ani push.
