using ChessVR.Domain;
using UnityEngine;
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

        private Transform _boardRoot;
        private Transform _piecesRoot;

        private void Start()
        {
            if (rebuildOnStart)
            {
                RebuildImmediate();
            }
        }

        [ContextMenu("Rebuild Board")]
        public void RebuildImmediate()
        {
            EnsureDependencies();
            EnsureRoots();

            ClearChildren(_boardRoot);
            ClearChildren(_piecesRoot);

            BuildBoard();
            BuildPieces();
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

            var view = root.AddComponent<PieceView>();
            view.Apply(piece, square);

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

        private void AddQueenTop(Transform parent, PieceColor color)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "QueenTop";
            orb.transform.SetParent(parent, false);
            orb.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            orb.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Tint(orb, color == PieceColor.White ? whitePieceColor : blackPieceColor);
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
