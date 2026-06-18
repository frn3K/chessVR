# Chess VR

Gra szachowa w wirtualnej rzeczywistości — projekt zaliczeniowy z przedmiotu *Systemy wirtualnej i rzeczywistej rzeczywistości*.

**Autorzy:** Konrad Markowicz (177127), Franciszek Międlar (177129)

## Opis

Gracz znajduje się w pomieszczeniu 3D z planszą szachową i wykonuje ruchy przez chwycenie figury kontrolerem VR. System waliduje posunięcia według zasad szachów — nielegalne ruchy są odrzucane, a figura wraca na poprzednie pole.

## Technologie

- Unity 6.3 LTS (`6000.3.12f1`)
- C#, Universal Render Pipeline
- OpenXR, XR Interaction Toolkit 3.3.1
- Unity Input System
- Platforma: Windows PCVR

## Uruchomienie

1. Zainstaluj [Unity Hub](https://unity.com/download) z edytorem **6000.3.12f1**.
2. Sklonuj repozytorium:
   ```bash
   git clone https://github.com/frn3K/chessVR.git
   ```
3. Otwórz folder projektu w Unity Hub.
4. Otwórz scenę `Assets/Scenes/Sandbox.unity`.
5. Naciśnij **Play** — gra działa w symulatorze XR (bez headsetu).
6. Do testu na headsetcie ustaw aktywny runtime OpenXR (Meta Link lub SteamVR).

## Testy

Testy logiki szachowej: `Assets/Tests/EditMode/BoardStateTests.cs`

Uruchomienie: **Window → General → Test Runner → EditMode → Run All**

## Struktura kodu

```
Assets/Scripts/
├── Domain/      — logika szachowa (BoardState)
├── Runtime/     — prezentacja, interakcja VR, UI
└── Contracts/   — interfejsy między warstwami
```

## Licencja

Kod projektu: [MIT](LICENSE). Modele figur: Chess MEGA-pack (Asset Store). Dźwięki: CC0 — szczegóły w `Assets/Audio/ATTRIBUTION.md`.
