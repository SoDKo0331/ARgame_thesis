using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class NomadARRuntimePermissionGate : MonoBehaviour
{
    public static NomadARRuntimePermissionGate Instance { get; private set; }

    private const float FirstCameraFrameTimeoutSeconds = 25f;
    private const float AvailabilityTimeoutSeconds = 15f;

    private ARSession arSession;
    private ARCameraManager arCameraManager;
    private ARCameraBackground arCameraBackground;
    private ARPlaneManager arPlaneManager;
    private ARRaycastManager arRaycastManager;
    private ARChestSpawner chestSpawner;

    private bool hasReceivedCameraFrame;
    private bool hasSentReady;
    private Coroutine initializationRoutine;

    public bool IsReady => hasSentReady;
    public bool HasReceivedCameraFrame => hasReceivedCameraFrame;
    public Camera PrimaryArCamera => arCameraManager != null ? arCameraManager.GetComponent<Camera>() : null;
    public ARRaycastManager PrimaryRaycastManager => arRaycastManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("NomadARRuntimePermissionGate");
        bootstrapObject.AddComponent<NomadARRuntimePermissionGate>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeCameraFrames();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetRuntimeState();

        if (!string.Equals(scene.name, "ARScene", StringComparison.Ordinal))
        {
            return;
        }

        NormalizeArScene();
        SetArRuntimeEnabled(false);
        initializationRoutine = StartCoroutine(InitializeArRuntime());
    }

    private IEnumerator InitializeArRuntime()
    {
        yield return null;

#if !UNITY_IOS && !UNITY_ANDROID
        NativeBridgeManager.Instance?.SendStatusToRN("ar_initializing");
        SetArRuntimeEnabled(true);
        NotifyReady();
        yield break;
#else
        CacheSceneReferences();
        NativeBridgeManager.Instance?.SendStatusToRN("ar_initializing");

        if (arSession == null || arCameraManager == null || arCameraBackground == null)
        {
            NotifyError("ar_setup_missing", "AR scene is missing required AR session or camera components.");
            yield break;
        }

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        bool hostCameraPermissionGranted = GameSession.hostCameraPermissionGranted;
        bool unityCameraPermissionGranted = Application.HasUserAuthorization(UserAuthorization.WebCam);

        if (!hostCameraPermissionGranted && !unityCameraPermissionGranted)
        {
            NativeBridgeManager.Instance?.SendStatusToRN("requesting_camera_permission");
            Debug.Log("[NomadARRuntimePermissionGate] Requesting camera authorization.");
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            yield return null;
            yield return new WaitForSeconds(0.35f);
            unityCameraPermissionGranted = Application.HasUserAuthorization(UserAuthorization.WebCam);
        }

        if (!hostCameraPermissionGranted && !unityCameraPermissionGranted)
        {
#if UNITY_IOS
            Debug.LogWarning("[NomadARRuntimePermissionGate] Camera permission still appears unavailable after request. Continuing on iOS and waiting for AR camera frames.");
            NativeBridgeManager.Instance?.SendStatusToRN("camera_permission_pending");
#else
            NotifyError("camera_permission_denied", "Camera access is required to open the AR scene.");
            yield break;
#endif
        }

        if (hostCameraPermissionGranted && !unityCameraPermissionGranted)
        {
            Debug.Log("[NomadARRuntimePermissionGate] Continuing with host camera permission granted by React Native.");
        }

        IEnumerator availabilityOperation = ARSession.CheckAvailability();
        float availabilityDeadline = Time.realtimeSinceStartup + AvailabilityTimeoutSeconds;
        while (availabilityOperation.MoveNext() && Time.realtimeSinceStartup < availabilityDeadline)
        {
            yield return availabilityOperation.Current;
        }

        if (Time.realtimeSinceStartup >= availabilityDeadline)
        {
            NotifyError("ar_availability_timeout", "Timed out while checking AR availability.");
            yield break;
        }

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            IEnumerator installOperation = ARSession.Install();
            while (installOperation.MoveNext())
            {
                yield return installOperation.Current;
            }
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            NotifyError("ar_unsupported", "This device does not support the required AR features.");
            yield break;
        }

        SubscribeCameraFrames();
        SetArRuntimeEnabled(true);
        NativeBridgeManager.Instance?.SendStatusToRN("waiting_for_camera_frame");

        float cameraFrameDeadline = Time.realtimeSinceStartup + FirstCameraFrameTimeoutSeconds;
        while (!hasReceivedCameraFrame && Time.realtimeSinceStartup < cameraFrameDeadline)
        {
            if (ARSession.state == ARSessionState.Unsupported)
            {
                NotifyError("ar_unsupported", "This device does not support the required AR features.");
                yield break;
            }

            yield return null;
        }

        if (!hasReceivedCameraFrame)
        {
            NotifyError("camera_frame_timeout", "Camera feed did not start.");
            yield break;
        }

        NotifyReady();
