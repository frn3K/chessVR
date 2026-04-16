# Product Background

## Elevator pitch

Gracz znajduje sie przy planszy szachowej w VR, moze podejsc do niej z dowolnej strony, obejsc ja, podniesc figure reka i wykonac ruch tylko wtedy, gdy jest on legalny wedlug zasad szachow. Pierwsza wersja ma byc czytelna, stabilna i grywalna bez zbednego rozrostu funkcji.

## Cel projektu

Zbudowac grywalne MVP szachow VR w Unity, ktore:

- dziala na Windows PCVR
- pozwala chodzic po scenie i wokol planszy
- obsluguje chwytanie figur i ich odkładanie na legalne pola
- posiada logike partii i przeciwnika komputerowego
- daje sie rozwijac nawet bez stalego dostepu do headsetu

## Filary produktu

- Tactile chess: figury musza dawac poczucie fizycznej obecnosci, ale bez chaosu pelnej fizyki.
- Readability first: stan gry, legalne ruchy i aktywna tura musza byc czytelne od pierwszego prototypu.
- Comfort over realism: komfort VR jest wazniejszy niz skrajny realizm interakcji.
- Architecture for iteration: logika szachow nie moze byc sklejona z VR.

## Domyslne zalozenia

- Target platform: Windows PCVR
- Tryb: solo vs AI
- Perspektywa: first-person VR
- Domyslna pozycja gracza: stojaca
- Interakcja: bezposredni chwyt kontrolerem
- Drop zachowania: snap na legalne pole, reset na nielegalnym ruchu
- Plansza: jedna arena, bez rozbudowanej metagry
- Styl graficzny: prosty i czytelny, placeholdery na starcie

## Poza zakresem pierwszego MVP

- multiplayer online
- hand tracking
- pelna fizyka figur i przewracanie elementow
- kampania, ranking, matchmaking
- Quest standalone jako target dnia pierwszego
- rozbudowane skiny i sklep

## Definicja sukcesu MVP

- da sie rozegrac cala partie przeciw AI
- legalne ruchy sa walidowane poprawnie
- gracz moze wejsc do sceny bez headsetu i testowac flow przez desktop simulator
- build nie zawiera krytycznych bugow blokujacych gre
- scena, ruch i interakcja sa na tyle stabilne, zeby rozpoczac prawdziwe strojenie UX VR
