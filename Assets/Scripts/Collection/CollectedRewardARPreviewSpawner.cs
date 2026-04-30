using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CollectedRewardARPreviewSpawner : MonoBehaviour
{
    public static CollectedRewardARPreviewSpawner Instance { get; private set; }

    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private static readonly Vector2[] previewPlaneViewportSamples =
    {
        new Vector2(0.5f, 0.62f),
        new Vector2(0.5f, 0.56f),
        new Vector2(0.42f, 0.60f),
        new Vector2(0.58f, 0.60f)
    };

    private const float MinimumPreviewPlaneDistance = 0.22f;
    private const float MaximumPreviewPlaneDistance = 1.35f;
    private const float FallbackPreviewDistance = 0.72f;
    private const float FallbackPreviewHeightOffset = -0.10f;
    private const float PlacementStabilizationDurationSeconds = 1.2f;
    private const float TargetPreviewHeightMeters = 0.42f;

    private Camera arCamera;
    private ARRaycastManager raycastManager;
    private GameObject spawnedPreviewObject;
    private Coroutine spawnRoutine;
    private Coroutine placementStabilizationRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("CollectedRewardARPreviewSpawner");
        bootstrapObject.AddComponent<CollectedRewardARPreviewSpawner>();
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupSpawnedPreview();

        if (scene.name == "ARScene" && GameSession.isCollectionPreviewMode)
        {
            spawnRoutine = StartCoroutine(SpawnPreviewWhenReady());
        }
    }

    public void RefreshPreviewIfNeeded()
    {
        CleanupSpawnedPreview();

        if (SceneManager.GetActiveScene().name != "ARScene" || !GameSession.isCollectionPreviewMode)
        {
            return;
        }

        spawnRoutine = StartCoroutine(SpawnPreviewWhenReady());
    }

    private IEnumerator SpawnPreviewWhenReady()
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline)
        {
            arCamera = ResolvePreviewCamera();
            raycastManager = ResolveRaycastManager();

            if (arCamera != null)
            {
                break;
            }

            yield return null;
        }

        if (arCamera == null)
        {
            Debug.LogWarning("[CollectedRewardARPreviewSpawner] Unable to resolve AR camera for preview spawn.");
            yield break;
        }

        SpawnPreviewObject();
        spawnRoutine = null;
    }

    private void SpawnPreviewObject()
    {
        if (spawnedPreviewObject != null)
        {
            return;
        }

        GameObject previewPrefab = RewardPreviewModelResolver.ResolvePrefab();
        GameObject previewObject = previewPrefab != null
            ? Instantiate(previewPrefab)
            : RewardPreviewModelResolver.CreateFallbackPreviewObject();

        RewardPreviewModelResolver.DisableAuxiliaryComponents(previewObject);
        previewObject.name = string.IsNullOrEmpty(GameSession.previewRewardName)
            ? "CollectedRewardPreview"
            : "CollectedRewardPreview_" + GameSession.previewRewardName;

        Transform previewRoot = EnsurePreviewRoot(previewObject.transform);
        PlacePreview(previewRoot);
        EnsureColliders(previewRoot);

        CollectedRewardPreviewInteraction interaction = gameObject.GetComponent<CollectedRewardPreviewInteraction>();
        if (interaction == null)
        {
            interaction = gameObject.AddComponent<CollectedRewardPreviewInteraction>();
        }
        interaction.Initialize(previewRoot);

        spawnedPreviewObject = previewRoot.gameObject;

        if (placementStabilizationRoutine != null)
        {
            StopCoroutine(placementStabilizationRoutine);
        }

        placementStabilizationRoutine = StartCoroutine(StabilizePreviewPlacement(previewRoot));
    }

    private void PlacePreview(Transform previewRoot)
    {
        if (TryGetNearbyPlanePose(out Pose planePose))
        {
            previewRoot.position = planePose.position + Vector3.up * 0.03f;
        }
        else
        {
            previewRoot.position = GetFallbackPreviewPosition();
        }

        Vector3 lookDirection = arCamera.transform.position - previewRoot.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            previewRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private static Transform EnsurePreviewRoot(Transform targetTransform)
    {
        GameObject rootObject = new GameObject("RewardPreviewRoot");
        rootObject.transform.position = targetTransform.position;
        rootObject.transform.rotation = targetTransform.rotation;
        rootObject.transform.localScale = Vector3.one;
        targetTransform.SetParent(rootObject.transform, false);

        float boundsHeight = CalculateBoundsHeight(rootObject.transform);
        if (boundsHeight > 0.001f)
        {
            float uniformScale = Mathf.Clamp(TargetPreviewHeightMeters / boundsHeight, 0.12f, 0.9f);
            rootObject.transform.localScale = Vector3.one * uniformScale;
        }
        else
        {
            rootObject.transform.localScale = Vector3.one * 0.32f;
        }

        AddPreviewLabel(rootObject.transform);
        return rootObject.transform;
    }

    private static void EnsureColliders(Transform previewRoot)
    {
        Collider[] colliders = previewRoot.GetComponentsInChildren<Collider>(true);
        if (colliders.Length > 0)
        {
            return;
        }

        BoxCollider boxCollider = previewRoot.gameObject.AddComponent<BoxCollider>();
        boxCollider.size = Vector3.one;
    }

    private static float CalculateBoundsHeight(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return 0f;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.size.y;
    }

    private static void AddPreviewLabel(Transform previewRoot)
    {
        GameObject labelObject = new GameObject("RewardPreviewLabel");
        labelObject.transform.SetParent(previewRoot, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.01f;

        TextMeshPro textMesh = labelObject.AddComponent<TextMeshPro>();
        textMesh.text = string.IsNullOrEmpty(GameSession.previewRewardName)
            ? "Collected Reward"
            : GameSession.previewRewardName;
        textMesh.fontSize = 8f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 30;
        }
    }

    private void CleanupSpawnedPreview()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (placementStabilizationRoutine != null)
        {
            StopCoroutine(placementStabilizationRoutine);
            placementStabilizationRoutine = null;
        }

        if (spawnedPreviewObject != null)
        {
            Destroy(spawnedPreviewObject);
            spawnedPreviewObject = null;
        }
    }

    private void OnDestroy()
    {
        CleanupSpawnedPreview();
    }

    private static Camera ResolvePreviewCamera()
    {
        if (NomadARRuntimePermissionGate.Instance?.PrimaryArCamera != null)
        {
            return NomadARRuntimePermissionGate.Instance.PrimaryArCamera;
        }

        return Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
    }

    private static ARRaycastManager ResolveRaycastManager()
    {
        if (NomadARRuntimePermissionGate.Instance?.PrimaryRaycastManager != null)
        {
            return NomadARRuntimePermissionGate.Instance.PrimaryRaycastManager;
        }

        return FindObjectOfType<ARRaycastManager>();
    }

    private IEnumerator StabilizePreviewPlacement(Transform previewRoot)
    {
        float deadline = Time.realtimeSinceStartup + PlacementStabilizationDurationSeconds;
        while (previewRoot != null && Time.realtimeSinceStartup < deadline)
        {
            arCamera = ResolvePreviewCamera();
            raycastManager = ResolveRaycastManager();

            if (arCamera != null)
            {
                PlacePreview(previewRoot);
            }

            yield return null;
        }

        placementStabilizationRoutine = null;
    }

    private bool TryGetNearbyPlanePose(out Pose bestPose)
    {
        bestPose = default;

        if (raycastManager == null || arCamera == null)
        {
            return false;
        }

        bool foundPose = false;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < previewPlaneViewportSamples.Length; i++)
        {
            Vector2 viewportSample = previewPlaneViewportSamples[i];
            Vector2 screenPoint = new Vector2(Screen.width * viewportSample.x, Screen.height * viewportSample.y);

            if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
            {
                continue;
            }

            Pose candidatePose = hits[0].pose;
            if (Vector3.Dot(candidatePose.up, Vector3.up) < 0.75f)
            {
                continue;
            }

            float candidateDistance = Vector3.Distance(arCamera.transform.position, candidatePose.position);
            if (candidateDistance < MinimumPreviewPlaneDistance || candidateDistance > MaximumPreviewPlaneDistance)
            {
                continue;
            }

            if (!foundPose || candidateDistance < bestDistance)
            {
                foundPose = true;
                bestDistance = candidateDistance;
                bestPose = candidatePose;
            }
        }

        return foundPose;
    }

    private Vector3 GetFallbackPreviewPosition()
    {
        Vector3 forward = arCamera != null ? arCamera.transform.forward : Vector3.forward;
        forward.y = Mathf.Clamp(forward.y, -0.16f, 0.08f);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        return arCamera.transform.position + forward * FallbackPreviewDistance + Vector3.up * FallbackPreviewHeightOffset;
    }
}
