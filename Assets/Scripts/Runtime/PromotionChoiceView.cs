using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ChessVR.Runtime
{
    public sealed class PromotionChoiceView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PieceType promotionPiece;

        private BoardPresenter _board;
        private XRSimpleInteractable _xrInteractable;

        public void Apply(PieceType pieceType, BoardPresenter board)
        {
            promotionPiece = pieceType;
            _board = board;
            EnsureXrInteractable();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _board?.TryChoosePromotion(promotionPiece);
        }

        private void OnMouseDown()
        {
            _board?.TryChoosePromotion(promotionPiece);
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
            _board?.TryChoosePromotion(promotionPiece);
        }
    }
}
