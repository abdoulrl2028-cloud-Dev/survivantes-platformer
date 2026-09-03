using UnityEditor;
using UnityEngine;

namespace BlackHorizon.EditorTools
{
    /// <summary>
    /// Automatically generates the playable Operation Dustline MVP scene the
    /// first time the editor becomes healthy. Guarded by a menu-item flag and
    /// the scene file's existence, so it runs once and never overwrites the
    /// user's scene afterwards. If the scene file already exists, this does
    /// nothing.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSceneBootstrap
    {
        private const string ScenePath = "Assets/_Game/Scenes/Missions/OperationDustline.unity";
        private const string BuildKey = "BlackHorizon.AutoBuildMVP";

        static AutoSceneBootstrap()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            // Only run in a healthy state, once, and when the scene is missing.
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool alreadyBuilt = SessionState.GetBool(BuildKey, false);
            if (alreadyBuilt) return;

            if (System.IO.File.Exists(ScenePath))
            {
                SessionState.SetBool(BuildKey, true);
                return;
            }

            SessionState.SetBool(BuildKey, true);
            SceneBuilder.BuildMissionScene();
            Debug.Log("[Black Horizon] Auto-built the playable MVP scene (Operation Dustline).");
        }

        [MenuItem("Black Horizon/Rebuild MVP Scene (manual)")]
        public static void RebuildManually()
        {
            SceneBuilder.BuildMissionScene();
            SessionState.SetBool(BuildKey, true);
        }
    }
}
