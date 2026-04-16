# Technical Foundation

## Wybrany stack

- Engine: Unity
- Editor line: Unity 6.3 LTS (`6000.3.x`)
- Render pipeline: URP
- XR stack: OpenXR + XR Interaction Toolkit + XR Interaction Simulator
- Input: Input System
- Primary language: C#
- Version control: Git
- IDE: Visual Studio 2022 lub JetBrains Rider

## Dlaczego ten stack

- `Unity` daje szybki workflow dla malego zespolu i lekkiej gry VR.
- `OpenXR` ogranicza vendor lock-in i wspiera sensowny PCVR workflow.
- `XR Interaction Toolkit` daje gotowe interactory, locomotion i sample do szybkiego prototypowania.
- `XR Interaction Simulator` jest krytyczny, bo nie ma headsetu na starcie.
- `URP` daje wystarczajaca jakosc i lepszy koszt wydajnosci niz ciezsze pipeline'y.

## Zaleznosci Unity

Pakiety pierwszego rzutu:

- `com.unity.inputsystem`
- `com.unity.xr.interaction.toolkit`
- `com.unity.xr.openxr`
- `com.unity.render-pipelines.universal`

Uwagi:

- `XR Interaction Toolkit` ma zaleznosc od `Input System`.
- `OpenXR` wlacza sie w `XR Plug-in Management`, samo dodanie pakietu nie wystarcza.
- `URP` jest pakietem core i jego wersja jest przypieta do wersji edytora.
- Import `Starter Assets` i `XR Interaction Simulator` z XRI samples jest czescia bootstrapu.

## Architektura zaleznosci runtime

- Logika szachow ma byc niezalezna od Unity XR.
- VR jest warstwa prezentacji i interakcji nad modelem gry.
- AI ma komunikowac sie przez interfejs, a nie przez bezposrednie wywolania z obiektow sceny.
- Kazda scena ma korzystac z jednego punktu startowego do konfiguracji runtime.

## Opcje dla logiki szachow

### Opcja A: wlasna logika C#

Plusy:

- pelna kontrola
- brak zewnetrznych runtime zaleznosci
- najczystsza integracja z Unity

Minusy:

- wyzsze ryzyko bledow w legal moves
- wiecej czasu na testy i edge case'y

### Opcja B: biblioteka C# jako adapter

Kandydaci do spike'a:

- `Geras1mleo/Chess` - MIT, C#, FEN/PGN, legal moves, events
- `rudzen/ChessLib` - MIT, C#, szybki move generator i niski poziom kontroli

Rekomendacja:

- zrobic adapter `IChessRulesAdapter`
- najpierw zrobic spike na `Geras1mleo/Chess`
- zostawic mozliwosc podmiany na implementacje wlasna, jesli integracja z Unity bedzie problematyczna

## Opcje dla AI

### Etap 1

- prosty debug bot lub losowy/legal-move bot

### Etap 2

- adapter UCI i zewnetrzny proces `Stockfish`

Uwagi:

- `Stockfish` jest mocny, ale ma licencje `GPL-3.0`
- trzeba go odizolowac adapterem i zdecydowac pozniej, czy odpowiada planowi komercyjnemu

## Lokalne blockery wykryte teraz

- brak `Unity Hub`
- brak `Unity Editor`
- brak potwierdzonego `Visual Studio` / `MSBuild`
- brak potwierdzonego aktywnego runtime `OpenXR`
- brak headsetu do realnych testow VR

## Polityka techniczna na start

- nie dodajemy multiplayera przed grywalnym solo MVP
- nie dodajemy hand trackingu przed stabilnym workflow z kontrolerami
- nie spinamy logiki gry z prefabami sceny
- nie wprowadzamy zewnetrznych pluginow VR, dopoki oficjalny stack Unity wystarcza
