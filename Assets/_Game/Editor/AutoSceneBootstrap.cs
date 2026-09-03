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

        static AutoSceneBootstrap()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void TryAutoBuild()
        {
            // Only run in a healthy state.
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Idempotent: once the scene exists we never touch it again.
            if (System.IO.File.Exists(ScenePath))
                return;

            try
            {
                SceneBuilder.BuildMissionScene();
                if (System.IO.File.Exists(ScenePath))
                {
                    Debug.Log("[Black Horizon] Auto-built the playable MVP scene (Operation Dustline).");
                }
                else
                {
                    Debug.LogWarning("[Black Horizon] Scene build reported completion but the scene file is missing; will retry on next editor load.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Black Horizon] Automated scene build failed and will retry on next editor load.\n" + e);
            }
        }

        [MenuItem("Black Horizon/Rebuild MVP Scene (manual)")]
        public static void RebuildManually()
        {
            SceneBuilder.BuildMissionScene();
        }
    }
}
