using System.IO;
using Inspection.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Inspection.EditorTools
{
    /// <summary>
    /// 一键生成虚拟巡检现场：地面、墙面、机柜、配电柜、消防栓箱、管线、相机、界面和客户端组件。
    /// 菜单 Inspection/重新生成巡检场景，或命令行 -executeMethod Inspection.EditorTools.InspectionSceneBuilder.BuildFromCommandLine。
    /// </summary>
    public static class InspectionSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/InspectionSite.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const float RoomHalfWidth = 10f;  // 场地东西方向半宽（米）
        private const float RoomHalfDepth = 8f;   // 场地南北方向半深（米）
        private const float WallHeight = 3.2f;
        private const float WallThickness = 0.2f;
        private const float MaterialSmoothness = 0.15f; // 场景材质光滑度，偏低以免地面墙面反光发白

        [MenuItem("Inspection/重新生成巡检场景")]
        public static void BuildFromMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                BuildScene();
            }
        }

        /// <summary>命令行入口，供 batchmode 调用</summary>
        public static void BuildFromCommandLine()
        {
            BuildScene();
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateLighting();
            Transform environment = new GameObject("Environment").transform;
            CreateRoom(environment);
            CreateEquipment(environment);
            Camera viewCamera = CreateCamera();
            CreateEventSystem();
            CreateClient(viewCamera);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Inspection] 已生成巡检场景 {ScenePath}");
        }

        /// <summary>平行光 + 均匀环境光</summary>
        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light");
            var sunLight = lightObject.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 0.9f;
            sunLight.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.48f);
        }

        /// <summary>地面和四面墙</summary>
        private static void CreateRoom(Transform environment)
        {
            Material floorMaterial = GetOrCreateMaterial("Floor", new Color32(154, 163, 168, 255));
            Material wallMaterial = GetOrCreateMaterial("Wall", new Color32(201, 207, 211, 255));
            float wallCenterY = WallHeight / 2f;
            float fullWidth = RoomHalfWidth * 2f + WallThickness;

            CreateBlock(environment, "地面", new Vector3(0f, -0.05f, 0f), new Vector3(RoomHalfWidth * 2f, 0.1f, RoomHalfDepth * 2f), floorMaterial);
            CreateBlock(environment, "北侧墙面", new Vector3(0f, wallCenterY, RoomHalfDepth), new Vector3(fullWidth, WallHeight, WallThickness), wallMaterial);
            CreateBlock(environment, "南侧墙面", new Vector3(0f, wallCenterY, -RoomHalfDepth), new Vector3(fullWidth, WallHeight, WallThickness), wallMaterial);
            CreateBlock(environment, "东侧墙面", new Vector3(RoomHalfWidth, wallCenterY, 0f), new Vector3(WallThickness, WallHeight, RoomHalfDepth * 2f), wallMaterial);
            CreateBlock(environment, "西侧墙面", new Vector3(-RoomHalfWidth, wallCenterY, 0f), new Vector3(WallThickness, WallHeight, RoomHalfDepth * 2f), wallMaterial);
        }

        /// <summary>机柜一排、精密空调、配电柜、消防栓箱和两条管线</summary>
        private static void CreateEquipment(Transform environment)
        {
            Material cabinetMaterial = GetOrCreateMaterial("Cabinet", new Color32(46, 58, 68, 255));
            Material indicatorMaterial = GetOrCreateMaterial("Indicator", new Color32(90, 200, 120, 255));
            for (int cabinetIndex = 0; cabinetIndex < 4; cabinetIndex++)
            {
                Vector3 cabinetPosition = new Vector3(-6f + cabinetIndex * 0.8f, 1f, 5.5f);
                Transform cabinet = CreateBlock(environment, $"{cabinetIndex + 1} 号机柜", cabinetPosition, new Vector3(0.6f, 2f, 1f), cabinetMaterial);
                // 机柜正面（朝南）的运行指示灯，属于机柜的一部分
                Transform indicator = CreateBlock(cabinet, "指示灯", Vector3.zero, Vector3.one, indicatorMaterial);
                indicator.localPosition = new Vector3(0f, 0.4f, -0.51f);
                indicator.localScale = new Vector3(0.15f, 0.025f, 0.02f);
                Object.DestroyImmediate(indicator.GetComponent<InspectionTarget>());
            }

            CreateBlock(environment, "精密空调", new Vector3(-2.4f, 1f, 5.5f), new Vector3(0.8f, 2f, 0.9f), GetOrCreateMaterial("AirConditioner", new Color32(221, 226, 229, 255)));
            CreateBlock(environment, "1 号配电柜", new Vector3(RoomHalfWidth - 0.3f, 0.9f, -2f), new Vector3(0.4f, 1.8f, 1.2f), GetOrCreateMaterial("PowerCabinet", new Color32(184, 176, 154, 255)));
            CreateBlock(environment, "消防栓箱", new Vector3(-RoomHalfWidth + 0.23f, 1f, -3f), new Vector3(0.25f, 0.8f, 0.7f), GetOrCreateMaterial("FireHydrant", new Color32(179, 38, 30, 255)));
            CreatePipe(environment, "给水管", 2.6f, GetOrCreateMaterial("SupplyPipe", new Color32(63, 127, 140, 255)));
            CreatePipe(environment, "回水管", 2.3f, GetOrCreateMaterial("ReturnPipe", new Color32(122, 133, 144, 255)));
        }

        /// <summary>沿北墙水平敷设的管线</summary>
        private static void CreatePipe(Transform environment, string displayName, float height, Material material)
        {
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = displayName;
            pipe.transform.SetParent(environment, false);
            pipe.transform.SetPositionAndRotation(new Vector3(1f, height, RoomHalfDepth - 0.3f), Quaternion.Euler(0f, 0f, 90f));
            pipe.transform.localScale = new Vector3(0.16f, 7f, 0.16f);
            pipe.GetComponent<Renderer>().sharedMaterial = material;
            pipe.AddComponent<InspectionTarget>().SetDisplayName(displayName);
        }

        /// <summary>创建带碰撞体和显示名称的立方体</summary>
        private static Transform CreateBlock(Transform parent, string displayName, Vector3 position, Vector3 size, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = displayName;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            block.GetComponent<Renderer>().sharedMaterial = material;
            block.AddComponent<InspectionTarget>().SetDisplayName(displayName);
            return block.transform;
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color32(28, 38, 48, 255);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<FlyCameraController>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, -5.5f), Quaternion.Euler(5f, -20f, 0f));
            return viewCamera;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>客户端组件：入口、标记放置、标记集合和界面，并完成相互绑定</summary>
        private static void CreateClient(Camera viewCamera)
        {
            InspectionUi ui = InspectionUiBuilder.Build();
            var markerRoot = new GameObject("IssueMarkers");
            var markerRegistry = markerRoot.AddComponent<IssueMarkerRegistry>();

            var clientObject = new GameObject("InspectionClient");
            var placementController = clientObject.AddComponent<IssuePlacementController>();
            placementController.Bind(viewCamera, markerRoot.transform);
            clientObject.AddComponent<InspectionClientApp>().Bind(placementController, markerRegistry, ui.FormPanel, ui.DetailPanel, ui.Hud);
        }

        /// <summary>读取或创建场景材质；着色器取自基础几何体的默认材质，兼容内置管线和 URP</summary>
        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            string materialPath = $"{MaterialFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Directory.CreateDirectory(MaterialFolder);
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                material = new Material(probe.GetComponent<Renderer>().sharedMaterial);
                Object.DestroyImmediate(probe);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.color = color;
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", MaterialSmoothness);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", MaterialSmoothness);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>允许访问 HTTP 后端、窗口化运行、切到后台时继续运行（便于同时操作网页管理端）</summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = "虚拟巡检问题标注";
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
        }
    }
}
