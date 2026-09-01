# Raport Codexa

- TASK_ID: DRAWING-DESCRIPTION-LAYOUT-004
- STATUS: COMPLETED
- STARTED_AT: 2026-09-01T19:33:00+0200
- COMPLETED_AT: 2026-09-01T19:41:18+0200
- REPOSITORY_ROOT: `/Users/Boris/RiderProjects/COMMA Workspace 4.0`
- BRANCH: `workspace-4.0`
- HEAD_BEFORE: `4de1142ecd4217a222b5163abf8627e963fc28f5`
- HEAD_AFTER: `4de1142ecd4217a222b5163abf8627e963fc28f5`

## Kontrole wstępne
- `pwd` i `git rev-parse --show-toplevel` wskazały wymagane repozytorium.
- Aktywna gałąź: `workspace-4.0`.
- Stan roboczy przed zmianami był czysty.
- Bieżący HEAD był nowszy od `EXPECTED_HEAD_AT_QUEUE_START`: po punkcie `6ec5893` historia zawierała kolejkę zadania, commit odzyskujący oraz merge; nie cofano tych zmian.
- `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Przyczyna regresji
`GetPdfTextHeight` wyznaczał pojemność opisu przez odjęcie `GetPdfRenderedImageHeight`. Po ograniczeniu renderowanej wysokości rysunku do 70 mm odejmowana wartość zmalała, dlatego całe zwolnione miejsce zostało automatycznie uznane za dostępne dla tekstu. Historyczny przypadek drugiej strony z jednym rysunkiem w lewej komórce wzrósł przez to z 3 do 10 linii.

Na bieżącym HEAD znajdowała się już minimalna naprawa tego sprzężenia: dla układu 1–2 rzutów pojemność opisu odejmuje historyczną referencyjną rezerwę obrazu, natomiast rzeczywisty obraz nadal renderuje się z limitem 70 mm i zachowaniem proporcji. Układy 3–4 rzutów nadal korzystają ze swojej dotychczasowej geometrii dynamicznej. Podgląd i PDF korzystają z tej samej geometrii tekstu.

## Wykonane zmiany
- Dodano minimalny test `TwoViewDescriptionCapacityKeepsLegacyImageReservationAtSeventyMillimetreRendering` w `COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs`.
- Test niezależnie potwierdza limit renderowania 70 mm, historyczną rezerwę używaną do obliczenia pojemności tekstu oraz brak przejęcia zwolnionej wysokości przez opis.
- Nie zmieniono oczekiwań testów Plopsa, czcionki 10 pt, układu stron ani `MainViewModel.cs`.

## Testy i walidacja
- Dwa wskazane testy przed zmianą: PASS, 2/2.
- Dwa wskazane testy wraz z nowym testem rozdzielenia geometrii: PASS, 3/3.
- `dotnet test "COMMA Workspace 4.0.sln" --no-restore -m:1`: PASS, 168/168, 0 pominiętych.
- `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore -m:1`: PASS, 0 ostrzeżeń, 0 błędów.
- `git diff --check`: PASS.
- Wszystkie zmienione ścieżki należą do `ALLOWED_PATHS_JSON`.
- `main`: potwierdzony wymagany hash `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.

## Problemy lub ryzyka
Brak znanych problemów. VSTest nie został zablokowany przez sandbox.

## Podsumowanie
Regresja pojemności opisu jest naprawiona i jawnie zabezpieczona testem rozdzielającym limit renderowania obrazu od referencyjnej przestrzeni walidacji tekstu. Zadanie gotowe do walidacji allowlisty oraz commita przez bezpiecznego workera.
