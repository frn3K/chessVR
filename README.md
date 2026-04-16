# VR Chess

Repo preproduction dla gry szachowej VR budowanej w Unity.

## Status

- Repo lokalne zostalo zainicjalizowane.
- Dokumentacja startowa i plan techniczny sa gotowe.
- Unity Hub, Unity Editor i Visual Studio zostaly zainstalowane lokalnie.
- Licencja Unity jest aktywna i projekt otwiera sie z poziomu edytora.
- Pakiety `URP`, `Input System`, `XR Interaction Toolkit`, `XR Management` i `OpenXR` sa juz rozwiazane.
- Sample `Starter Assets` i `XR Interaction Simulator` zostaly zaimportowane do `Assets/Samples`.
- Istnieje pierwsza scena startowa `Sandbox`.
- Istnieje pierwszy czysty model domenowy szachow z testami EditMode.
- `Sandbox` pokazuje juz plansze i placeholderowe figury generowane z `BoardState`.
- Projekt jest przygotowany pod workflow `Unity + OpenXR + XR Interaction Toolkit + desktop/XR simulator`.

## Zalozenia bazowe

- Silnik: Unity
- Wersja edytora: Unity 6.3 LTS (`6000.3.x`)
- Platforma pierwszego MVP: Windows PCVR
- Tryb gry: solo vs AI
- Interakcja: chwyt figury i snap tylko na legalne pole
- Brak headsetu na starcie: obowiazkowy tryb debugowania na desktopie i `XR Interaction Simulator`

## Co jest w repo

- [docs/README.md](/C:/Users/fmied/Desktop/chessVR/docs/README.md) - indeks dokumentacji
- [docs/01-product-background.md](/C:/Users/fmied/Desktop/chessVR/docs/01-product-background.md) - wizja produktu i zalozenia
- [docs/02-technical-foundation.md](/C:/Users/fmied/Desktop/chessVR/docs/02-technical-foundation.md) - stack techniczny i zaleznosci
- [docs/03-unity-setup.md](/C:/Users/fmied/Desktop/chessVR/docs/03-unity-setup.md) - setup srodowiska i bootstrap projektu
- [docs/04-architecture.md](/C:/Users/fmied/Desktop/chessVR/docs/04-architecture.md) - architektura projektu
- [docs/05-mvp-scope.md](/C:/Users/fmied/Desktop/chessVR/docs/05-mvp-scope.md) - zakres MVP
- [docs/06-roadmap.md](/C:/Users/fmied/Desktop/chessVR/docs/06-roadmap.md) - roadmapa etapow
- [docs/07-todo.md](/C:/Users/fmied/Desktop/chessVR/docs/07-todo.md) - backlog i lista prac
- [docs/08-open-questions.md](/C:/Users/fmied/Desktop/chessVR/docs/08-open-questions.md) - otwarte pytania i domyslne decyzje
- [docs/09-sources.md](/C:/Users/fmied/Desktop/chessVR/docs/09-sources.md) - zrodla
- [Assets/Scripts/Domain/BoardState.cs](/C:/Users/fmied/Desktop/chessVR/Assets/Scripts/Domain/BoardState.cs) - pierwszy model zasad i stanu gry
- [Assets/Tests/EditMode/BoardStateTests.cs](/C:/Users/fmied/Desktop/chessVR/Assets/Tests/EditMode/BoardStateTests.cs) - testy EditMode dla domeny
- [Assets/Scripts/Runtime/BoardPresenter.cs](/C:/Users/fmied/Desktop/chessVR/Assets/Scripts/Runtime/BoardPresenter.cs) - generowanie planszy i figur w scenie
- [Assets/Scripts/Runtime/ChessGameController.cs](/C:/Users/fmied/Desktop/chessVR/Assets/Scripts/Runtime/ChessGameController.cs) - runtimeowy stan partii dla sceny
- [tools/check-environment.ps1](/C:/Users/fmied/Desktop/chessVR/tools/check-environment.ps1) - szybki audit lokalnego srodowiska

## Najwazniejsze decyzje

- Stawiamy na `Unity`, bo dla tej gry daje najlepszy balans szybkosci iteracji, stacku VR i ryzyka projektu.
- Uzywamy `OpenXR`, a nie vendor-specific SDK jako glownego fundamentu.
- Rzeczy VR sa projektowane tak, by logika szachow byla niezalezna od prezentacji i interakcji.
- AI ma byc podlaczane przez adapter. Na poczatku mozemy miec prosty bot lub tryb debug, a pozniej `Stockfish` za procesem UCI.

## Kolejnosc startu

1. Otworzyc repo w `Unity`.
2. Poczekac az Unity dokonczy import.
3. Otworzyc `Assets/Scenes/Sandbox.unity`.
4. Zweryfikowac scene i dopiac recznie ewentualne ustawienia `XR Plug-in Management`, jesli edytor o nie poprosi.
5. Zaczac budowe warstwy domenowej szachow.

## Uwagi

- `OpenXR runtime` na systemie nadal nie jest skonfigurowany, ale nie blokuje to pracy przez `XR Interaction Simulator`.
- Ten repo jest przygotowany tak, zebys mial komplet backgroundu i decyzji jeszcze przed pierwszym dniem implementacji.
