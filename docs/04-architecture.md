# Architecture

## Zasada glowna

Warstwa logiki szachow musi byc niezalezna od Unity scene graph i systemu XR. Dzieki temu mozemy testowac partie, AI i legal moves bez headsetu i bez sceny.

## Warstwy projektu

### 1. Application

Odpowiada za:

- start gry
- ladowanie scen
- konfiguracje runtime
- przejscia miedzy menu, sandboxem i partia

### 2. Chess Domain

Odpowiada za:

- stan planszy
- tury
- legalne ruchy
- roszade, en passant, promocje
- check, mate, stalemate
- serializacje FEN i PGN

### 3. Match Orchestration

Odpowiada za:

- rozpoczecie partii
- zmiane tury
- komunikacje z AI
- restart i koniec gry

### 4. Board Presentation

Odpowiada za:

- wizualna plansze
- pozycje figur
- highlight legalnych pol
- animacje snapowania i resetu

### 5. VR Interaction

Odpowiada za:

- chwyt figur
- detekcje dropu
- mapowanie pozycji kontrolera na pole planszy
- desktop simulator support

### 6. UI

Odpowiada za:

- menu glowne
- pause menu
- stan gry i komunikaty
- wybor strony i restart

## Kluczowe interfejsy

- `IChessRulesAdapter`
- `IChessMatchService`
- `IAiMoveProvider`
- `IBoardCoordinateMapper`
- `IPieceInteractionService`

## Przeplyw jednej tury

1. Gracz podnosi figure.
2. System pyta `IChessRulesAdapter` o legalne cele.
3. Legalne pola sa podswietlane.
4. Gracz puszcza figure.
5. `IPieceInteractionService` mapuje drop na konkretne pole.
6. `IChessMatchService` waliduje ruch.
7. Gdy ruch jest legalny, `Board Presentation` wykonuje snap i aktualizuje scene.
8. Gdy ruch jest nielegalny, figura wraca na stare pole.
9. Po ruchu gracza `IAiMoveProvider` liczy odpowiedz.
10. Odpowiedz AI jest wykonana tym samym pipeline'em, ale bez chwytu.

## Zalecana struktura katalogow

- `Assets/Scenes`
- `Assets/Scripts/Application`
- `Assets/Scripts/Domain`
- `Assets/Scripts/AI`
- `Assets/Scripts/Board`
- `Assets/Scripts/Interaction`
- `Assets/Scripts/UI`
- `Assets/Prefabs`
- `Assets/Materials`
- `Assets/Art`
- `Assets/Audio`

## Sceny

- `Bootstrap`
- `MainMenu`
- `Sandbox`
- `MatchArena`

Na samym poczatku wystarczy `Sandbox`, a potem `MatchArena`.

## Prefaby pierwszego rzutu

- `BoardRoot`
- `SquareAnchor`
- `ChessPieceView`
- `XRRigRoot`
- `DesktopDebugRig`
- `LegalMoveMarker`

## Najwazniejsza zasada implementacyjna

Kazdy element sceny ma byc "glupi" wzgledem zasad szachow. Scene ma tylko wyswietlac i zbierac input. Prawda o stanie partii jest w warstwie domenowej.
