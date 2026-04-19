## Implementation Notes (sesja robocza)

- **Automatyczne mapowanie 32 figur do pól na starcie**: `BoardPresenter` skanuje collidery pod `PiecesRoot`, dopina `PieceView` i dopasowuje obiekt do najbliższego pola (z tolerancją) na podstawie pozycji XZ; loguje sukces/porażkę mapowania.
- **Naprawa EventSystem**: `BoardPresenter` upewnia się, że na scenie istnieje `EventSystem` i `InputSystemUIInputModule`, a gdy akcje UI są puste — programowo przypisuje domyślne akcje (Point/Click itd.) i loguje potwierdzenie.
- **Auto-naprawa colliderów dla skalowanych modeli**: `FixPieceCollidersUnderRoot()` usuwa `CapsuleCollider`, dodaje `BoxCollider` na korzeniu figury i dopasowuje jego `center/size` do połączonych `Renderer.bounds` (przeliczając rozmiar do lokalnej skali).
- **Interakcja z figurami + debug promieni**: `PieceView` obsługuje kliknięcie (UI oraz fallback) i wypisuje czerwony log „TRAFIONY…”. Dodatkowo rysuje debugowy promień z kamery przez mysz (`Debug.DrawRay`) w Scene View.
- **Regulowana wysokość podświetleń ruchów**: `highlightHeightOffset` w `BoardPresenter` pozwala ustawić wysokość kropek nad planszą w Inspektorze.
- **Poprawki operacji niszczenia obiektów**: czyszczenie dzieci w `BoardPresenter` obsługuje tryb Play vs Edit (użycie `Destroy` / `DestroyImmediate` zależnie od kontekstu).

