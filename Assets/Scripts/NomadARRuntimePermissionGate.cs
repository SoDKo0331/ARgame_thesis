using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

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

        CacheSceneReferences();
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

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            NativeBridgeManager.Instance?.SendStatusToRN("requesting_camera_permission");
            Debug.Log("[NomadARRuntimePermissionGate] Requesting camera authorization.");
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            NotifyError("camera_permission_denied", "Camera access is required to open the AR scene.");
            yield break;
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
        arCameraManager = FindObjectOfType<ARCameraManager>(true);
        arCameraBackground = FindObjectOfType<ARCameraBackground>(true);
        arPlaneManager = FindObjectOfType<ARPlaneManager>(true);
        arRaycastManager = FindObjectOfType<ARRaycastManager>(true);
        chestSpawner = FindObjectOfType<ARChestSpawner>(true);
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
