using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using BlackHorizon.Player;
using BlackHorizon.Core;
using BlackHorizon.Weapons;
using BlackHorizon.AI;
using BlackHorizon.Missions;
using BlackHorizon.Systems;
using BlackHorizon.UI;

namespace BlackHorizon.EditorTools
{
    /// <summary>
    /// One-shot editor tool that assembles the playable MVP scene and all its
    /// ScriptableObject assets on disk. Run: menu "Black Horizon > Build MVP".
    /// Everything is generated procedurally so it works without any art files.
    /// </summary>
    public static class SceneBuilder
    {
        private const string MissionScenePath = "Assets/_Game/Scenes/Missions/OperationDustline.unity";

        [MenuItem("Black Horizon/Build MVP Assets")]
        public static void BuildMVPAssets()
        {
            CreateWeaponAssets();
            Debug.Log("[Black Horizon] MVP assets created.");
        }

        [MenuItem("Black Horizon/Build MVP Scene (Operation Dustline)")]
        public static void BuildMissionScene()
        {
            BuildMVPAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "OperationDustline";

            BuildLevel(scene);
            BuildLighting(scene);
            var player = BuildPlayer(scene);
            BuildEnemies(scene, player);
            BuildMission(scene, player);
            BuildSystems(scene, player);
            BuildNavMesh(scene);
            BuildCameraAndAudio(scene, player);

            if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes/Missions"))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Scenes", "Missions");
            }

            EditorSceneManager.SaveScene(scene, MissionScenePath);
            RegisterSceneInBuild(MissionScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("[Black Horizon] Operation Dustline scene built and saved: " + MissionScenePath);
        }

        // ====================================================================
        // WEAPON DATA ASSETS
        // ====================================================================
        private static void CreateWeaponAssets()
        {
            CreateWeapon("AssaultRifle", make => {
                make.weaponName = "Vanguard-7 Assault Rifle";
                make.description = "Standard-issue fictional assault rifle. Reliable, full-auto.";
                make.damage = 34f;
                make.range = 100f;
                make.fireRate = 9f;
                make.automatic = true;
                make.spread = 0.6f;
                make.spreadAimMultiplier = 0.35f;
                make.magazineSize = 30;
                make.reserveAmmo = 120;
                make.reloadTime = 2.2f;
                make.recoilForce = 0.6f;
            });

            CreateWeapon("SMG", make => {
                make.weaponName = "Raptor-PD Submachine Gun";
                make.description = "Fictional compact SMG. High fire rate, lower damage.";
                make.damage = 22f;
                make.range = 60f;
                make.fireRate = 14f;
                make.automatic = true;
                make.spread = 1.1f;
                make.spreadAimMultiplier = 0.4f;
                make.magazineSize = 40;
                make.reserveAmmo = 160;
                make.reloadTime = 1.8f;
                make.recoilForce = 0.5f;
            });

            CreateWeapon("Pistol", make => {
                make.weaponName = "Sentinel-9 Sidearm";
                make.description = "Fictional semi-auto sidearm.";
                make.damage = 28f;
                make.range = 50f;
                make.fireRate = 4.5f;
                make.automatic = false;
                make.spread = 0.8f;
                make.spreadAimMultiplier = 0.3f;
                make.magazineSize = 15;
                make.reserveAmmo = 60;
                make.reloadTime = 1.4f;
                make.recoilForce = 0.7f;
            });
        }

        private static void CreateWeapon(string name, System.Action<WeaponData> configure)
        {
            const string folder = "Assets/_Game/ScriptableObjects/Weapons";
            if (!AssetDatabase.IsValidFolder("Assets/_Game/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Weapons");

            var path = folder + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(path) != null) return;

            var so = ScriptableObject.CreateInstance<WeaponData>();
            configure(so);
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[Black Horizon] Created weapon asset: " + path);
        }

