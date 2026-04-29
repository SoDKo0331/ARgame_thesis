using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.ARFoundation;

public static class NomadArSceneCleaner
{
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string NomadArScenePath = "Assets/Scenes/NomadAR.unity";
    private const string LegacyRootArScenePath = "Assets/ARScene.unity";

    [MenuItem("Nomad Adventure/Clean AR Only Scene")]
    public static void Clean()
    {
        Directory.CreateDirectory("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ARScene";

        GameObject sessionObject = new GameObject("AR Session");
        sessionObject.AddComponent<ARSession>();
        sessionObject.AddComponent<ARInputManager>();

        GameObject originObject = new GameObject("AR Session Origin");
        ARSessionOrigin origin = originObject.AddComponent<ARSessionOrigin>();
        ARRaycastManager raycastManager = originObject.AddComponent<ARRaycastManager>();
        originObject.AddComponent<ARPlaneManager>();

        GameObject cameraObject = new GameObject("AR Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(originObject.transform, false);

        Camera arCamera = cameraObject.AddComponent<Camera>();
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = Color.black;
        arCamera.nearClipPlane = 0.01f;
        arCamera.farClipPlane = 50f;
        arCamera.depth = 0f;
        cameraObject.AddComponent<AudioListener>();

        TrackedPoseDriver trackedPoseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
        trackedPoseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.ColorCamera);
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        cameraObject.AddComponent<ARCameraManager>();
        cameraObject.AddComponent<ARCameraBackground>();
        origin.camera = arCamera;

        GameObject runtimeObject = new GameObject("AR Runtime");
        ARChestSpawner spawner = runtimeObject.AddComponent<ARChestSpawner>();
        spawner.raycastManager = raycastManager;
        spawner.arCamera = arCamera;
        spawner.rewardPanel = null;
        spawner.showStatusOverlay = false;
        runtimeObject.AddComponent<NomadARController>();

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        EditorSceneManager.SaveScene(scene, ArScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ArScenePath, true)
        };

        DeleteAssetIfPresent(MainScenePath);
        DeleteAssetIfPresent(NomadArScenePath);
        DeleteAssetIfPresent(LegacyRootArScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void DeleteAssetIfPresent(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }
}
