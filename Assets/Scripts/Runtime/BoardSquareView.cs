using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChessVR.Runtime
{
    public sealed class BoardSquareView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string squareNotation;

        private XRSimpleInteractable _xrInteractable;
        private BoardSquare _square;
        private BoardPresenter _board;

        public void Apply(BoardSquare square, BoardPresenter board)
        {
            _square = square;
            _board = board;
            squareNotation = square.ToString();
            EnsureXrInteractable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _board?.TryHandleSquareInteraction(_square, clearOnInvalidDestination: true);
        }

        private void OnMouseDown()
        {
            _board?.TryHandleSquareInteraction(_square, clearOnInvalidDestination: true);
        }

        private void OnDestroy()
        {
            if (_xrInteractable != null)
            {
                _xrInteractable.selectEntered.RemoveListener(OnXrSelectEntered);
            }
        }

        private void EnsureXrInteractable()
        {
            _xrInteractable = gameObject.GetComponent<XRSimpleInteractable>();
            if (_xrInteractable == null)
            {
                _xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
            }

            _xrInteractable.selectEntered.RemoveListener(OnXrSelectEntered);
            _xrInteractable.selectEntered.AddListener(OnXrSelectEntered);
        }

        private void OnXrSelectEntered(SelectEnterEventArgs args)
        {
            _board?.TryHandleSquareInteraction(_square, clearOnInvalidDestination: true);
        }
    }
}