        private static WeaponData LoadWeapon(string name)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/ScriptableObjects/Weapons/" + name + ".asset");
        }

        // ====================================================================
        // LEVEL
        // ====================================================================
        private static int _envLayer = 8;   // Environment
        private static readonly Material _groundMat;
        private static readonly Material _buildingMat;

        static SceneBuilder()
        {
            _groundMat = new Material(Shader.Find("Standard"));
            _groundMat.color = new Color(0.35f, 0.34f, 0.31f);
            _buildingMat = new Material(Shader.Find("Standard"));
            _buildingMat.color = new Color(0.45f, 0.46f, 0.50f);
        }

        private static GameObject Prim(Scene scene, string name, PrimitiveType type, Vector3 pos, Vector3 scale, int layer, Material mat = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.layer = layer;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        private static void BuildLevel(Scene scene)
        {
            var root = new GameObject("Level");
            SceneManager.MoveGameObjectToScene(root, scene);

            // Ground plane (large, baked-light surface).
            var ground = Prim(scene, "Ground", PrimitiveType.Plane, Vector3.zero, new Vector3(60, 1, 60), _envLayer, _groundMat);
            ground.transform.SetParent(root.transform);

            // --- City blocks / roads ---
            BuildBuilding(scene, root.transform, new Vector3(-18, 0, -12), new Vector3(10, 9, 10));
            BuildBuilding(scene, root.transform, new Vector3(-6, 0, -16), new Vector3(8, 13, 9));
            BuildBuilding(scene, root.transform, new Vector3(8, 0, -10), new Vector3(12, 11, 8));
            BuildBuilding(scene, root.transform, new Vector3(22, 0, -18), new Vector3(9, 15, 9));

            // Narrow road (visual only, slight raise).
            var road = Prim(scene, "Road", PrimitiveType.Plane, new Vector3(0, 0.02f, 2), new Vector3(8, 1, 60), _envLayer,
                new Material(Shader.Find("Standard")) { color = new Color(0.16f, 0.16f, 0.17f) });
            road.transform.SetParent(root.transform);

            // --- Industrial zone: containers, crates, solar panels, towers ---
            for (int i = 0; i < 6; i++)
            {
                var container = Prim(scene, "Container_" + i, PrimitiveType.Cube,
                    new Vector3(6 + i * 2.4f, 1.25f, 14), new Vector3(2.4f, 2.5f, 2.4f * 3f), _envLayer,
                    new Material(Shader.Find("Standard")) { color = new Color(0.35f, 0.24f, 0.12f) });
                container.transform.SetParent(root.transform);
            }

            for (int i = 0; i < 8; i++)
            {
                var crate = Prim(scene, "Crate_" + i, PrimitiveType.Cube,
                    new Vector3(-4 + i % 4 * 1.6f, 0.5f, 18 + (i / 4) * 1.4f), new Vector3(1f, 1f, 1f), _envLayer,
                    new Material(Shader.Find("Standard")) { color = new Color(0.5f, 0.4f, 0.2f) });
                crate.transform.SetParent(root.transform);
            }

            // Weapon pickup crate marker at spawn (functional placeholder).
            var crateSpawn = Prim(scene, "ArmoryCrate", PrimitiveType.Cube,
                new Vector3(0, 0.55f, 6), new Vector3(1.2f, 1.1f, 1.2f), _envLayer,
                new Material(Shader.Find("Standard")) { color = new Color(0.6f, 0.2f, 0.15f) });
            crateSpawn.transform.SetParent(root.transform);

            BuildSolarArray(scene, root.transform, new Vector3(26, 0, 6), 3, 2);
            BuildTower(scene, root.transform, new Vector3(-26, 0, 20));
            BuildTower(scene, root.transform, new Vector3(30, 0, -30));

            // A few cover props for the AI.
            for (int i = 0; i < 4; i++)
            {
                var wall = Prim(scene, "CoverWall_" + i, PrimitiveType.Cube,
                    new Vector3(-2 + i * 3, 1f, 4), new Vector3(2.5f, 2f, 0.4f), _envLayer,
                    new Material(Shader.Find("Standard")) { color = new Color(0.4f, 0.4f, 0.4f) });
                wall.transform.SetParent(root.transform);
            }
        }

        private static void BuildBuilding(Scene scene, Transform parent, Vector3 pos, Vector3 scale)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "Building";
            b.transform.position = pos + new Vector3(0, scale.y * 0.5f, 0);
            b.transform.localScale = scale;
            b.layer = _envLayer;
            b.GetComponent<Renderer>().sharedMaterial = _buildingMat;
            SceneManager.MoveGameObjectToScene(b, scene);
            b.transform.SetParent(parent);

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Rooftop_Accent";
            roof.transform.position = pos + new Vector3(0, scale.y + 0.1f, 0);
            roof.transform.localScale = new Vector3(scale.x * 0.6f, 0.3f, scale.z * 0.6f);
            roof.layer = _envLayer;
            roof.GetComponent<Renderer>().sharedMaterial = _buildingMat;
            SceneManager.MoveGameObjectToScene(roof, scene);
            roof.transform.SetParent(b.transform);
        }

        private static void BuildSolarArray(Scene scene, Transform parent, Vector3 origin, int cols, int rows)
        {
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var panel = Prim(scene, "SolarPanel", PrimitiveType.Cube,
                        origin + new Vector3(x * 2, 0.6f, y * 3), new Vector3(1.8f, 0.1f, 2.6f), _envLayer,
                        new Material(Shader.Find("Standard")) { color = new Color(0.1f, 0.15f, 0.4f) });
                    panel.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
                    panel.transform.SetParent(parent);
                }
            }
        }

        private static void BuildTower(Scene scene, Transform parent, Vector3 pos)
        {
            for (int i = 0; i < 5; i++)
            {
                var seg = Prim(scene, "TowerSeg", PrimitiveType.Cylinder,
                    pos + new Vector3(0, 1f + i * 2, 0), new Vector3(1f, 1f, 1f), _envLayer,
                    new Material(Shader.Find("Standard")) { color = new Color(0.5f, 0.5f, 0.52f) });
                seg.transform.SetParent(parent);
            }
        }

        // ====================================================================
        // LIGHTING
        // ====================================================================
        private static void BuildLighting(Scene scene)
        {
            var sun = new GameObject("Sun");
            SceneManager.MoveGameObjectToScene(sun, scene);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.42f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.28f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.7f, 0.72f, 0.78f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.55f, 0.6f, 0.7f);
        }

        // ====================================================================
        // PLAYER
        // ====================================================================
        private static GameObject BuildPlayer(Scene scene)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.layer = GameLayers.Player;
            SceneManager.MoveGameObjectToScene(player, scene);
            player.transform.position = new Vector3(0f, 0.1f, 6f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            var health = player.AddComponent<Health>();
            health.SetForInspector(100f);

            var camGO = new GameObject("CameraRig");
            camGO.transform.SetParent(player.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGO.AddComponent<AudioListener>();

            var playerCam = player.AddComponent<PlayerCamera>();
            RuntimeHelper.SetPrivate(playerCam, "cam", cam);

            var fpc = player.AddComponent<FirstPersonController>();
            RuntimeHelper.SetPrivate(fpc, "cameraTransform", camGO.transform);

            player.AddComponent<FootstepPlayer>();

            var wm = player.AddComponent<WeaponManager>();
            var loadout = new[] { LoadWeapon("AssaultRifle"), LoadWeapon("SMG"), LoadWeapon("Pistol") };
            RuntimeHelper.SetPrivate(wm, "startingLoadout", loadout);

            player.AddComponent<PlayerHealth>();

            var interact = player.AddComponent<Interaction.InteractionController>();
            RuntimeHelper.SetPrivate(interact, "cameraTransform", camGO.transform);

            return player;
        }

        // ====================================================================
        // ENEMIES
        // ====================================================================
        private static void BuildEnemies(Scene scene, GameObject player)
        {
            var root = new GameObject("Enemies");
            SceneManager.MoveGameObjectToScene(root, scene);

            var perception = CreatePerceptionAsset();

            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = new Vector3(6f + i * 4f, 0.1f, 12f);
                CreateEnemy(scene, root.transform, pos, perception);
            }
        }

        private static PerceptionConfig CreatePerceptionAsset()
        {
            const string folder = "Assets/_Game/ScriptableObjects";
            const string path = folder + "/DefaultPerception.asset";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
            var existing = AssetDatabase.LoadAssetAtPath<PerceptionConfig>(path);
            if (existing != null) return existing;

            var cfg = ScriptableObject.CreateInstance<PerceptionConfig>();
            cfg.sightRange = 30f;
            cfg.fieldOfView = 90f;
            cfg.hearingRange = 25f;
            cfg.lineOfSightMask = GameLayers.EnvironmentMask;
            cfg.attackRange = 28f;
            cfg.attackCooldown = 0.9f;
            cfg.loseSightTime = 3.5f;
            cfg.patrolSpeed = 2.5f;
            cfg.combatSpeed = 5.2f;
            cfg.turnSpeed = 540f;
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            return cfg;
        }

        private static GameObject CreateEnemy(Scene scene, Transform parent, Vector3 pos, PerceptionConfig perception)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy_Militant";
            enemy.tag = "Enemy";
            enemy.layer = GameLayers.Enemy;
            enemy.transform.position = pos;
            enemy.transform.localScale = new Vector3(1f, 1.3f, 1f);
            enemy.GetComponent<Collider>().isTrigger = false;

            SceneManager.MoveGameObjectToScene(enemy, scene);
            enemy.transform.SetParent(parent);

            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 3f;
            agent.angularSpeed = 720f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;
            agent.radius = 0.4f;
            agent.height = 1.8f;

            var health = enemy.AddComponent<Health>();
            health.SetForInspector(60f);

            var ctrl = enemy.AddComponent<EnemyController>();
            RuntimeHelper.SetPrivate(ctrl, "perception", perception);

            return enemy;
        }

        // ====================================================================
        // MISSION
        // ====================================================================
        private static void BuildMission(Scene scene, GameObject player)
        {
            var root = new GameObject("Mission");
            SceneManager.MoveGameObjectToScene(root, scene);

            var manager = root.AddComponent<MissionManager>();

            var objectives = new List<MissionObjective>();

            // Objective 1: Reach the industrial zone.
            objectives.Add(CreateObjective(scene, root.transform, "Reach the industrial zone",
                ObjectiveType.ReachLocation, new Vector3(6f, 0f, 14f), requiredDistance: 8f));

            // Objective 2: Eliminate hostiles.
            objectives.Add(CreateObjective(scene, root.transform, "Eliminate hostiles",
                ObjectiveType.EliminateEnemies, new Vector3(6f, 0f, 12f), requiredCount: 3));

            // Objective 3: Reach extraction.
            objectives.Add(CreateObjective(scene, root.transform, "Reach the extraction point",
                ObjectiveType.ReachExtraction, new Vector3(-24f, 0f, 22f), requiredDistance: 8f));

            RuntimeHelper.SetPrivate(manager, "objectives", objectives.ToArray());
            RuntimeHelper.SetPrivate(manager, "playerTransform", player.transform);

            // Checkpoints.
            CreateCheckpoint(scene, root.transform, "Checkpoint_Industrial", new Vector3(2f, 0f, 10f));
            CreateCheckpoint(scene, root.transform, "Checkpoint_Extraction", new Vector3(-18f, 0f, 18f));
        }

        private static MissionObjective CreateObjective(Scene scene, Transform parent, string title,
            ObjectiveType type, Vector3 pos, float requiredDistance = 6f, int requiredCount = 1)
        {
            var marker = new GameObject("Objective_" + title);
            marker.transform.position = pos;
            SceneManager.MoveGameObjectToScene(marker, scene);
            marker.transform.SetParent(parent);
            var obj = marker.AddComponent<MissionObjective>();
            RuntimeHelper.SetPrivate(obj, "title", title);
            RuntimeHelper.SetPrivate(obj, "type", type);
            if (type == ObjectiveType.EliminateEnemies || type == ObjectiveType.Collect)
                RuntimeHelper.SetPrivate(obj, "requiredCount", requiredCount);
            else
                RuntimeHelper.SetPrivate(obj, "requiredDistance", requiredDistance);
            return obj;
        }

        private static void CreateCheckpoint(Scene scene, Transform parent, string name, Vector3 pos)
        {
            var cp = new GameObject(name);
            cp.transform.position = pos;
            SceneManager.MoveGameObjectToScene(cp, scene);
            cp.transform.SetParent(parent);
            var col = cp.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(6f, 4f, 6f);
            cp.AddComponent<Checkpoint>();
        }

        // ====================================================================
        // SYSTEMS / HUD / CAMERA
        // ====================================================================
        private static void BuildSystems(Scene scene, GameObject player)
        {
            var gm = new GameObject("GameManager");
            SceneManager.MoveGameObjectToScene(gm, scene);
            gm.AddComponent<GameManager>();

            var pooler = new GameObject("ObjectPooler");
            SceneManager.MoveGameObjectToScene(pooler, scene);
            pooler.AddComponent<ObjectPooler>();

            var audio = new GameObject("AudioManager");
            SceneManager.MoveGameObjectToScene(audio, scene);
            audio.AddComponent<Audio.AudioManager>();

            var hud = new GameObject("HUD");
            SceneManager.MoveGameObjectToScene(hud, scene);
            var hudCtrl = hud.AddComponent<HUDController>();
            RuntimeHelper.SetPrivate(hudCtrl, "player", player.transform);
        }

        private static void BuildCameraAndAudio(Scene scene, GameObject player)
        {
            // Cinematic intro on the player camera.
            var intro = player.AddComponent<Missions.CinematicIntro>();
            RuntimeHelper.SetPrivate(intro, "playerTransform", player.transform);
            var camGO = player.transform.Find("CameraRig");
            if (camGO != null) RuntimeHelper.SetPrivate(intro, "gameplayCamera", camGO.GetComponent<Camera>());

            // Fade plane (fullscreen black quad).
            var fade = CreateFadePlane(scene);
            RuntimeHelper.SetPrivate(intro, "fadePlane", fade);
            RuntimeHelper.SetPrivate(intro, "fadeMaterial", fade.GetComponent<MeshRenderer>().sharedMaterial);
        }

        private static GameObject CreateFadePlane(Scene scene)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FadePlane";
            go.transform.SetParent(Camera.main.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            go.transform.localScale = new Vector3(0.6f, 0.4f, 1f);
            var fadeMat = new Material(Shader.Find("Unlit/Color"));
            fadeMat.color = Color.black;
            go.GetComponent<Renderer>().sharedMaterial = fadeMat;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            go.layer = 2; // Ignore Raycast
            go.SetActive(false); // hidden; intro enables it
            return go;
        }

        // ====================================================================
        // NAVMESH
        // ====================================================================
        private static void BuildNavMesh(Scene scene)
        {
            var go = new GameObject("NavMesh");
            SceneManager.MoveGameObjectToScene(go, scene);

            NavMeshSurface surface = go.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = go.AddComponent<NavMeshSurface>();
            }
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            surface.BuildNavMesh();
        }

        private static void RegisterSceneInBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    /// <summary>
    /// Tiny reflection helper used only by editor tools to populate private
    /// serialized fields at build time so the Inspector stays clean.
    /// </summary>
    public static class RuntimeHelper
    {
        public static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }
    }
}
