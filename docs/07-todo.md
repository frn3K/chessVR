# TODO

## Repo i srodowisko

- [x] zainicjalizowac repo git
- [x] przygotowac dokumentacje preproduction
- [x] przygotowac audit srodowiska
- [x] zainstalowac Unity Hub
- [x] zainstalowac Unity 6.3 LTS
- [x] zainstalowac Visual Studio 2022 z supportem dla Unity
- [x] aktywowac licencje Unity
- [ ] potwierdzic aktywny OpenXR runtime na maszynie

## Projekt Unity

- [x] zbootstrapowac projekt Unity w tym repo
- [x] dodac `Input System`
- [x] dodac `OpenXR Plugin`
- [x] dodac `XR Interaction Toolkit`
- [x] dodac `URP`
- [x] zaimportowac `Starter Assets`
- [x] zaimportowac `XR Interaction Simulator`
- [ ] naprawic wszystkie ostrzezenia z `Project Validation`
- [x] stworzyc scene `Sandbox`

## Architektura i kod

- [ ] zalozyc katalogi `Application`, `Domain`, `AI`, `Board`, `Interaction`, `UI`
- [ ] zdefiniowac interfejsy graniczne
- [x] przygotowac pierwszy model planszy
- [x] dodac testy legal move validation
- [ ] zdecydowac czy bierzemy biblioteke C# do zasad szachow, czy implementacje wlasna

## Gameplay

- [x] stworzyc scene `Sandbox`
- [ ] stworzyc scene `MatchArena`
- [x] zrobic podstawowa plansze i pierwszy `BoardPresenter`
- [x] przygotowac placeholderowe figury
- [ ] podlaczyc highlight legalnych pol
- [ ] wdrozyc chwyt figury
- [ ] wdrozyc snap na legalne pole
- [ ] wdrozyc reset przy nielegalnym ruchu
- [ ] dodac prosty AI provider
- [ ] obsluzyc wynik partii i restart

## QA

- [ ] test bez headsetu przez simulator
- [ ] test przeplywu calej partii
- [ ] testy edge case'ow roszady, en passant i promocji
- [ ] pierwszy test na realnym headsetcie, gdy sprzet bedzie dostepny
