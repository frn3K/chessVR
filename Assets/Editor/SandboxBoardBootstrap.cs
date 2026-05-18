using ChessVR.Domain;
using ChessVR.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Codex.EditorTools
{
    public static class SandboxBoardBootstrap
    {
        private const string SandboxScenePath = "Assets/Scenes/Sandbox.unity";
        private const string PiecePrefabFolder = "Assets/Chess MEGA-pack/prefabs/pieces/lowPoly2/";
        private const float BoardSurfaceWorldY = 0.02f;
        private const float PlayerEyeHeightAboveBoard = 0.7f;

        public static void ApplyAndExit()
        {
            var scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);

            var sandboxRoot = GameObject.Find("ChessSandboxRoot");
            if (sandboxRoot == null)
            {
                sandboxRoot = new GameObject("ChessSandboxRoot");
            }

            sandboxRoot.transform.position = new Vector3(0f, 0f, 1.2f);

            var controller = sandboxRoot.GetComponent<ChessGameController>();
            if (controller == null)
            {
                controller = sandboxRoot.AddComponent<ChessGameController>();
            }

            var presenter = sandboxRoot.GetComponent<BoardPresenter>();
            if (presenter == null)
            {
                presenter = sandboxRoot.AddComponent<BoardPresenter>();
            }

            WirePiecePrefabs(presenter);
            presenter.RebuildImmediate();
            MinimalStudioBuilder.BuildForSandbox(sandboxRoot);
            GameUIBuilder.BuildChessUIForSandbox(sandboxRoot, selectPanel: false, showDialog: false);

            SetupXROrigin();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tools/Wire Piece Prefabs")]
        public static void WirePiecePrefabsMenu()
        {
            var sandboxRoot = GameObject.Find("ChessSandboxRoot");
            if (sandboxRoot == null)
            {
                EditorUtility.DisplayDialog("Wire Prefabs", "Nie znaleziono 'ChessSandboxRoot'.", "OK");
                return;
            }

            sandboxRoot.transform.position = new Vector3(0f, 0f, 1.2f);

            var presenter = sandboxRoot.GetComponent<BoardPresenter>();
            if (presenter == null)
            {
                EditorUtility.DisplayDialog("Wire Prefabs", "Brak BoardPresenter na ChessSandboxRoot.", "OK");
                return;
            }

            WirePiecePrefabs(presenter);
            presenter.RebuildImmediate();
            SetupXROrigin();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void SetupXROrigin()
        {
            var xrOrigin = GameObject.Find("XROrigin");
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("XR Origin");
            }

            if (xrOrigin == null)
            {
                return;
            }

            xrOrigin.transform.position = new Vector3(0f, BoardSurfaceWorldY, 0f);
            xrOrigin.transform.rotation = Quaternion.identity;
            SetCameraOffsetHeight(xrOrigin.transform, PlayerEyeHeightAboveBoard);
            SetXrOriginCameraYOffset(xrOrigin, PlayerEyeHeightAboveBoard);

            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = xrOrigin.AddComponent<CharacterController>();
            }

            cc.height = 1.8f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 0f;
            cc.stepOffset = 0.1f;
        }

        private static void SetCameraOffsetHeight(Transform xrOrigin, float height)
        {
            var cameraOffset = xrOrigin.Find("Camera Offset");
            if (cameraOffset == null)
            {
                return;
            }

            var localPosition = cameraOffset.localPosition;
            cameraOffset.localPosition = new Vector3(localPosition.x, height, localPosition.z);
        }

        private static void SetXrOriginCameraYOffset(GameObject xrOrigin, float height)
        {
            var behaviours = xrOrigin.GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                var so = new SerializedObject(behaviour);
                var cameraYOffset = so.FindProperty("m_CameraYOffset");
                if (cameraYOffset == null)
                {
                    continue;
                }

                cameraYOffset.floatValue = height;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WirePiecePrefabs(BoardPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var prefabsProperty = so.FindProperty("piecePrefabs");
            if (prefabsProperty == null)
            {
                return;
            }

            var pieces = new[]
            {
                (PieceType.Pawn, "pawnLowPoly2"),
                (PieceType.Knight, "knightLowPoly2"),
                (PieceType.Bishop, "bishopLowPoly2"),
                (PieceType.Rook, "rookLowPoly2"),
                (PieceType.Queen, "queenLowPoly2"),
                (PieceType.King, "kingLowPoly2"),
            };

            prefabsProperty.arraySize = pieces.Length;
            for (var i = 0; i < pieces.Length; i++)
            {
                var (pieceType, baseName) = pieces[i];
                var whitePath = PiecePrefabFolder + baseName + " 1.prefab";
                var blackPath = PiecePrefabFolder + baseName + ".prefab";

                var element = prefabsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("pieceType").intValue = (int)pieceType;
                element.FindPropertyRelative("whitePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(whitePath);
                element.FindPropertyRelative("blackPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(blackPath);
            }

            var scaleProperty = so.FindProperty("piecePrefabScale");
            if (scaleProperty != null)
            {
                scaleProperty.floatValue = 0.32f;
            }

            var heightProperty = so.FindProperty("boardHeight");
            if (heightProperty != null)
            {
                heightProperty.floatValue = 0.02f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
