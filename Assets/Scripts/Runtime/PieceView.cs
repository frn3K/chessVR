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
        private Quaternion _restLocalRotation;
        private float _restLocalY;

        public BoardSquare Square => _square;
        public PieceType PieceType => pieceType;
        public PieceColor PieceColor => pieceColor;

        /// <summary>
        /// Canonical local-space Y at which this piece sits on the board.
        /// Captured once in Apply() so MovePieceVisual always snaps back to the
        /// correct board height regardless of where the player released it in VR.
        /// </summary>
        public float RestLocalY => _restLocalY;

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

            // Save the Y the artist / builder placed the piece at so MovePieceVisual
            // can always snap back to this exact height after a move (VR or mouse).
            _restLocalY = transform.localPosition.y;

            // Najpierw collider — XRGrabInteractable będzie miał do czego się podpiąć
            FitBoxColliderToMesh();
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
            // Recompute the canonical position from the piece's own square and rest height —
            // never trust a localPosition captured at grab time, because XRI's dynamic-attach
            // machinery can introduce a Z offset that is sign-flipped for black pieces
            // (rotated 180° in Y), producing the characteristic forward/backward drift.
            if (_board != null)
            {
                transform.localPosition = _board.GetPieceRestLocalPosition(_square, _restLocalY);
            }

            transform.localRotation = _restLocalRotation;
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

        /// <summary>
        /// Usuwa wszelkie collidery z hierarchii figury i zastępuje je jednym BoxColliderem
        /// na tym obiekcie (rootu), dopasowanym dokładnie do widocznych meshy.
        /// Bez tego prefaby z Chess MEGA-pack mają CapsuleCollidery 10× za duże.
        /// </summary>
        private void FitBoxColliderToMesh()
        {
            // --- Usuń "złe" collidery z całej hierarchii ---

            var capsules = GetComponentsInChildren<CapsuleCollider>(true);
            for (var i = 0; i < capsules.Length; i++)
            {
                if (capsules[i] != null)
                {
                    Destroy(capsules[i]);
                }
            }

            var spheres = GetComponentsInChildren<SphereCollider>(true);
            for (var i = 0; i < spheres.Length; i++)
            {
                if (spheres[i] != null)
                {
                    Destroy(spheres[i]);
                }
            }

            var meshCols = GetComponentsInChildren<MeshCollider>(true);
            for (var i = 0; i < meshCols.Length; i++)
            {
                if (meshCols[i] != null)
                {
                    Destroy(meshCols[i]);
                }
            }

            // BoxCollidery na dzieciach też precz — chcemy jeden, centralny, na rootu
            var childBoxes = GetComponentsInChildren<BoxCollider>(true);
            for (var i = 0; i < childBoxes.Length; i++)
            {
                var b = childBoxes[i];
                if (b != null && b.gameObject != gameObject)
                {
                    Destroy(b);
                }
            }

            // --- Oblicz granice na podstawie wszystkich Rendererów ---

            var renderers = GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var worldBounds = new Bounds();

            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            // --- Dopasuj (lub utwórz) BoxCollider na tym obiekcie ---

            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            // Środek w przestrzeni lokalnej tego obiektu
            box.center = transform.InverseTransformPoint(worldBounds.center);

            // Rozmiar: konwersja z przestrzeni świata do lokalnej przez lossyScale
            var lossy = transform.lossyScale;
            var sx = Mathf.Abs(lossy.x) < 1e-5f ? 1f : Mathf.Abs(lossy.x);
            var sy = Mathf.Abs(lossy.y) < 1e-5f ? 1f : Mathf.Abs(lossy.y);
            var sz = Mathf.Abs(lossy.z) < 1e-5f ? 1f : Mathf.Abs(lossy.z);
            box.size = new Vector3(
                worldBounds.size.x / sx,
                worldBounds.size.y / sy,
                worldBounds.size.z / sz);
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
            body.constraints = RigidbodyConstraints.FreezeRotation;

            // Instantaneous: transform ustawiany bezpośrednio, omija silnik fizyczny.
            // Dzięki temu pionek NIE kolizuje z planszą podczas przeciągania i nie "odlatuje".
            _xrGrabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;

            _xrGrabInteractable.throwOnDetach = false;
            _xrGrabInteractable.forceGravityOnDetach = false;

            // Pionek pozostaje pionowy podczas przeciągania (nie obraca się z ręką)
            _xrGrabInteractable.trackRotation = false;

            // useDynamicAttach = true: w chwili chwytu XRI zapisuje AKTUALNY offset między
            // kontrolerem a figuro i przez cały czas przeciągania go zachowuje — pionek
            // NIE przeskakuje do dłoni.
            // matchAttachPosition/Rotation = false: nie przesuwamy figury do attach-pointa
            // kontrolera; dynamiczny offset startuje od aktualnej pozycji obiektu.
            _xrGrabInteractable.useDynamicAttach = true;
            _xrGrabInteractable.matchAttachPosition = false;
            _xrGrabInteractable.matchAttachRotation = false;
            _xrGrabInteractable.snapToColliderVolume = false;

            // Pionek nigdy nie staje się dzieckiem kontrolera VR — zostaje pod PiecesRoot
            _xrGrabInteractable.retainTransformParent = true;

            // Utwórz (lub odśwież) punkt chwytu wycentrowany na BoxColliderze figury.
            // Dzięki temu chwyt jest zawsze stabilny i pionek nie "ucieka" przy złapaniu.
            const string attachPointName = "XRAttachPoint";
            var attachGo = transform.Find(attachPointName)?.gameObject;
            if (attachGo == null)
            {
                attachGo = new GameObject(attachPointName);
                attachGo.transform.SetParent(transform, false);
            }

            var box = GetComponent<BoxCollider>();
            attachGo.transform.localPosition = box != null ? box.center : Vector3.zero;
            _xrGrabInteractable.attachTransform = attachGo.transform;

            _xrGrabInteractable.selectEntered.RemoveListener(OnXrSelectEntered);
            _xrGrabInteractable.selectExited.RemoveListener(OnXrSelectExited);
            _xrGrabInteractable.selectEntered.AddListener(OnXrSelectEntered);
            _xrGrabInteractable.selectExited.AddListener(OnXrSelectExited);
        }

        /// <summary>
        /// Enables or disables the XRGrabInteractable component so that VR hands can
        /// only physically grab pieces that are legal to move. Has no effect on the
        /// mouse/click path.
        /// </summary>
        public void SetInteractable(bool canInteract)
        {
            if (_xrGrabInteractable != null)
            {
                _xrGrabInteractable.enabled = canInteract;
            }
        }

        private void OnXrSelectEntered(SelectEnterEventArgs args)
        {
            _board?.HandlePieceGrabStarted(this);
        }

        private void OnXrSelectExited(SelectExitEventArgs args)
        {
            // Capture world position as the very first action — before the XR system
            // can move, reparent, or reset the piece's transform during release processing.
            var dropWorldPosition = transform.position;
            _board?.HandlePieceGrabEnded(this, dropWorldPosition);
        }
    }
}
