# Aktualne zadanie

- TASK_ID: CARD-DATA-DRAWING-HEIGHT-003
- STATUS: READY
- PROJECT: COMMA Workspace 4.1
- BRANCH: workspace-4.0
- EXPECTED_HEAD_AT_QUEUE_START: 520b58f5dfcbc4c304abe281a08a19c19f82def7
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Preserve production entries and limit drawing height
- ALLOWED_PATHS_JSON: ["COMMA.App/ViewModels/MainViewModel.cs", "COMMA.App/Layout/GarmentViewDescriptionLayout.cs", "COMMA.App/Services/Pdf/PdfStyles.cs", "COMMA.App.Tests/MainViewModelOrderTests.cs", "COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs", ".ai/report.md", ".ai/handoff.md"]

## Cel
Naprawić dwa potwierdzone problemy bez dodawania nowych funkcji:
1. Dane wpisane w sekcji LOGOWANIE/KOLORYSTYKA nie mogą znikać po wybraniu i dodaniu kolejnego rodzaju odzieży do tej samej karty produkcyjnej.
2. Wszystkie rysunki odzieży, niezależnie od liczby rzutów i strony, mają mieć maksymalnie 70 mm wysokości zamiast około 80 mm.

## Ustalona przyczyna pierwszego problemu
`OnSelectedProductChanged` buduje nowy obiekt `ProductionCard` i kopiuje podstawowe dane zlecenia, ale nie kopiuje `ProductionEntries`. Napraw tę ścieżkę w najprostszy sposób, zachowując:
- nazwę logo,
- wymiar,
- wszystkie kolory/nici,
- ich kolejność i numerację,
- niezależne obiekty kolekcji bez współdzielenia mutowalnego stanu.

## Wymagania dotyczące rysunków
- Wysokość 70 mm jest limitem dla 1, 2, 3 i 4 rzutów.
- Zachowaj proporcje obrazu; nie rozciągaj rysunków.
- Jeśli szerokość komórki wymusza mniejszy obraz, obraz może pozostać mniejszy.
- Podgląd aplikacji i wygenerowany PDF muszą korzystać z tej samej geometrii.
- Nie zmieniaj rozmiaru czcionki opisów pod rysunkami (pozostaje 10 pt).
- Nie zmieniaj układu stron, ramek, nagłówków ani innych wymiarów.

## Testy regresyjne
1. Dodaj test potwierdzający, że po zmianie wybranego produktu/rodzaju odzieży istniejące wpisy LOGOWANIE/KOLORYSTYKA pozostają identyczne.
2. Test musi obejmować co najmniej dwa logowania, wymiar oraz wiele kolorów i potwierdzać brak współdzielenia mutowalnych kolekcji między starą i nową kartą.
3. Dodaj lub zaktualizuj testy geometrii dla 1, 2, 3 i 4 rzutów: maksymalna wysokość wynosi 70 mm w punktach PDF z rozsądną tolerancją.
4. Potwierdź w wygenerowanym PDF, że obrazy nie przekraczają 70 mm i zachowują proporcje.
5. Zachowaj wszystkie dotychczasowe testy.

## Walidacja
1. Uruchom pełne `dotnet test "COMMA Workspace 4.0.sln"`.
2. Uruchom `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore`.
3. Uruchom `git diff --check`.
4. Sprawdź, że zmienione ścieżki są dokładnie w `ALLOWED_PATHS_JSON`.
5. Potwierdź, że `main` nadal wskazuje `4efdb3036a4f0e0e77ea7d4f3cbf2878c122a85a`.
6. Nie uruchamiaj `build_app.sh`, nie zapisuj na Pulpit i nie podmieniaj obecnej aplikacji 4.1 w tym zadaniu.

## Zakazy
- Nie zmieniaj gałęzi `main`.
- Nie zmieniaj COMMA WMS ani KOMI Animation Lab.
- Nie dodawaj żadnych innych funkcji.
- Nie wykonuj resetu, rebase ani force push.

## Git
Nie wykonuj commit ani push bezpośrednio. Bezpieczny worker zrobi to po walidacji allowlisty i statusie COMPLETED.
