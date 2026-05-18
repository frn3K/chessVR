# chessVR - instrukcja testu na komputerze z headsetem

Ten folder jest przygotowany na test VR na innym komputerze.

Najwazniejsze dane projektu:

- Unity: `6000.3.12f1`
- Scena do odpalenia: `Assets/Scenes/Sandbox.unity`
- Build scene: `Assets/Scenes/Sandbox.unity`
- XR: OpenXR
- XR Interaction Toolkit: `3.3.1`
- OpenXR package: `1.16.1`
- Platforma do najlatwiejszego testu: Windows PCVR

## Co zabrac na drugi komputer

Najprosciej zabrac plik:

`VR_Headset_Handoff/chessVR-headset-test.zip`

Ten zip powinien zawierac projekt bez folderow generowanych przez Unity, czyli bez:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `Build/`
- `Builds/`
- `.git/`

Jesli zipa jeszcze nie ma albo chcesz zrobic nowego, uruchom na swoim komputerze:

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force
.\VR_Headset_Handoff\Make-VR-Test-Zip.ps1
```

## Przygotowanie komputera z headsetem

1. Zainstaluj Unity Hub.
2. Zainstaluj Unity Editor `6000.3.12f1`.
3. Do testu PCVR wystarczy Windows build support / domyslna instalacja edytora.
4. Rozpakuj `chessVR-headset-test.zip` do zwyklego folderu, np. `C:\Projects\chessVR`.
5. Otworz projekt przez Unity Hub.
6. Poczekaj az Unity odbuduje `Library/`. Pierwsze otwarcie moze potrwac kilka minut.

Nie kopiuj recznie starego folderu `Library/` z innego komputera.

## Ustawienie OpenXR runtime

Przed wlaczeniem Play Mode upewnij sie, ze aktywny jest runtime od uzywanego headsetu.

### Meta Quest przez Link / Air Link

1. Zainstaluj Meta Quest Link app na PC.
2. Podlacz headset przez Link albo Air Link.
3. W Meta Quest Link app ustaw Meta Quest jako aktywny OpenXR runtime.
4. Dopiero potem odpal Unity.

### Valve Index / HTC Vive / SteamVR

1. Zainstaluj i uruchom SteamVR.
2. W ustawieniach SteamVR ustaw SteamVR jako aktywny OpenXR runtime.
3. Dopiero potem odpal Unity.

## Walidacja w Unity

W Unity przejdz do:

`Edit -> Project Settings -> XR Plug-in Management -> OpenXR`

Nastepnie:

1. Otworz `Project Validation`.
2. Kliknij `Fix All`, jesli sa ostrzezenia ktore Unity potrafi naprawic.
3. Sprawdz, czy OpenXR jest wlaczony dla platformy PC.

Jesli Unity pyta o restart po zmianach XR/Input System, zrestartuj edytor.

## Jak odpalic Sandbox w edytorze

1. W panelu Project znajdz:

   `Assets/Scenes/Sandbox.unity`

2. Otworz scene dwuklikiem.
3. Upewnij sie, ze headset jest podlaczony i aktywny.
4. Kliknij `Play`.
5. Zaloz headset i sprawdz:

   - czy widac plansze,
   - czy kontrolery/ray/hands sa widoczne,
   - czy da sie zlapac figure,
   - czy legalny ruch snapuje na pole,
   - czy nielegalny ruch wraca,
   - czy UI dziala z kontrolera.

Pelna lista testow jest w:

`VR_Headset_Handoff/VR_TEST_CHECKLIST.md`

## Jak zrobic Windows build

Najpierw sprawdz gre w edytorze. Build rob dopiero gdy Play Mode dziala.

1. Otworz:

   `File -> Build Profiles`

   W starszym widoku Unity moze to byc:

   `File -> Build Settings`

2. Wybierz platforme:

   `Windows`

3. Architektura:

   `x86_64`

4. Upewnij sie, ze w scenach jest zaznaczone:

   `Assets/Scenes/Sandbox.unity`

5. Kliknij `Build` albo `Build And Run`.
6. Wybierz folder wyjsciowy poza repozytorium, np.:

   `Desktop/chessVR_Build`

7. Po uruchomieniu builda zaloz headset i powtorz podstawowy test VR.

## Jesli cos nie dziala

Najczestsze problemy:

- Headset nie pokazuje gry: zly aktywny OpenXR runtime.
- Kontrolery nie dzialaja: restart Unity po wlaczeniu XR/Input System.
- Gra dziala w oknie, ale nie w headsetcie: headset nie jest aktywny przez Link/SteamVR.
- Chwytanie nie dziala: sprawdz, czy kontrolery maja interakcje/ray/direct interactor i czy figura reaguje na hover/select.
- Drop na pole jest nieprecyzyjny: do tuningu bedzie promien snapowania w `BoardPresenter`.
- UI nie klika sie z VR: sprawdz ray interactor i world-space canvas.

## Co zapisac po tescie

Po tescie zapisz:

- model headsetu,
- czy test byl przez Meta Link, Air Link, SteamVR czy cos innego,
- czy dzialalo w edytorze,
- czy dzialalo w buildzie,
- czy da sie chwycic figure,
- czy da sie wykonac legalny ruch,
- czy nielegalny ruch wraca,
- czy UI dziala,
- czy byly spadki FPS / lagi / choroba lokomocyjna,
- screenshot albo krotki opis kazdego bledu.