#endif
    }

    private void CacheSceneReferences()
    {
        arSession = FindObjectOfType<ARSession>(true);
        chestSpawner = FindObjectOfType<ARChestSpawner>(true);

        ARCameraManager[] cameraManagers = FindObjectsOfType<ARCameraManager>(true);
        arCameraManager = SelectPrimaryCameraManager(cameraManagers);
        arCameraBackground = arCameraManager != null
            ? arCameraManager.GetComponent<ARCameraBackground>()
            : null;

        Transform primaryArRoot = arCameraManager != null
            ? FindArRoot(arCameraManager.transform)
            : null;

        arPlaneManager = SelectPrimaryComponent(FindObjectsOfType<ARPlaneManager>(true), primaryArRoot);
        arRaycastManager = SelectPrimaryComponent(FindObjectsOfType<ARRaycastManager>(true), primaryArRoot);
    }

    private void NormalizeArScene()
    {
        ARCameraManager[] cameraManagers = FindObjectsOfType<ARCameraManager>(true);
        ARCameraManager primaryCameraManager = SelectPrimaryCameraManager(cameraManagers);
        if (primaryCameraManager == null)
        {
            CacheSceneReferences();
            return;
        }

        Transform primaryArRoot = FindArRoot(primaryCameraManager.transform);
        Camera primaryCamera = primaryCameraManager.GetComponent<Camera>();
        ARCameraBackground primaryBackground = primaryCameraManager.GetComponent<ARCameraBackground>();

        HashSet<GameObject> disabledRoots = new HashSet<GameObject>();

        foreach (ARCameraManager manager in cameraManagers)
        {
            if (manager == null || manager == primaryCameraManager)
            {
                continue;
            }

            Transform duplicateRoot = FindArRoot(manager.transform);
            Camera duplicateCamera = manager.GetComponent<Camera>();
            ARCameraBackground duplicateBackground = manager.GetComponent<ARCameraBackground>();
            AudioListener duplicateListener = manager.GetComponent<AudioListener>();

            manager.enabled = false;
            if (duplicateBackground != null)
            {
                duplicateBackground.enabled = false;
            }

            if (duplicateCamera != null)
            {
                duplicateCamera.enabled = false;
            }

            if (duplicateListener != null)
            {
                duplicateListener.enabled = false;
            }

            if (duplicateRoot != null && duplicateRoot != primaryArRoot)
            {
                GameObject duplicateRootObject = duplicateRoot.gameObject;
                if (disabledRoots.Add(duplicateRootObject))
                {
                    Debug.Log("[NomadARRuntimePermissionGate] Disabled duplicate AR root: " + duplicateRootObject.name);
                    duplicateRootObject.SetActive(false);
                }
            }
        }

        ARRaycastManager primaryRaycastManager = SelectPrimaryComponent(
            FindObjectsOfType<ARRaycastManager>(true),
            primaryArRoot);
        ARPlaneManager primaryPlaneManager = SelectPrimaryComponent(
            FindObjectsOfType<ARPlaneManager>(true),
            primaryArRoot);

        foreach (ARRaycastManager manager in FindObjectsOfType<ARRaycastManager>(true))
        {
            if (manager != null)
            {
                manager.enabled = manager == primaryRaycastManager;
            }
        }

        foreach (ARPlaneManager manager in FindObjectsOfType<ARPlaneManager>(true))
        {
            if (manager != null)
            {
                manager.enabled = manager == primaryPlaneManager;
            }
        }

        chestSpawner = FindObjectOfType<ARChestSpawner>(true);
        if (chestSpawner != null)
        {
            chestSpawner.arCamera = primaryCamera;
            chestSpawner.raycastManager = primaryRaycastManager;
        }

        arSession = FindObjectOfType<ARSession>(true);
        arCameraManager = primaryCameraManager;
        arCameraBackground = primaryBackground;
        arPlaneManager = primaryPlaneManager;
        arRaycastManager = primaryRaycastManager;

        Debug.Log(
            "[NomadARRuntimePermissionGate] Primary AR camera = " +
            (primaryCamera != null ? primaryCamera.name : "null") +
            ", primary root = " +
            (primaryArRoot != null ? primaryArRoot.name : "null"));
    }

    private static ARCameraManager SelectPrimaryCameraManager(ARCameraManager[] cameraManagers)
    {
        ARCameraManager bestMatch = null;
        int bestScore = int.MinValue;

        foreach (ARCameraManager manager in cameraManagers)
        {
            if (manager == null)
            {
                continue;
            }

            int score = ScoreCameraManager(manager);
            if (bestMatch == null || score > bestScore)
            {
                bestMatch = manager;
                bestScore = score;
            }
        }

        return bestMatch;
    }

    private static int ScoreCameraManager(ARCameraManager manager)
    {
        int score = 0;
        Transform arRoot = FindArRoot(manager.transform);
        string objectName = manager.gameObject.name ?? string.Empty;
        string rootName = arRoot != null ? arRoot.gameObject.name ?? string.Empty : string.Empty;

        if (manager.isActiveAndEnabled)
        {
            score += 4;
        }

        if (manager.GetComponent<ARCameraBackground>() != null)
        {
            score += 3;
        }

        if (manager.GetComponent<Camera>() != null)
        {
            score += 3;
        }

        if (HasComponentOnRoot<ARRaycastManager>(arRoot))
        {
            score += 10;
        }

        if (HasComponentOnRoot<ARPlaneManager>(arRoot))
        {
            score += 6;
        }

        if (string.Equals(objectName, "AR Camera", StringComparison.Ordinal))
        {
            score += 6;
        }

        if (string.Equals(rootName, "AR Session Origin", StringComparison.Ordinal))
        {
            score += 6;
        }

        if (string.Equals(objectName, "Main Camera", StringComparison.Ordinal))
        {
            score -= 2;
        }

        if (string.Equals(rootName, "XR Origin", StringComparison.Ordinal) && !HasComponentOnRoot<ARRaycastManager>(arRoot))
        {
            score -= 6;
        }

        return score;
    }

    private static T SelectPrimaryComponent<T>(T[] components, Transform preferredRoot) where T : Component
    {
        if (preferredRoot != null)
        {
            foreach (T component in components)
            {
                if (component != null && FindArRoot(component.transform) == preferredRoot)
                {
                    return component;
                }
            }
        }

        foreach (T component in components)
        {
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindArRoot(Transform start)
    {
        if (start == null)
        {
            return null;
        }

        Transform current = start;
        while (current != null)
        {
            if (current.GetComponent<ARRaycastManager>() != null ||
                current.GetComponent<ARPlaneManager>() != null ||
                current.GetComponent<ARSessionOrigin>() != null ||
                current.GetComponent<XROrigin>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return start.root;
    }

    private static bool HasComponentOnRoot<T>(Transform targetRoot) where T : Component
    {
        return targetRoot != null && targetRoot.GetComponent<T>() != null;
    }

    private void SetArRuntimeEnabled(bool enabled)
    {
        if (arSession != null)
        {
            arSession.enabled = enabled;
        }

        if (arCameraManager != null)
        {
            arCameraManager.enabled = enabled;
        }

        if (arCameraBackground != null)
        {
            arCameraBackground.enabled = enabled;
        }

        if (arPlaneManager != null)
        {
            arPlaneManager.enabled = enabled;
        }

        if (arRaycastManager != null)
        {
            arRaycastManager.enabled = enabled;
        }

        if (chestSpawner != null)
        {
            chestSpawner.enabled = enabled && !GameSession.isCollectionPreviewMode;
        }
    }

    private void SubscribeCameraFrames()
    {
        if (arCameraManager == null)
        {
            return;
        }

        arCameraManager.frameReceived -= HandleCameraFrameReceived;
        arCameraManager.frameReceived += HandleCameraFrameReceived;
    }

    private void UnsubscribeCameraFrames()
    {
        if (arCameraManager == null)
        {
            return;
        }

        arCameraManager.frameReceived -= HandleCameraFrameReceived;
    }

    private void HandleCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (hasReceivedCameraFrame)
        {
            return;
        }

        hasReceivedCameraFrame = true;
        Debug.Log("[NomadARRuntimePermissionGate] First AR camera frame received.");
        NotifyReady();
    }

    private void NotifyReady()
    {
        if (hasSentReady)
        {
            return;
        }

        hasSentReady = true;

        if (GameSession.isCollectionPreviewMode)
        {
            CollectedRewardPreviewController.Instance?.RefreshPreviewIfNeeded();
            CollectedRewardARPreviewSpawner.Instance?.RefreshPreviewIfNeeded();
        }

        NativeBridgeManager.Instance?.SendReadyToRN();
    }

    private void NotifyError(string code, string message)
    {
        Debug.LogError("[NomadARRuntimePermissionGate] " + code + ": " + message);
        NativeBridgeManager.Instance?.SendErrorToRN(code, message);
    }

    private void ResetRuntimeState()
    {
        if (initializationRoutine != null)
        {
            StopCoroutine(initializationRoutine);
            initializationRoutine = null;
        }

        hasReceivedCameraFrame = false;
        hasSentReady = false;
        UnsubscribeCameraFrames();
    }
}
