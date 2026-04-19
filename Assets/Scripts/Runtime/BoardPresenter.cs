using System.Collections.Generic;
using System.Reflection;
using ChessVR.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace ChessVR.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoardPresenter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ChessGameController gameController;

        [Header("Layout")]
        [SerializeField] private float squareSize = 0.32f;
        [SerializeField] private float boardThickness = 0.04f;
        [SerializeField] private float boardHeight = 0.8f;
        [SerializeField] private float boardPadding = 0.08f;

        [Header("Visuals")]
        [SerializeField] private Color lightSquareColor = new(0.91f, 0.88f, 0.81f, 1f);
        [SerializeField] private Color darkSquareColor = new(0.29f, 0.22f, 0.16f, 1f);
        [SerializeField] private Color borderColor = new(0.11f, 0.08f, 0.06f, 1f);
        [SerializeField] private Color whitePieceColor = new(0.92f, 0.92f, 0.9f, 1f);
        [SerializeField] private Color blackPieceColor = new(0.16f, 0.16f, 0.18f, 1f);

        [Header("Runtime")]
        [SerializeField] private bool rebuildOnStart = true;

        [Header("Legal move highlights")]
        [SerializeField] private Color legalMoveHighlightColor = new(0.15f, 0.75f, 0.35f, 1f);
        [SerializeField] private float highlightHeightOffset = 0.05f;

        [Tooltip("Promień dopasowania figury do pola (przestrzeń lokalna PiecesRoot), w jednostkach świata lokalnego — duży = ręczne ustawienia.")]
        [SerializeField] private float pieceSquareMatchRadius = 0.9f;

        [Header("Collider fix (for manually scaled pieces)")]
        [Tooltip("Jeśli figury są ręcznie skalowane (np. skala 25), BoxCollider będzie dopasowany do rendererów.")]
        [SerializeField] private bool fixPieceCollidersOnStart = true;

        private Transform _boardRoot;
        private Transform _piecesRoot;
        private Transform _highlightsRoot;
        private BoardSquare? _selectedSquare;

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
                EnsurePieceViewsForPiecesUnderRoot();
                if (fixPieceCollidersOnStart)
                {
                    FixPieceCollidersUnderRoot();
                }
            }
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            if (es.IsPointerOverGameObject(-1) || es.IsPointerOverGameObject(0))
            {
                Debug.Log("KLIK ZABLOKOWANY PRZEZ UI");
            }
        }

        [ContextMenu("Rebuild Board")]
        public void RebuildImmediate()
        {
            EnsureDependencies();
            EnsureRoots();

            ClearLegalMoveHighlights();
            ClearChildren(_boardRoot);
            ClearChildren(_piecesRoot);

            BuildBoard();
            BuildPieces();
            EnsurePieceViewsForPiecesUnderRoot();
            if (fixPieceCollidersOnStart)
            {
                FixPieceCollidersUnderRoot();
            }
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
                        Destroy(view);
                    }
                }

                if (target.GetComponent<PieceView>() == null)
                {
                    target.AddComponent<PieceView>();
                }
            }
        }

        private static void RemoveCapsuleColliders(Transform root)
        {
            var capsules = root.GetComponentsInChildren<CapsuleCollider>(true);
            for (var i = 0; i < capsules.Length; i++)
            {
                if (capsules[i] != null)
                {
                    Destroy(capsules[i]);
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

        /// <summary>
        /// Dla każdego collidera pod <see cref="_piecesRoot"/> dopina <see cref="PieceView"/> i odświeża referencje
        /// (klik trafia w obiekt z colliderem — często mesh dziecka, nie korzeń figury).
        /// </summary>
        public void EnsurePieceViewsForPiecesUnderRoot()
        {
            EnsureDependencies();
            EnsureRoots();

            var board = gameController.CurrentBoard;
            if (board == null || _piecesRoot == null)
            {
                return;
            }

            var colliders = _piecesRoot.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null || !col.enabled || col.isTrigger)
                {
                    continue;
                }

                var pieceRoot = FindPieceRootUnderPiecesRoot(col.transform);
                if (pieceRoot == null)
                {
                    Debug.Log($"Nie dopasowano obiektu {col.gameObject.name}");
                    continue;
                }

                var matchPosition = _piecesRoot.InverseTransformPoint(col.bounds.center);
                if (!TryMatchPieceAtSquareFromLocalPosition(matchPosition, board, out var square, out var piece))
                {
                    Debug.Log(
                        $"Nie dopasowano obiektu {col.gameObject.name} (środek collidera w lokalnym układzie PiecesRoot: X={matchPosition.x:F3}, Z={matchPosition.z:F3})");
                    continue;
                }

                Debug.Log(
                    $"Znalazłem obiekt {col.gameObject.name} na pozycji [{matchPosition.x:F3}, {matchPosition.z:F3}], przypisuję go do pola [{square}]");

                var host = col.gameObject;
                var view = host.GetComponent<PieceView>();
                if (view == null)
                {
                    view = host.AddComponent<PieceView>();
                }

                view.Apply(piece, square, gameController, this);
            }
        }

        private Transform FindPieceRootUnderPiecesRoot(Transform t)
        {
            var current = t;
            while (current != null && current.parent != _piecesRoot)
            {
                current = current.parent;
            }

            return current != null && current.parent == _piecesRoot ? current : null;
        }

        /// <summary>
        /// Dopasowuje pole i figurę po XZ w przestrzeni lokalnej <see cref="_piecesRoot"/> (duży próg pod ręczne ustawienia).
        /// </summary>
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

        /// <summary>
        /// Przywraca domyślny asset akcji UI (wbudowany DefaultInputActions) i referencje Point / Click itd.,
        /// gdy moduł ma puste sloty — typowy przypadek przy ręcznej scenie bez przypisanego Input Action Asset.
        /// </summary>
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
                Debug.Log("EventSystem: Domyślne akcje wejścia zostały przypisane.");
            }

            TryEnableBuiltinActionsAsFallback(module);
        }

        private static bool NeedsDefaultUiActions(InputSystemUIInputModule module)
        {
            return module.point == null || module.point.action == null
                   || module.leftClick == null || module.leftClick.action == null;
        }

        /// <summary>
        /// Ustawia opcję inspektora „Enable Builtin Actions As Fallback”, jeśli istnieje w tej wersji Input Systemu.
        /// </summary>
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

        public void ClearLegalMoveHighlights()
        {
            _selectedSquare = null;
            if (_highlightsRoot == null)
            {
                return;
            }

            ClearChildren(_highlightsRoot);
        }

        /// <summary>Rysuje płaskie znaczniki na polach docelowych; lista ruchów pochodzi z <see cref="ChessGameController.GetLegalMovesFrom"/> (adapter zasad).</summary>
        public void ShowLegalMoveHighlights(BoardSquare from, IReadOnlyList<ChessMove> legalMoves)
        {
            EnsureDependencies();
            EnsureRoots();
            ClearLegalMoveHighlights();
            _selectedSquare = from;

            if (legalMoves == null || legalMoves.Count == 0)
            {
                return;
            }

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
        }

        private void EnsureRoots()
        {
            _boardRoot = FindOrCreateChild("BoardRoot");
            _piecesRoot = FindOrCreateChild("PiecesRoot");
            _highlightsRoot = FindOrCreateChild("LegalHighlightsRoot");
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

        private void BuildBoard()
        {
            var boardWorldSize = squareSize * 8f;

            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = "BoardStand";
            stand.transform.SetParent(_boardRoot, false);
            stand.transform.localPosition = new Vector3(0f, boardHeight * 0.5f, 0f);
            stand.transform.localScale = new Vector3(boardWorldSize * 0.38f, boardHeight, boardWorldSize * 0.38f);
            Tint(stand, borderColor * 0.85f);

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
                    var square = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    square.name = $"Square_{(char)('A' + file)}{rank + 1}";
                    square.transform.SetParent(_boardRoot, false);
                    square.transform.localPosition = SquareToLocalPosition(new BoardSquare(file, rank), boardHeight);
                    square.transform.localScale = new Vector3(squareSize, boardThickness * 0.5f, squareSize);
                    Tint(square, ((file + rank) % 2 == 0) ? lightSquareColor : darkSquareColor);
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
            var root = CreatePrimitiveForPiece(piece.Type);
            root.transform.SetParent(_piecesRoot, false);
            root.transform.localPosition = PieceLocalPosition(square, piece.Type);
            Tint(root, piece.Color == PieceColor.White ? whitePieceColor : blackPieceColor);

            AttachOrRefreshPieceView(root, square, piece);

            if (piece.Type == PieceType.King)
            {
                AddKingCrown(root.transform, piece.Color);
            }

            if (piece.Type == PieceType.Queen)
            {
                AddQueenTop(root.transform, piece.Color);
            }
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
                Destroy(col);
            }
        }

        private Vector3 SquareToLocalPosition(BoardSquare square, float y)
        {
            var boardOffset = squareSize * 3.5f;
            var x = (square.File * squareSize) - boardOffset;
            var z = (square.Rank * squareSize) - boardOffset;
            return new Vector3(x, y, z);
        }

        private Vector3 PieceLocalPosition(BoardSquare square, PieceType pieceType)
        {
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

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
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
