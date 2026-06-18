using System;
using System.Collections.Generic;
using System.Reflection;
using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;


namespace ChessVR.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoardPresenter : MonoBehaviour
    {
        [Serializable]
        private struct PiecePrefabSet
        {
            public PieceType pieceType;
            public GameObject whitePrefab;
            public GameObject blackPrefab;
        }

        [Header("Dependencies")]
        [SerializeField] private ChessGameController gameController;

        [Header("Layout")]
        [SerializeField] private float squareSize = 0.32f;
        [SerializeField] private float boardThickness = 0.04f;
        [SerializeField] private float boardHeight = 0.02f;
        [SerializeField] private float boardPadding = 0.08f;

        [Header("Visuals")]
        [SerializeField] private Color lightSquareColor = new(0.91f, 0.88f, 0.81f, 1f);
        [SerializeField] private Color darkSquareColor = new(0.29f, 0.22f, 0.16f, 1f);
        [SerializeField] private Color borderColor = new(0.11f, 0.08f, 0.06f, 1f);
        [SerializeField] private Color whitePieceColor = new(0.92f, 0.92f, 0.9f, 1f);
        [SerializeField] private Color blackPieceColor = new(0.16f, 0.16f, 0.18f, 1f);

        [Header("Piece prefabs (optional)")]
        [SerializeField] private PiecePrefabSet[] piecePrefabs;
        [SerializeField] private float piecePrefabScale = 1f;
        [SerializeField] private float promotionChoiceScale = 0.8f;

        [Header("Runtime")]
        [SerializeField] private bool rebuildOnStart = true;

        [Header("Legal move highlights")]
        [SerializeField] private Color legalMoveHighlightColor = new(0.15f, 0.75f, 0.35f, 1f);
        [SerializeField] private float highlightHeightOffset = 0.05f;

        [Header("VR grab & drop")]
        [Tooltip("Maksymalna odleglosc od srodka pola (lokalne XZ), zeby uznac drop za trafiony.")]
        [SerializeField] private float dropSquareSnapRadius = 0.28f;

        [Header("XR player height")]
        [Tooltip("Trzyma root XR Origin na wysokosci planszy, zeby gracz nie startowal ani nie zostawal ponad szachownica.")]
        [SerializeField] private bool keepXrOriginAtBoardHeight = true;

        [Tooltip("Ustawia POV gracza troche nad najwyzsza figura zamiast na wysokosci podlogi albo pelnego wzrostu.")]
        [SerializeField] private bool keepXrCameraAbovePieces = true;

        [Tooltip("Dodatkowy zapas nad najwyzsza figura dla pozycji oczu gracza.")]
        [SerializeField] private float xrEyeClearanceAboveTallestPiece = 0.12f;

        [Tooltip("Fallback wysokosci oczu nad plansza, gdy renderery figur nie sa jeszcze dostepne.")]
        [SerializeField] private float xrFallbackEyeHeightAboveBoard = 0.7f;

        [Tooltip("Promień dopasowania figury do pola (przestrzeń lokalna PiecesRoot), w jednostkach świata lokalnego — duży = ręczne ustawienia.")]
        [SerializeField] private float pieceSquareMatchRadius = 0.9f;

        [Header("Collider fix (for manually scaled pieces)")]
        [Tooltip("Jeśli figury są ręcznie skalowane (np. skala 25), BoxCollider będzie dopasowany do rendererów.")]
        [SerializeField] private bool fixPieceCollidersOnStart = true;

        [Header("Game Over UI")]
        [Tooltip("Opcjonalny GameUIManager do wyświetlania panelu końca gry (mat/pat/remis).")]
        [SerializeField] private GameUIManager gameUIManager;

        [Header("Audio")]
        [SerializeField] private GameAudioManager gameAudioManager;

        private Transform _boardRoot;
        private Transform _piecesRoot;
        private Transform _highlightsRoot;
        private Transform _promotionChoicesRoot;
        private BoardSquare? _selectedSquare;
        private readonly List<ChessMove> _selectedMoves = new();
        private readonly List<ChessMove> _pendingPromotionMoves = new();
        private Transform _xrOrigin;
        private Transform _xrCameraOffset;

        private void Awake()
        {
            EnsureEventSystemExists();
            EnsureMainCameraHasPhysicsRaycaster();
        }

        private void Start()
        {
            if (rebuildOnStart)
            {
                RebuildImmediate();
            }
            else
            {
                EnsureDependencies();
                EnsureRoots();
                EnsureSquareViewsForBoardUnderRoot();
                EnsurePieceViewsForPiecesUnderRoot();
                if (fixPieceCollidersOnStart)
                {
                    FixPieceCollidersUnderRoot();
                }

                UpdateStatusIndicator();
                UpdateAllPiecesInteractability();
            }

            ApplyXrPlayerHeight();
        }

        private void LateUpdate()
        {
            ApplyXrPlayerHeight();
        }

        [ContextMenu("Rebuild Board")]
        public void RebuildImmediate()
        {
            EnsureDependencies();
            EnsureRoots();

            gameUIManager?.HideGameOver();
            ClearLegalMoveHighlights();
            ClearChildren(_boardRoot);
            ClearChildren(_piecesRoot);

            BuildBoard();
            EnsureSquareViewsForBoardUnderRoot();
            BuildPieces();
            EnsurePieceViewsForPiecesUnderRoot();
            if (fixPieceCollidersOnStart)
            {
                FixPieceCollidersUnderRoot();
            }

            UpdateStatusIndicator();
            UpdateAllPiecesInteractability();
        }

        [ContextMenu("Fix Piece Colliders (Box fit to renderers)")]
        public void FixPieceCollidersUnderRoot()
        {
            EnsureDependencies();
            EnsureRoots();

            if (_piecesRoot == null)
            {
                return;
            }

            for (var i = 0; i < _piecesRoot.childCount; i++)
            {
                var pieceRoot = _piecesRoot.GetChild(i);
                if (pieceRoot == null)
                {
                    continue;
                }

                RemoveCapsuleColliders(pieceRoot);

                if (!TryGetCombinedWorldBounds(pieceRoot, out var worldBounds))
                {
                    continue;
                }

                var target = pieceRoot.gameObject;

                // Ensure BoxCollider exists on the same object that will host PieceView.
                var box = target.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = target.AddComponent<BoxCollider>();
                }

                box.center = pieceRoot.InverseTransformPoint(worldBounds.center);

                // Convert world-space bounds size to local size.
                var lossy = pieceRoot.lossyScale;
                var sx = Mathf.Abs(lossy.x) < 1e-5f ? 1f : Mathf.Abs(lossy.x);
                var sy = Mathf.Abs(lossy.y) < 1e-5f ? 1f : Mathf.Abs(lossy.y);
                var sz = Mathf.Abs(lossy.z) < 1e-5f ? 1f : Mathf.Abs(lossy.z);
                box.size = new Vector3(worldBounds.size.x / sx, worldBounds.size.y / sy, worldBounds.size.z / sz);

                // PieceView must be on the same object as the collider that receives clicks.
                // We standardize it to the piece root.
                var childViews = pieceRoot.GetComponentsInChildren<PieceView>(true);
                for (var v = 0; v < childViews.Length; v++)
                {
                    var view = childViews[v];
                    if (view != null && view.gameObject != target)
                    {
                        DestroyUnityObject(view);
                    }
                }

                if (target.GetComponent<PieceView>() == null)
                {
                    target.AddComponent<PieceView>();
                }
            }
        }

        private void EnsureRootCollider(GameObject root)
        {
            if (root == null || root.GetComponent<Collider>() != null)
            {
                return;
            }

            if (!TryGetCombinedWorldBounds(root.transform, out var worldBounds))
            {
                return;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(worldBounds.center);

            var lossy = root.transform.lossyScale;
            var sx = Mathf.Abs(lossy.x) < 1e-5f ? 1f : Mathf.Abs(lossy.x);
            var sy = Mathf.Abs(lossy.y) < 1e-5f ? 1f : Mathf.Abs(lossy.y);
            var sz = Mathf.Abs(lossy.z) < 1e-5f ? 1f : Mathf.Abs(lossy.z);
            box.size = new Vector3(worldBounds.size.x / sx, worldBounds.size.y / sy, worldBounds.size.z / sz);
        }

        private static void StripPieceInteractionComponents(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var pieceView = root.GetComponent<PieceView>();
            if (pieceView != null)
            {
                DestroyUnityObject(pieceView);
            }

            var grab = root.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null)
            {
                DestroyUnityObject(grab);
            }

            var body = root.GetComponent<Rigidbody>();
            if (body != null)
            {
                DestroyUnityObject(body);
            }
        }

        private static void RemoveCapsuleColliders(Transform root)
        {
            var capsules = root.GetComponentsInChildren<CapsuleCollider>(true);
            for (var i = 0; i < capsules.Length; i++)
            {
                if (capsules[i] != null)
                {
                    DestroyUnityObject(capsules[i]);
                }
            }
        }

        private static bool TryGetCombinedWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasAny = false;

            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!hasAny)
                {
                    bounds = r.bounds;
                    hasAny = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return hasAny;
        }

        public void EnsurePieceViewsForPiecesUnderRoot()
        {
            EnsureDependencies();
            EnsureRoots();

            var board = gameController.CurrentBoard;
            if (board == null || _piecesRoot == null)
            {
                return;
            }

            for (var i = 0; i < _piecesRoot.childCount; i++)
            {
                var pieceRoot = _piecesRoot.GetChild(i);
                if (pieceRoot == null)
                {
                    continue;
                }

                var matchPosition = _piecesRoot.InverseTransformPoint(pieceRoot.position);
                if (TryGetCombinedWorldBounds(pieceRoot, out var worldBounds))
                {
                    matchPosition = _piecesRoot.InverseTransformPoint(worldBounds.center);
                }

                if (!TryMatchPieceAtSquareFromLocalPosition(matchPosition, board, out var square, out var piece))
                {
                    Debug.LogWarning(
                        $"Nie dopasowano obiektu {pieceRoot.gameObject.name} (środek w lokalnym układzie PiecesRoot: X={matchPosition.x:F3}, Z={matchPosition.z:F3})");
                    continue;
                }

                var view = pieceRoot.GetComponent<PieceView>();
                if (view == null)
                {
                    view = pieceRoot.gameObject.AddComponent<PieceView>();
                }

                view.Apply(piece, square, gameController, this);
            }
        }

        private bool TryMatchPieceAtSquareFromLocalPosition(Vector3 localOnPiecesRoot, BoardState board, out BoardSquare square, out Piece piece)
        {
            square = default;
            piece = Piece.None;
            var thresholdSq = pieceSquareMatchRadius * pieceSquareMatchRadius;
            var best = float.MaxValue;

            for (var index = 0; index < 64; index++)
            {
                var candidateSquare = BoardSquare.FromIndex(index);
                var candidatePiece = board.GetPiece(candidateSquare);
                if (candidatePiece.IsNone)
                {
                    continue;
                }

                var expected = PieceLocalPosition(candidateSquare, candidatePiece.Type);
                var dx = expected.x - localOnPiecesRoot.x;
                var dz = expected.z - localOnPiecesRoot.z;
                var distSq = dx * dx + dz * dz;
                if (distSq < best)
                {
                    best = distSq;
                    square = candidateSquare;
                    piece = candidatePiece;
                }
            }

            return !piece.IsNone && best <= thresholdSq;
        }

        private static void EnsureEventSystemExists()
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                uiModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            ConfigureInputSystemUiModule(uiModule);
        }

        private static void ConfigureInputSystemUiModule(InputSystemUIInputModule module)
        {
            if (module == null)
            {
                return;
            }

            if (NeedsDefaultUiActions(module))
            {
                var wasEnabled = module.enabled;
                module.enabled = false;
                module.UnassignActions();
                module.AssignDefaultActions();
                module.enabled = wasEnabled;
            }

            TryEnableBuiltinActionsAsFallback(module);
        }

        private static bool NeedsDefaultUiActions(InputSystemUIInputModule module)
        {
            return module.point == null || module.point.action == null
                   || module.leftClick == null || module.leftClick.action == null;
        }

        private static void TryEnableBuiltinActionsAsFallback(InputSystemUIInputModule module)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = typeof(InputSystemUIInputModule);
            foreach (var memberName in new[] { "enableBuiltinActionsAsFallback", "m_EnableBuiltinActionsAsFallback" })
            {
                var prop = type.GetProperty(memberName, flags);
                if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
                {
                    prop.SetValue(module, true);
                    return;
                }

                var field = type.GetField(memberName, flags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(module, true);
                    return;
                }
            }
        }

        private static void EnsureMainCameraHasPhysicsRaycaster()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var raycaster = cam.GetComponent<PhysicsRaycaster>();
            if (raycaster == null)
            {
                raycaster = cam.gameObject.AddComponent<PhysicsRaycaster>();
            }

            raycaster.eventMask = (LayerMask)(-1);
        }

        public bool IsSquareCurrentlySelected(BoardSquare square)
        {
            return _selectedSquare.HasValue && _selectedSquare.Value.Equals(square);
        }

        public bool TryHandleSquareInteraction(BoardSquare square, bool clearOnInvalidDestination)
        {
            return TryHandleSquareInteraction(square, clearOnInvalidDestination, out _);
        }

        public bool TryHandleSquareInteraction(BoardSquare square, bool clearOnInvalidDestination, out bool moveApplied)
        {
            moveApplied = false;
            EnsureDependencies();

            if (_pendingPromotionMoves.Count > 0)
            {
                ClearLegalMoveHighlights();
                return true;
            }

            if (!_selectedSquare.HasValue)
            {
                return false;
            }

            if (_selectedSquare.Value.Equals(square))
            {
                ClearLegalMoveHighlights();
                return true;
            }

            if (TryGetSelectedMove(square, out var move))
            {
                moveApplied = TryExecuteSelectedMove(move);
                return moveApplied;
            }

            if (_pendingPromotionMoves.Count > 0)
            {
                return true;
            }

            if (clearOnInvalidDestination)
            {
                ClearLegalMoveHighlights();
                return true;
            }

            return false;
        }

        public bool TryChoosePromotion(PieceType promotionPiece)
        {
            if (_pendingPromotionMoves.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < _pendingPromotionMoves.Count; i++)
            {
                if (_pendingPromotionMoves[i].Promotion == promotionPiece)
                {
                    return TryExecuteSelectedMove(_pendingPromotionMoves[i]);
                }
            }

            return false;
        }

        public void HandlePieceGrabStarted(PieceView pieceView)
        {
            EnsureDependencies();

            if (pieceView == null || gameController.CurrentBoard == null)
            {
                return;
            }

            if (_pendingPromotionMoves.Count > 0)
            {
                return;
            }

            var currentBoard = gameController.CurrentBoard;
            if (currentBoard.IsCheckmate(currentBoard.SideToMove) || currentBoard.IsStalemate(currentBoard.SideToMove))
            {
                return;
            }

            // Use the stored square directly — same approach as the mouse click path.
            // Mapping from world position is unreliable at grab start (dynamic attach, hand offset).
            var square = pieceView.Square;
            var occupant = currentBoard.GetPiece(square);
            if (occupant.IsNone || occupant.Color != currentBoard.SideToMove)
            {
                return;
            }

            var legalMoves = gameController.GetLegalMovesFrom(square);
            ShowLegalMoveHighlights(square, legalMoves);
        }

        public void HandlePieceGrabEnded(PieceView pieceView, Vector3 dropWorldPosition)
        {
            EnsureDependencies();
            EnsureRoots();

            if (pieceView == null)
            {
                return;
            }

            // Reparent to PiecesRoot so SnapBackToGrabStart / localPosition assignments
            // work in the correct coordinate space. worldPositionStays keeps the world
            // position intact (we already captured it in dropWorldPosition above).
            if (_piecesRoot != null && pieceView.transform.parent != _piecesRoot)
            {
                pieceView.transform.SetParent(_piecesRoot, worldPositionStays: true);
            }

            if (_pendingPromotionMoves.Count > 0)
            {
                PlayInvalidFeedback();
                pieceView.SnapBackToGrabStart();
                return;
            }

            var board = gameController?.CurrentBoard;
            if (board != null && (board.IsCheckmate(board.SideToMove) || board.IsStalemate(board.SideToMove)))
            {
                PlayInvalidFeedback();
                pieceView.SnapBackToGrabStart();
                return;
            }

            if (!_selectedSquare.HasValue || !_selectedSquare.Value.Equals(pieceView.Square))
            {
                PlayInvalidFeedback();
                pieceView.SnapBackToGrabStart();
                return;
            }

            // Use the pre-captured world position to find the target square —
            // same flow as TryHandleSquareInteraction called by the mouse/click path.
            if (!TryGetSquareFromWorldPosition(dropWorldPosition, out var targetSquare))
            {
                PlayInvalidFeedback();
                pieceView.SnapBackToGrabStart();
                ClearLegalMoveHighlights();
                return;
            }

            var handled = TryHandleSquareInteraction(targetSquare, clearOnInvalidDestination: true, out var moveApplied);
            if (!handled || !moveApplied)
            {
                PlayInvalidFeedback();
                pieceView.SnapBackToGrabStart();
            }
        }

        public void ClearLegalMoveHighlights()
        {
            _selectedSquare = null;
            _selectedMoves.Clear();
            _pendingPromotionMoves.Clear();
            if (_highlightsRoot == null)
            {
                UpdateStatusIndicator();
                return;
            }

            ClearChildren(_highlightsRoot);
            if (_promotionChoicesRoot != null)
            {
                ClearChildren(_promotionChoicesRoot);
            }

            UpdateStatusIndicator();
        }

        public void ShowLegalMoveHighlights(BoardSquare from, IReadOnlyList<ChessMove> legalMoves)
        {
            EnsureDependencies();
            EnsureRoots();
            ClearLegalMoveHighlights();
            _selectedSquare = from;

            if (legalMoves == null || legalMoves.Count == 0)
            {
                UpdateStatusIndicator();
                return;
            }

            _selectedMoves.AddRange(legalMoves);
            ResolveAudioManager()?.PlayPickup();
            var seen = new HashSet<BoardSquare>();
            for (var i = 0; i < legalMoves.Count; i++)
            {
                var to = legalMoves[i].To;
                if (!seen.Add(to))
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = $"LegalHighlight_{to}";
                marker.transform.SetParent(_highlightsRoot, false);
                var xz = SquareToLocalPosition(to, boardHeight);
                marker.transform.localPosition = new Vector3(xz.x, boardHeight + highlightHeightOffset, xz.z);
                marker.transform.localScale = new Vector3(squareSize * 0.42f, 0.008f, squareSize * 0.42f);
                marker.transform.localRotation = Quaternion.identity;
                Tint(marker, legalMoveHighlightColor);
                DestroyCollider(marker);
            }

            UpdateStatusIndicator();
        }

        private void EnsureDependencies()
        {
            if (gameController == null)
            {
                gameController = GetComponent<ChessGameController>();
            }

            if (gameController == null)
            {
                gameController = gameObject.AddComponent<ChessGameController>();
            }

            if (gameController.CurrentBoard == null)
            {
                gameController.ResetMatch();
            }

            ResolveAudioManager();
        }

        private void EnsureRoots()
        {
            _boardRoot = FindOrCreateChild("BoardRoot");
            _piecesRoot = FindOrCreateChild("PiecesRoot");
            _highlightsRoot = FindOrCreateChild("LegalHighlightsRoot");
            _promotionChoicesRoot = FindOrCreateChild("PromotionChoicesRoot");
            RemoveLegacyStatusIndicator();
        }

        private Transform FindOrCreateChild(string childName)
        {
            var existing = transform.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            return child.transform;
        }

        private void ApplyXrPlayerHeight()
        {
            if (!keepXrOriginAtBoardHeight || !TryResolveXrOrigin())
            {
                return;
            }

            var targetWorldY = transform.TransformPoint(new Vector3(0f, boardHeight, 0f)).y;
            var xrPosition = _xrOrigin.position;
            if (!Mathf.Approximately(xrPosition.y, targetWorldY))
            {
                _xrOrigin.position = new Vector3(xrPosition.x, targetWorldY, xrPosition.z);
            }

            ApplyXrCameraHeight(targetWorldY);
        }

        private bool TryResolveXrOrigin()
        {
            if (_xrOrigin == null)
            {
                var xrOriginObject = GameObject.Find("XROrigin")
                                     ?? GameObject.Find("XR Origin")
                                     ?? GameObject.Find("XR Origin (XR Rig)");
                if (xrOriginObject != null)
                {
                    _xrOrigin = xrOriginObject.transform;
                }
            }

            if (_xrOrigin == null)
            {
                return false;
            }

            if (_xrCameraOffset == null)
            {
                _xrCameraOffset = _xrOrigin.Find("Camera Offset");
            }

            return true;
        }

        private void ApplyXrCameraHeight(float xrOriginWorldY)
        {
            if (!keepXrCameraAbovePieces || _xrCameraOffset == null)
            {
                return;
            }

            var targetEyeWorldY = TryGetTallestPieceWorldY(out var tallestPieceWorldY)
                ? tallestPieceWorldY + Mathf.Max(0f, xrEyeClearanceAboveTallestPiece)
                : xrOriginWorldY + Mathf.Max(0f, xrFallbackEyeHeightAboveBoard);

            var localEyeY = Mathf.Max(0f, targetEyeWorldY - xrOriginWorldY);
            var offsetPosition = _xrCameraOffset.localPosition;
            if (!Mathf.Approximately(offsetPosition.y, localEyeY))
            {
                _xrCameraOffset.localPosition = new Vector3(offsetPosition.x, localEyeY, offsetPosition.z);
            }
        }

        private bool TryGetTallestPieceWorldY(out float tallestPieceWorldY)
        {
            tallestPieceWorldY = 0f;
            if (_piecesRoot == null)
            {
                return false;
            }

            var renderers = _piecesRoot.GetComponentsInChildren<Renderer>(true);
            var hasRenderer = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var top = renderer.bounds.max.y;
                if (!hasRenderer || top > tallestPieceWorldY)
                {
                    tallestPieceWorldY = top;
                    hasRenderer = true;
                }
            }

            return hasRenderer;
        }

        private void RemoveLegacyStatusIndicator()
        {
            var statusRoot = transform.Find("StatusTextRoot");
            if (statusRoot != null)
            {
                DestroyUnityObject(statusRoot.gameObject);
            }
        }

        private void EnsureSquareViewsForBoardUnderRoot()
        {
            if (_boardRoot == null)
            {
                return;
            }

            for (var i = 0; i < _boardRoot.childCount; i++)
            {
                var boardChild = _boardRoot.GetChild(i);
                if (!TryParseSquareName(boardChild.name, out var square))
                {
                    continue;
                }

                AttachOrRefreshSquareView(boardChild.gameObject, square);
            }
        }

        private void BuildBoard()
        {
            var boardWorldSize = squareSize * 8f;

            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "BoardBorder";
            border.transform.SetParent(_boardRoot, false);
            border.transform.localPosition = new Vector3(0f, boardHeight - (boardThickness * 0.5f), 0f);
            border.transform.localScale = new Vector3(
                boardWorldSize + boardPadding,
                boardThickness,
                boardWorldSize + boardPadding);
            Tint(border, borderColor);

            for (var rank = 0; rank < 8; rank++)
            {
                for (var file = 0; file < 8; file++)
                {
                    var boardSquare = new BoardSquare(file, rank);
                    var square = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    square.name = $"Square_{(char)('A' + file)}{rank + 1}";
                    square.transform.SetParent(_boardRoot, false);
                    square.transform.localPosition = SquareToLocalPosition(boardSquare, boardHeight);
                    square.transform.localScale = new Vector3(squareSize, boardThickness * 0.5f, squareSize);
                    Tint(square, ((file + rank) % 2 == 0) ? lightSquareColor : darkSquareColor);
                    AttachOrRefreshSquareView(square, boardSquare);
                }
            }
        }

        private void BuildPieces()
        {
            var board = gameController.CurrentBoard;
            for (var index = 0; index < 64; index++)
            {
                var square = BoardSquare.FromIndex(index);
                var piece = board.GetPiece(square);
                if (piece.IsNone)
                {
                    continue;
                }

                CreatePieceView(square, piece);
            }
        }

        private void CreatePieceView(BoardSquare square, Piece piece)
        {
            var root = CreateVisualForPiece(piece, 1f, out var usedPrefab);
            root.transform.SetParent(_piecesRoot, false);

            // Use the canonical rest-Y from an existing peer piece (same type & colour) so
            // that promoted pieces land at the exact same height as the rest of the set.
            // This matters especially when pieces were artist-placed (rebuildOnStart = false)
            // or when PieceLocalPosition offsets don't match the prefab pivot height.
            // Falls back to PieceLocalPosition when no peer exists yet (initial board build).
            var spawnPosition = PieceLocalPosition(square, piece.Type);
            if (TryGetPeerRestLocalY(piece, out var peerY))
            {
                spawnPosition.y = peerY;
            }

            root.transform.localPosition = spawnPosition;
            EnsureRootCollider(root);

            if (!usedPrefab)
            {
                Tint(root, piece.Color == PieceColor.White ? whitePieceColor : blackPieceColor);
            }

            AttachOrRefreshPieceView(root, square, piece);

            if (!usedPrefab && piece.Type == PieceType.King)
            {
                AddKingCrown(root.transform, piece.Color);
            }

            if (!usedPrefab && piece.Type == PieceType.Queen)
            {
                AddQueenTop(root.transform, piece.Color);
            }
        }

        private bool TryGetPeerRestLocalY(Piece piece, out float restY)
        {
            restY = 0f;
            if (_piecesRoot == null)
            {
                return false;
            }

            var views = _piecesRoot.GetComponentsInChildren<PieceView>(true);
            for (var i = 0; i < views.Length; i++)
            {
                var v = views[i];
                if (v != null && v.PieceType == piece.Type && v.PieceColor == piece.Color)
                {
                    restY = v.RestLocalY;
                    return true;
                }
            }

            return false;
        }

        private GameObject CreateVisualForPiece(Piece piece, float scaleMultiplier, out bool usedPrefab)
        {
            if (TryGetPiecePrefab(piece.Type, piece.Color, out var prefab))
            {
                var instance = Instantiate(prefab);
                var targetScale = piecePrefabScale <= 0f ? 1f : piecePrefabScale;
                var multiplier = scaleMultiplier <= 0f ? 1f : scaleMultiplier;
                ApplyPrefabScale(instance.transform, targetScale * multiplier);
                usedPrefab = true;
                return instance;
            }

            if (TryGetPieceTemplate(piece, out var template))
            {
                var instance = Instantiate(template);
                var multiplier = scaleMultiplier <= 0f ? 1f : scaleMultiplier;
                ApplyPrefabScale(instance.transform, multiplier);
                usedPrefab = true;
                return instance;
            }

            usedPrefab = false;
            var root = CreatePrimitiveForPiece(piece.Type);
            ApplyPrefabScale(root.transform, scaleMultiplier);
            return root;
        }

        private static void ApplyPrefabScale(Transform target, float scaleFactor)
        {
            if (Mathf.Abs(scaleFactor - 1f) < 0.0001f)
            {
                return;
            }

            target.localScale *= scaleFactor;
        }

        private bool TryGetPiecePrefab(PieceType pieceType, PieceColor pieceColor, out GameObject prefab)
        {
            prefab = null;
            if (piecePrefabs == null)
            {
                return false;
            }

            for (var i = 0; i < piecePrefabs.Length; i++)
            {
                if (piecePrefabs[i].pieceType != pieceType)
                {
                    continue;
                }

                prefab = pieceColor == PieceColor.White ? piecePrefabs[i].whitePrefab : piecePrefabs[i].blackPrefab;
                return prefab != null;
            }

            return false;
        }

        private bool TryGetPieceTemplate(Piece piece, out GameObject template)
        {
            template = null;
            if (_piecesRoot == null)
            {
                return false;
            }

            var pieceViews = _piecesRoot.GetComponentsInChildren<PieceView>(true);
            for (var i = 0; i < pieceViews.Length; i++)
            {
                var view = pieceViews[i];
                if (view == null)
                {
                    continue;
                }

                if (view.PieceType == piece.Type && view.PieceColor == piece.Color)
                {
                    template = view.gameObject;
                    return true;
                }
            }

            return false;
        }

        private GameObject CreatePrimitiveForPiece(PieceType pieceType)
        {
            PrimitiveType primitiveType;
            Vector3 scale;

            switch (pieceType)
            {
                case PieceType.Pawn:
                    primitiveType = PrimitiveType.Capsule;
                    scale = new Vector3(0.15f, 0.18f, 0.15f);
                    break;
                case PieceType.Knight:
                    primitiveType = PrimitiveType.Sphere;
                    scale = new Vector3(0.19f, 0.19f, 0.19f);
                    break;
                case PieceType.Bishop:
                    primitiveType = PrimitiveType.Cylinder;
                    scale = new Vector3(0.13f, 0.19f, 0.13f);
                    break;
                case PieceType.Rook:
                    primitiveType = PrimitiveType.Cube;
                    scale = new Vector3(0.18f, 0.24f, 0.18f);
                    break;
                case PieceType.Queen:
                    primitiveType = PrimitiveType.Cylinder;
                    scale = new Vector3(0.16f, 0.23f, 0.16f);
                    break;
                case PieceType.King:
                    primitiveType = PrimitiveType.Cylinder;
                    scale = new Vector3(0.16f, 0.26f, 0.16f);
                    break;
                default:
                    primitiveType = PrimitiveType.Cube;
                    scale = new Vector3(0.18f, 0.18f, 0.18f);
                    break;
            }

            var go = GameObject.CreatePrimitive(primitiveType);
            go.transform.localScale = scale;
            return go;
        }

        private void AttachOrRefreshPieceView(GameObject root, BoardSquare square, Piece piece)
        {
            var view = root.GetComponent<PieceView>();
            if (view == null)
            {
                view = root.AddComponent<PieceView>();
            }

            view.Apply(piece, square, gameController, this);
        }

        private void AttachOrRefreshSquareView(GameObject root, BoardSquare square)
        {
            var view = root.GetComponent<BoardSquareView>();
            if (view == null)
            {
                view = root.AddComponent<BoardSquareView>();
            }

            view.Apply(square, this);
        }

        private void AttachOrRefreshPromotionChoiceView(GameObject root, PieceType promotionPiece)
        {
            var view = root.GetComponent<PromotionChoiceView>();
            if (view == null)
            {
                view = root.AddComponent<PromotionChoiceView>();
            }

            view.Apply(promotionPiece, this);
        }

        private void AddQueenTop(Transform parent, PieceColor color)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "QueenTop";
            orb.transform.SetParent(parent, false);
            orb.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            orb.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Tint(orb, color == PieceColor.White ? whitePieceColor : blackPieceColor);
            SetIgnoreRaycast(orb);
        }

        private void AddKingCrown(Transform parent, PieceColor color)
        {
            var vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vertical.name = "KingCrossVertical";
            vertical.transform.SetParent(parent, false);
            vertical.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            vertical.transform.localScale = new Vector3(0.03f, 0.13f, 0.03f);
            Tint(vertical, color == PieceColor.White ? whitePieceColor : blackPieceColor);

            var horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horizontal.name = "KingCrossHorizontal";
            horizontal.transform.SetParent(parent, false);
            horizontal.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            horizontal.transform.localScale = new Vector3(0.09f, 0.03f, 0.03f);
            Tint(horizontal, color == PieceColor.White ? whitePieceColor : blackPieceColor);
            SetIgnoreRaycast(vertical);
            SetIgnoreRaycast(horizontal);
        }

        private static void SetIgnoreRaycast(GameObject gameObject)
        {
            var layer = LayerMask.NameToLayer("Ignore Raycast");
            if (layer < 0)
            {
                return;
            }

            gameObject.layer = layer;
        }

        private static void DestroyCollider(GameObject gameObject)
        {
            var col = gameObject.GetComponent<Collider>();
            if (col != null)
            {
                DestroyUnityObject(col);
            }
        }

        public Vector3 GetPieceRestLocalPosition(BoardSquare square, float restY)
        {
            return SquareToLocalPosition(square, restY);
        }

        private Vector3 SquareToLocalPosition(BoardSquare square, float y)
        {
            var boardOffset = squareSize * 3.5f;
            var x = (square.File * squareSize) - boardOffset;
            var z = (square.Rank * squareSize) - boardOffset;
            return new Vector3(x, y, z);
        }

        private bool TryGetSquareFromWorldPosition(Vector3 worldPosition, out BoardSquare square)
        {
            square = default;

            // Prefer _boardRoot: squares are placed there and their stored indices match
            // this coordinate space exactly. Fall back to _piecesRoot, then the presenter
            // itself — all three should share the same world transform, but _boardRoot is
            // the authoritative anchor.
            var refTransform = _boardRoot != null ? _boardRoot
                : _piecesRoot != null ? _piecesRoot
                : transform;

            var local = refTransform.InverseTransformPoint(worldPosition);
            var boardOffset = squareSize * 3.5f;
            var file = Mathf.RoundToInt((local.x + boardOffset) / squareSize);
            var rank = Mathf.RoundToInt((local.z + boardOffset) / squareSize);

            if (file < 0 || file > 7 || rank < 0 || rank > 7)
            {
                return false;
            }

            var centerX = (file * squareSize) - boardOffset;
            var centerZ = (rank * squareSize) - boardOffset;
            var dx = local.x - centerX;
            var dz = local.z - centerZ;
            var maxDistance = dropSquareSnapRadius > 0f ? dropSquareSnapRadius : squareSize * 0.45f;

            if ((dx * dx) + (dz * dz) > maxDistance * maxDistance)
            {
                return false;
            }

            square = new BoardSquare(file, rank);
            return true;
        }

        private Vector3 PieceLocalPosition(BoardSquare square, PieceType pieceType)
        {
            if (HasPiecePrefabs())
            {
                return SquareToLocalPosition(square, boardHeight + 0.005f);
            }

            var basePosition = SquareToLocalPosition(square, boardHeight + 0.03f);
            var yOffset = pieceType switch
            {
                PieceType.Pawn => 0.18f,
                PieceType.Knight => 0.11f,
                PieceType.Bishop => 0.19f,
                PieceType.Rook => 0.12f,
                PieceType.Queen => 0.20f,
                PieceType.King => 0.22f,
                _ => 0.14f
            };

            basePosition.y += yOffset;
            return basePosition;
        }

        private bool HasPiecePrefabs()
        {
            if (piecePrefabs == null || piecePrefabs.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < piecePrefabs.Length; i++)
            {
                if (piecePrefabs[i].whitePrefab != null || piecePrefabs[i].blackPrefab != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSelectedMove(BoardSquare square, out ChessMove move)
        {
            var matchingMoves = new List<ChessMove>(4);
            for (var i = 0; i < _selectedMoves.Count; i++)
            {
                if (_selectedMoves[i].To.Equals(square))
                {
                    matchingMoves.Add(_selectedMoves[i]);
                }
            }

            if (matchingMoves.Count == 0)
            {
                move = default;
                return false;
            }

            if (matchingMoves.Count == 1)
            {
                move = matchingMoves[0];
                return true;
            }

            ShowPromotionChoices(matchingMoves);
            move = default;
            return false;
        }

        private void ShowPromotionChoices(IReadOnlyList<ChessMove> promotionMoves)
        {
            if (promotionMoves == null || promotionMoves.Count == 0 || _promotionChoicesRoot == null)
            {
                return;
            }

            _pendingPromotionMoves.Clear();
            ClearChildren(_promotionChoicesRoot);
            for (var i = 0; i < promotionMoves.Count; i++)
            {
                _pendingPromotionMoves.Add(promotionMoves[i]);
            }

            var move = promotionMoves[0];
            var movingPiece = gameController.CurrentBoard.GetPiece(move.From);
            var basePosition = SquareToLocalPosition(move.To, boardHeight + 0.16f);
            var startOffset = -((promotionMoves.Count - 1) * 0.5f);

            for (var i = 0; i < promotionMoves.Count; i++)
            {
                var choiceRoot = CreatePromotionChoiceVisual(promotionMoves[i].Promotion, movingPiece.Color);
                choiceRoot.name = $"PromotionChoice_{promotionMoves[i].Promotion}";
                choiceRoot.transform.SetParent(_promotionChoicesRoot, false);
                choiceRoot.transform.localPosition = basePosition + new Vector3((startOffset + i) * squareSize * 0.65f, 0f, squareSize * 0.95f);
                AttachOrRefreshPromotionChoiceView(choiceRoot, promotionMoves[i].Promotion);
            }

            UpdateStatusIndicator();
            UpdateAllPiecesInteractability();
        }

        private GameObject CreatePromotionChoiceVisual(PieceType promotionPiece, PieceColor color)
        {
            var root = CreateVisualForPiece(new Piece(promotionPiece, color), promotionChoiceScale, out var usedPrefab);
            EnsureRootCollider(root);
            StripPieceInteractionComponents(root);

            if (!usedPrefab)
            {
                Tint(root, color == PieceColor.White ? whitePieceColor : blackPieceColor);

                if (promotionPiece == PieceType.King)
                {
                    AddKingCrown(root.transform, color);
                }
                else if (promotionPiece == PieceType.Queen)
                {
                    AddQueenTop(root.transform, color);
                }
            }

            return root;
        }

        private void UpdateAllPiecesInteractability()
        {
            if (_piecesRoot == null)
            {
                return;
            }

            var board = gameController?.CurrentBoard;
            var gameBlocked = board == null
                || _pendingPromotionMoves.Count > 0
                || board.IsCheckmate(board.SideToMove)
                || board.IsStalemate(board.SideToMove);

            var pieceViews = _piecesRoot.GetComponentsInChildren<PieceView>(true);
            for (var i = 0; i < pieceViews.Length; i++)
            {
                var view = pieceViews[i];
                if (view == null)
                {
                    continue;
                }

                if (gameBlocked)
                {
                    view.SetInteractable(false);
                    continue;
                }

                var occupant = board.GetPiece(view.Square);
                if (occupant.IsNone || occupant.Color != board.SideToMove)
                {
                    view.SetInteractable(false);
                    continue;
                }

                // Only compute legal moves for pieces that pass the turn check —
                // keeps the per-frame cost minimal (at most ~16 pieces per refresh).
                var hasMoves = gameController.GetLegalMovesFrom(view.Square).Count > 0;
                view.SetInteractable(hasMoves);
            }
        }

        private bool TryExecuteSelectedMove(ChessMove move)
        {
            var board = gameController.CurrentBoard;
            if (board == null)
            {
                return false;
            }

            var movingPiece = board.GetPiece(move.From);
            if (movingPiece.IsNone || !TryGetPieceViewAt(move.From, out var movingView))
            {
                ClearLegalMoveHighlights();
                return false;
            }

            var targetPiece = board.GetPiece(move.To);
            var isCapture = IsCaptureMove(move, movingPiece, targetPiece);
            if (!gameController.TryMakeMove(move))
            {
                return false;
            }

            HandleCaptureVisuals(move, movingPiece, targetPiece);
            HandleCastlingVisualMove(move, movingPiece);
            UpdateMovedPieceVisual(movingView, move, movingPiece, board.GetPiece(move.To));
            if (isCapture)
            {
                ResolveAudioManager()?.PlayCapture();
            }
            else
            {
                ResolveAudioManager()?.PlayDrop();
            }

            ClearLegalMoveHighlights();
            UpdateStatusIndicator();
            gameUIManager?.CheckAndShowIfGameOver(gameController.CurrentBoard);
            UpdateAllPiecesInteractability();
            return true;
        }

        private static bool IsCaptureMove(ChessMove move, Piece movingPiece, Piece targetPiece)
        {
            if (!targetPiece.IsNone)
            {
                return true;
            }

            return movingPiece.Type == PieceType.Pawn && move.From.File != move.To.File;
        }

        private void UpdateMovedPieceVisual(PieceView movingView, ChessMove move, Piece movingPiece, Piece resultingPiece)
        {
            if (movingPiece.Type == resultingPiece.Type)
            {
                MovePieceVisual(movingView, move.To);
                return;
            }

            DestroyUnityObject(movingView.gameObject);
            CreatePieceView(move.To, resultingPiece);
        }

        private void HandleCaptureVisuals(ChessMove move, Piece movingPiece, Piece targetPiece)
        {
            if (movingPiece.Type == PieceType.Pawn &&
                move.From.File != move.To.File &&
                targetPiece.IsNone)
            {
                var captureRankOffset = movingPiece.Color == PieceColor.White ? -1 : 1;
                var capturedPawnSquare = new BoardSquare(move.To.File, move.To.Rank + captureRankOffset);
                TryDestroyPieceAt(capturedPawnSquare);
                return;
            }

            if (!targetPiece.IsNone)
            {
                TryDestroyPieceAt(move.To);
            }
        }

        private void HandleCastlingVisualMove(ChessMove move, Piece movingPiece)
        {
            if (movingPiece.Type != PieceType.King || Math.Abs(move.To.File - move.From.File) != 2)
            {
                return;
            }

            var isKingside = move.To.File > move.From.File;
            var rookFrom = new BoardSquare(isKingside ? 7 : 0, move.From.Rank);
            var rookTo = new BoardSquare(isKingside ? 5 : 3, move.From.Rank);
            if (TryGetPieceViewAt(rookFrom, out var rookView))
            {
                MovePieceVisual(rookView, rookTo);
            }
        }

        private void MovePieceVisual(PieceView pieceView, BoardSquare square)
        {
            // RestLocalY is the Y captured at Apply() time (initial board placement).
            // Using it here ensures the piece always lands flush with the board after
            // a move regardless of whether the player was holding it in the air (VR)
            // or clicking with the mouse, and regardless of prefab pivot offsets.
            pieceView.transform.localPosition = SquareToLocalPosition(square, pieceView.RestLocalY);
            pieceView.UpdateSquare(square);
            pieceView.RestoreRotation();
        }

        private bool TryDestroyPieceAt(BoardSquare square)
        {
            if (!TryGetPieceViewAt(square, out var pieceView))
            {
                return false;
            }

            DestroyUnityObject(pieceView.gameObject);
            return true;
        }

        private bool TryGetPieceViewAt(BoardSquare square, out PieceView pieceView)
        {
            pieceView = null;
            if (_piecesRoot == null)
            {
                return false;
            }

            var pieceViews = _piecesRoot.GetComponentsInChildren<PieceView>(true);
            for (var i = 0; i < pieceViews.Length; i++)
            {
                var view = pieceViews[i];
                if (view != null && view.Square.Equals(square))
                {
                    pieceView = view;
                    return true;
                }
            }

            var boardOffset = squareSize * 3.5f;
            var targetX = (square.File * squareSize) - boardOffset;
            var targetZ = (square.Rank * squareSize) - boardOffset;
            var maxDistance = Mathf.Max(squareSize * 0.45f, 0.001f);
            var bestDistanceSq = maxDistance * maxDistance;

            for (var i = 0; i < pieceViews.Length; i++)
            {
                var view = pieceViews[i];
                if (view == null)
                {
                    continue;
                }

                var localPosition = _piecesRoot.InverseTransformPoint(view.transform.position);
                var dx = localPosition.x - targetX;
                var dz = localPosition.z - targetZ;
                var distSq = dx * dx + dz * dz;
                if (distSq <= bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    pieceView = view;
                }
            }

            if (pieceView != null && !pieceView.Square.Equals(square))
            {
                pieceView.UpdateSquare(square);
            }

            return pieceView != null;
        }

        private static bool TryParseSquareName(string objectName, out BoardSquare square)
        {
            const string prefix = "Square_";
            if (!objectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                square = default;
                return false;
            }

            return BoardSquare.TryParse(objectName.Substring(prefix.Length), out square);
        }

        private void UpdateStatusIndicator()
        {
            var board = gameController != null ? gameController.CurrentBoard : null;
            gameUIManager?.UpdateGameStatus(board, _pendingPromotionMoves.Count > 0);
        }

        private GameAudioManager ResolveAudioManager()
        {
            if (gameAudioManager != null)
            {
                return gameAudioManager;
            }

            gameAudioManager = GameAudioManager.Instance != null
                ? GameAudioManager.Instance
                : FindFirstObjectByType<GameAudioManager>();
            return gameAudioManager;
        }

        private void PlayInvalidFeedback()
        {
            ResolveAudioManager()?.PlayInvalid();
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                DestroyUnityObject(child);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void Tint(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = ResolvePreferredShader();
            if (shader == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(shader) { color = color };
        }

        private static Shader ResolvePreferredShader()
        {
            var hasActiveRenderPipeline = GraphicsSettings.currentRenderPipeline != null || HasConfiguredRenderPipelineAsset();
            if (hasActiveRenderPipeline)
            {
                var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    return urpShader;
                }
            }

            return Shader.Find("Standard");
        }

        private static bool HasConfiguredRenderPipelineAsset()
        {
            var graphicsSettingsType = typeof(GraphicsSettings);
            var property = graphicsSettingsType.GetProperty("defaultRenderPipeline")
                           ?? graphicsSettingsType.GetProperty("renderPipelineAsset");

            return property?.GetValue(null) is RenderPipelineAsset;
        }
    }
}
