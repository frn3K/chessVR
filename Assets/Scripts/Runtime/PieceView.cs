using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ChessVR.Runtime
{
    public sealed class PieceView : MonoBehaviour, IPointerClickHandler
    {
        private const float PointerDebugRayLength = 80f;

        [SerializeField] private string squareNotation;
        [SerializeField] private PieceType pieceType;
        [SerializeField] private PieceColor pieceColor;

        private BoardSquare _square;
        private ChessGameController _game;
        private BoardPresenter _board;

        public void Apply(Piece piece, BoardSquare square, ChessGameController game, BoardPresenter board)
        {
            _square = square;
            _game = game;
            _board = board;
            squareNotation = square.ToString();
            pieceType = piece.Type;
            pieceColor = piece.Color;
            gameObject.name = $"{pieceColor} {pieceType} [{squareNotation}]";
        }

        private void Update()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector2 screen = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : (Vector2)UnityEngine.Input.mousePosition;

            var ray = cam.ScreenPointToRay(screen);
            Debug.DrawRay(ray.origin, ray.direction * PointerDebugRayLength, Color.magenta);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.LogError("TRAFIONY: Kliknięto w figurę " + gameObject.name);
            HandlePieceInteraction();
        }

        private void OnMouseDown()
        {
            Debug.Log("Kliknięcie przez OnMouseDown");
            HandlePieceInteraction();
        }

        private void HandlePieceInteraction()
        {
            if (_game == null || _board == null)
            {
                return;
            }

            var boardState = _game.CurrentBoard;
            var occupant = boardState.GetPiece(_square);
            if (occupant.IsNone || occupant.Color != boardState.SideToMove)
            {
                return;
            }

            if (_board.IsSquareCurrentlySelected(_square))
            {
                _board.ClearLegalMoveHighlights();
                return;
            }

            var legal = _game.GetLegalMovesFrom(_square);
            _board.ShowLegalMoveHighlights(_square, legal);
        }
    }
}
