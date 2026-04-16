# Unity Setup

## Cel

Doprowadzic repo do stanu, w ktorym mozna otworzyc projekt Unity, uruchomic scene przez desktop/XR simulator i zaczac implementacje.

## Krok 1: narzedzia

Zainstaluj:

- Unity Hub
- Unity 6.3 LTS (`6000.3.x`) z modulami Windows Build Support
- Visual Studio 2022 z workloadem `Game development with Unity`
- Git

Opcjonalnie:

- Rider
- SteamVR lub runtime producenta headsetu jako aktywny OpenXR runtime

Status na tej maszynie:

- `Unity Hub` zainstalowany
- `Unity 6000.3.12f1` zainstalowany
- `Visual Studio 2022 Community` zainstalowany
- aktywna licencja Unity
- projekt Unity zostal juz zbootstrapowany
- pakiety `Input System`, `OpenXR`, `XR Interaction Toolkit`, `URP` i `UGUI` sa rozwiazane
- sample `Starter Assets` i `XR Interaction Simulator` sa w `Assets/Samples`
- scena `Assets/Scenes/Sandbox.unity` istnieje
- brak aktywnego runtime `OpenXR`

## Krok 1.5: aktywacja licencji Unity

Na tej maszynie ten krok jest juz wykonany.

## Krok 2: bootstrap projektu Unity

Ten krok jest juz wykonany lokalnie, ale zostawiam go jako referencje:

1. Otworz Unity Hub.
2. Utworz nowy projekt typu `3D (URP)`.
3. Jako lokalizacje wybierz to repo: `C:\Users\fmied\Desktop\chessVR`.
4. Pozwol Unity wygenerowac standardowy szkielet `Assets`, `Packages`, `ProjectSettings`.
5. Otworz projekt i pozwol mu dokonczyc import.

## Krok 3: pakiety

W `Package Manager` dodaj:

- `Input System`
- `XR Interaction Toolkit`
- `OpenXR Plugin`

Nastepnie w `XR Interaction Toolkit` zaimportuj samples:

- `Starter Assets`
- `XR Interaction Simulator`

## Krok 4: projekt XR

W `Project Settings`:

- wlacz `Active Input Handling` dla `Input System` albo `Both`
- wlacz `OpenXR` w `XR Plug-in Management` dla platformy Windows
- przejdz przez `Project Validation` i napraw wszystkie bledy
- dodaj interaction profiles dla kontrolerow, ktore chcesz wspierac

Rekomendowane ustawienia startowe dla PCVR:

- render mode: `Single Pass Instanced`
- latency optimization: `Prioritize Input Polling`
- depth submission: `24-bit`

## Krok 5: scena testowa

Scena `Assets/Scenes/Sandbox.unity` zostala juz utworzona. Docelowo ma zawierac:

- `XR Origin`
- `XR Interaction Simulator`
- prosta podloge
- tymczasowa plansze 8x8
- dwa lub trzy placeholderowe pionki jako test chwytu

Cel tej sceny:

- zweryfikowac input
- zweryfikowac chwyt i drop
- zweryfikowac poruszanie bez headsetu

## Krok 6: podstawowe standardy projektu

- wszystkie scene sa ladowane z jednego miejsca startowego
- logika gry nie siedzi w prefabach bezposrednio
- kazda istotna warstwa ma osobny katalog
- naming ma byc prosty i przewidywalny

## Co da sie zrobic juz teraz bez Unity

- dopiac dokumentacje i decyzje architektoniczne
- przygotowac repo
- przygotowac backlog i plan sprintow
- przygotowac audit srodowiska
- zaplanowac warstwy kodu i interfejsy

## Czego nie da sie zrobic uczciwie bez Unity

- rozwiazac pakietow UPM lokalnie
- wygenerowac kompletnego projektu
- odpalic `XR Interaction Simulator`
- zweryfikowac importu URP i OpenXR

Ten punkt jest juz w praktyce zamkniety na tej maszynie.
