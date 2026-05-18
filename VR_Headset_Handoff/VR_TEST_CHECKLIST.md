# chessVR - checklista testu VR

Uzyj tej checklisty w czwartek na komputerze z headsetem.

Wynik oznacz jako:

- `[OK]` dziala
- `[BUG]` blad
- `[N/A]` nie testowano

## 1. Start projektu

- `[ ]` Projekt otwiera sie w Unity `6000.3.12f1`.
- `[ ]` Unity odbudowalo `Library/` bez krytycznych bledow.
- `[ ]` Scena `Assets/Scenes/Sandbox.unity` otwiera sie poprawnie.
- `[ ]` `Project Validation` dla OpenXR nie pokazuje krytycznych bledow.
- `[ ]` Aktywny OpenXR runtime pasuje do headsetu.

## 2. Play Mode w edytorze

- `[ ]` Po kliknieciu `Play` gra startuje bez crasha.
- `[ ]` Obraz jest widoczny w headsetcie.
- `[ ]` Kamera/headset ma dobra pozycje wzgledem planszy.
- `[ ]` Plansza jest na wygodnej wysokosci.
- `[ ]` UI jest czytelne w headsetcie.
- `[ ]` Muzyka gra.
- `[ ]` Dzwieki ruchow sa slyszalne.

## 3. Kontrolery i interakcja

- `[ ]` Kontrolery albo rece sa wykrywane.
- `[ ]` Ray/direct interaction dziala.
- `[ ]` Da sie wskazac figure.
- `[ ]` Da sie zlapac figure.
- `[ ]` Figura trzyma sie kontrolera podczas przenoszenia.
- `[ ]` Figura nie wpada pod plansze ani nie odlatuje.
- `[ ]` Da sie puscic figure nad plansza.

## 4. Ruchy figur

- `[ ]` Legalny ruch pionkiem dziala.
- `[ ]` Legalny ruch skoczkiem dziala.
- `[ ]` Legalny ruch goncem dziala.
- `[ ]` Legalny ruch wieza dziala.
- `[ ]` Legalny ruch hetmanem dziala.
- `[ ]` Legalny ruch krolem dziala.
- `[ ]` Po legalnym dropie figura snapuje do srodka pola.
- `[ ]` Po nielegalnym dropie figura wraca na poprzednie pole.
- `[ ]` Nie da sie ruszyc figura przeciwnika w zlej turze.
- `[ ]` Podswietlenie legalnych pol jest widoczne.

## 5. Specjalne zasady

- `[ ]` Bicie figury dziala.
- `[ ]` Szach jest wykrywany/komunikowany.
- `[ ]` Mat jest wykrywany/komunikowany.
- `[ ]` Pat jest wykrywany/komunikowany.
- `[ ]` Roszada dziala, jesli jest dostepna.
- `[ ]` En passant dziala, jesli jest dostepne.
- `[ ]` Promocja pionka pokazuje wybor figury.
- `[ ]` Promocja da sie obsluzyc w VR.

## 6. UI w VR

- `[ ]` Da sie kliknac przycisk restartu.
- `[ ]` Da sie obslugiwac ustawienia audio, jesli sa widoczne.
- `[ ]` UI nie zaslania planszy w niewygodny sposob.
- `[ ]` Tekst tury/gracza jest czytelny.
- `[ ]` Komunikaty typu invalid move sa zrozumiale.

## 7. Komfort

- `[ ]` Skala figur jest wygodna.
- `[ ]` Odleglosc od planszy jest wygodna.
- `[ ]` Nie trzeba nienaturalnie sie schylac.
- `[ ]` Nie trzeba zbyt wysoko podnosic rak.
- `[ ]` Gra utrzymuje plynne FPS.
- `[ ]` Nie ma migotania UI ani planszy.
- `[ ]` Audio nie jest zbyt glosne.

## 8. Windows build

- `[ ]` Build Windows x86_64 konczy sie sukcesem.
- `[ ]` Build uruchamia sie poza Unity.
- `[ ]` Build pokazuje obraz w headsetcie.
- `[ ]` Chwytanie dziala w buildzie.
- `[ ]` Ruch legalny i nielegalny dziala w buildzie.
- `[ ]` UI dziala w buildzie.
- `[ ]` Audio dziala w buildzie.

## 9. Notatki z testu

Headset:

Runtime OpenXR:

Testowane w edytorze: tak / nie

Testowane w buildzie: tak / nie

Najwiekszy problem:

```

```

Rzeczy do poprawy:

```

```

