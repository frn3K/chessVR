using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChessVR.Runtime
{
    public sealed class PieceView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string squareNotation;
        [SerializeField] private PieceType pieceType;
        [SerializeField] private PieceColor pieceColor;

        private XRGrabInteractable _xrGrabInteractable;
        private BoardSquare _square;
        private ChessGameController _game;
        private BoardPresenter _board;
        private Vector3 _grabStartLocalPosition;
        private Quaternion _grabStartLocalRotation;
        private Quaternion _restLocalRotation;

        public BoardSquare Square => _square;
        public PieceType PieceType => pieceType;
        public PieceColor PieceColor => pieceColor;

        public void Apply(Piece piece, BoardSquare square, ChessGameController game, BoardPresenter board)
        {
            _square = square;
            _game = game;
            _board = board;
            squareNotation = square.ToString();
            pieceType = piece.Type;
            pieceColor = piece.Color;
            gameObject.name = $"{pieceColor} {pieceType} [{squareNotation}]";
            _restLocalRotation = transform.localRotation;
            EnsureXrGrabInteractable();
        }

        public void UpdateSquare(BoardSquare square)
        {
            _square = square;
            squareNotation = square.ToString();
            gameObject.name = $"{pieceColor} {pieceType} [{squareNotation}]";
        }

        public void SnapBackToGrabStart()
        {
            transform.localPosition = _grabStartLocalPosition;
            transform.localRotation = _grabStartLocalRotation;
        }

        public void RestoreRotation()
        {
            transform.localRotation = _restLocalRotation;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandlePieceInteraction();
        }

        private void OnMouseDown()
        {
            HandlePieceInteraction();
        }

        private void OnDestroy()
        {
            if (_xrGrabInteractable != null)
            {
                _xrGrabInteractable.selectEntered.RemoveListener(OnXrSelectEntered);
                _xrGrabInteractable.selectExited.RemoveListener(OnXrSelectExited);
            }
        }

        private void HandlePieceInteraction()
        {
            if (_game == null || _board == null)
            {
                return;
            }

            if (_board.TryHandleSquareInteraction(_square, clearOnInvalidDestination: false))
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

        private void EnsureXrGrabInteractable()
        {
            _xrGrabInteractable = gameObject.GetComponent<XRGrabInteractable>();
            if (_xrGrabInteractable == null)
            {
                _xrGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }

            var body = gameObject.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
            _xrGrabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;

            _xrGrabInteractable.selectEntered.RemoveListener(OnXrSelectEntered);
            _xrGrabInteractable.selectExited.RemoveListener(OnXrSelectExited);
            _xrGrabInteractable.selectEntered.AddListener(OnXrSelectEntered);
            _xrGrabInteractable.selectExited.AddListener(OnXrSelectExited);
        }

        private void OnXrSelectEntered(SelectEnterEventArgs args)
        {
            _grabStartLocalPosition = transform.localPosition;
            _grabStartLocalRotation = transform.localRotation;
            _board?.HandlePieceGrabStarted(this);
        }

        private void OnXrSelectExited(SelectExitEventArgs args)
        {
            _board?.HandlePieceGrabEnded(this);
        }
    }
}
