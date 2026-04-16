using ChessVR.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Codex.EditorTools
{
    public static class SandboxBoardBootstrap
    {
        private const string SandboxScenePath = "Assets/Scenes/Sandbox.unity";

        public static void ApplyAndExit()
        {
            var scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);

            var sandboxRoot = GameObject.Find("ChessSandboxRoot");
            if (sandboxRoot == null)
            {
                sandboxRoot = new GameObject("ChessSandboxRoot");
            }

            sandboxRoot.transform.position = new Vector3(0f, 0f, 1.6f);

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

            presenter.RebuildImmediate();

            var xrOrigin = GameObject.Find("XROrigin");
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = Vector3.zero;
                xrOrigin.transform.rotation = Quaternion.identity;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
    }
}
