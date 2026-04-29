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

    private Camera arCamera;
    private ARRaycastManager raycastManager;
    private GameObject spawnedPreviewObject;
    private Coroutine spawnRoutine;

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
        yield return null;
        yield return new WaitForSeconds(1.25f);

        arCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        raycastManager = FindObjectOfType<ARRaycastManager>();

        if (arCamera == null)
        {
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
    }

    private void PlacePreview(Transform previewRoot)
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (raycastManager != null && raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            previewRoot.position = hitPose.position + Vector3.up * 0.02f;
        }
        else
        {
            Vector3 fallbackPosition = arCamera.transform.position + arCamera.transform.forward * 1.15f;
            fallbackPosition.y -= 0.08f;
            previewRoot.position = fallbackPosition;
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
            float uniformScale = Mathf.Clamp(0.26f / boundsHeight, 0.08f, 0.55f);
            rootObject.transform.localScale = Vector3.one * uniformScale;
        }
        else
        {
            rootObject.transform.localScale = Vector3.one * 0.25f;
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
}
