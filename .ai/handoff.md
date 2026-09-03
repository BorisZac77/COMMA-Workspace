# Stan przekazania

- TASK_ID: PACKAGE-WINDOWS-4.1E-PC-TEST-017
- STATUS: COMPLETED
- LAST_ACTOR: Codex
- NEXT_ACTOR: PC test
- BRANCH: workspace-4.0
- HEAD: 634d72b211d3925c37e17b436ebddbcab90b45b8

## Stan

Fresh Windows x64 self-contained publish został wykonany i tymczasowe archiwum ZIP zostało zweryfikowane, ale nie można było zapisać wymaganej nowej paczki na Pulpicie z powodu ograniczenia uprawnień środowiska: `Operation not permitted` dla `/Users/Boris/Desktop/COMMA Workspace 4.1E Windows x64.zip`.

Nie należy oznaczać poprawki jako potwierdzonej na PC. Nie utworzono docelowego ZIP, nie zmieniono kodu ani konfiguracji i nie wykonano commita/pushu.

## Zweryfikowane dane tymczasowego archiwum

- Tymczasowa ścieżka: `/private/tmp/comma-4.1e-win-x64.E8DJb5/COMMA Workspace 4.1E Windows x64.zip`.
- Rozmiar: `98,688,922` B; SHA-256: `8eb208502052b76c28badf1c0a443f6761c4a54478db47f5ac722eec4efc0c64`; wpisy: `283`.
- `unzip -t`: PASS; jeden katalog główny; `COMMA Workspace 4.1E Windows x64/COMMA.App.exe`: obecny dokładnie raz.
- Źródła odpowiadają HEAD `634d72b211d3925c37e17b436ebddbcab90b45b8`, który zawiera `3ff9879` z `WindowsAttachmentSourceStager` i `SHFileOperation`.

## Następny krok

Zapewnić uprawnienie do utworzenia nowego pliku na Pulpicie lub ręcznie przenieść zweryfikowane archiwum tymczasowe pod dokładnie wymaganą nazwę, nie nadpisując istniejącego pliku. Następnie ponownie sprawdzić ZIP na docelowej ścieżce przed przekazaniem do PC testu.

## Ręczne dokończenie pakietu

Interaktywny Terminal utworzył docelowy pakiet:
`/Users/Boris/Desktop/COMMA Workspace 4.1E Windows x64.zip`.

- Release publish `win-x64`, self-contained: PASS.
- Integralność ZIP: PASS.
- Jeden katalog główny: PASS.
- `COMMA.App.exe`: PASS.
- Liczba wpisów: `283`.
- Rozmiar: `98674206` bajtów.
- SHA-256: `16e62841585e42a41527849d1aed3769e449809684feb94e92422557f07a62c6`.
- Następny krok: test `Vandeputte-10.pdf` bezpośrednio z `Z:` na PC.
