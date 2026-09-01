# Aktualne zadanie

- TASK_ID: PDF-FONT-001
- STATUS: READY
- PROJECT: COMMA Workspace 4.0
- BRANCH: workspace-4.0
- AUTO_COMMIT_PUSH: YES
- COMMIT_MESSAGE: Align PDF colour and drawing description fonts
- ALLOWED_PATHS_JSON: ["COMMA.App/Services/Pdf/HandwrittenSection.cs", "COMMA.App/Services/Pdf/PdfStyles.cs", "COMMA.App/Layout/GarmentViewDescriptionLayout.cs", "COMMA.App.Tests/OrderPdfGeneratorTests.cs", "COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs", ".ai/report.md", ".ai/handoff.md"]

## Cel
Ujednolicić typowy rozmiar tekstu w generowanym PDF: wpisy w sekcji KOLORYSTYKA oraz opisy umieszczane pod rysunkami odzieży mają być renderowane w rozmiarze 10.

## Dozwolone zmiany
- `COMMA.App/Services/Pdf/HandwrittenSection.cs`
- `COMMA.App/Services/Pdf/PdfStyles.cs`
- `COMMA.App/Layout/GarmentViewDescriptionLayout.cs`
- `COMMA.App.Tests/OrderPdfGeneratorTests.cs`
- `COMMA.App.Tests/GarmentViewDescriptionLayoutTests.cs`
- `.ai/report.md`
- `.ai/handoff.md`

Nie wolno zmieniać innych plików, czcionek pól edycyjnych aplikacji, układu sekcji, wymiarów rysunków, danych użytkownika ani kompatybilności dokumentów.

## Wymagania
1. W typowym polu kolorów (np. pięć krótkich wpisów `1. 1050`, `2. 20202`) zarówno numer, jak i wartość mają w wygenerowanym PDF rozmiar 10. Dotychczasowe 8.5 ma zostać zwiększone.
2. Krótkie i standardowe opisy pod rysunkami mają w PDF rozmiar 10 zamiast 11.
3. Podgląd opisu korzystający ze wspólnego mechanizmu ma pozostać zgodny skalą z PDF.
4. Zachowaj bezpieczne adaptacyjne zmniejszanie czcionki dla wyjątkowo dużej liczby kolorów, bardzo długich wartości i opisów, które inaczej nie zmieściłyby się w przeznaczonym polu. Nie wolno ucinać tekstu ani powodować przepełnienia.
5. Nie zmieniaj grubości czcionek: numer koloru pozostaje pogrubiony, wartość koloru pozostaje zwykła. Nie zmieniaj pozostałych napisów.
6. Dodaj lub zaktualizuj testy potwierdzające co najmniej:
   - typowe krótkie wpisy kolorów są fizycznie zapisane w PDF w rozmiarze 10,
   - krótki opis pod rysunkiem jest fizycznie zapisany w PDF w rozmiarze 10,
   - długie lub gęste treści nadal mieszczą się dzięki istniejącemu mechanizmowi bezpieczeństwa,
   - istniejące mapowanie opisów do właściwych rysunków pozostaje bez zmian.
7. Wykonaj pełne testy rozwiązania oraz build Release:
   - `dotnet test "COMMA Workspace 4.0.sln" -c Release`
   - `dotnet build "COMMA Workspace 4.0.sln" -c Release --no-restore`
8. Jeśli środowisko pozwala, wyrenderuj reprezentatywny PDF z pięcioma krótkimi kolorami i opisem pod rysunkiem oraz sprawdź, czy tekst nie nachodzi na inne elementy.
9. Zaktualizuj `.ai/report.md` i `.ai/handoff.md` ze stanem `COMPLETED`, listą zmienionych plików, wynikami testów i QA. Jeśli zadanie jest zablokowane, ustaw odpowiedni status i opisz blokadę.
10. Nie wykonuj samodzielnie operacji Git. Commit i push wykona worker po walidacji allowlisty.
