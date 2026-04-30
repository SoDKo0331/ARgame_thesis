using UnityEngine;

public class ARChestInteraction : MonoBehaviour
{
    public GameObject rewardUI;
    public Animator animator;
    public float rewardRevealDelay = 0.45f;
    public float rewardSpawnHeight = 0.28f;
    public string openTriggerName = "Open";
    public bool verboseLogging = true;

    private bool isOpening = false;
    private bool isOpened = false;
    private bool isRewardCollected = false;
    private Coroutine rewardRevealRoutine;
    private ARChestRewardCollectible activeCollectible;
    private Collider[] cachedColliders;
    private static readonly string[] FallbackOpenStateNames =
    {
        "Animated PBR Chest _Opening_UnCommon",
        "Animated PBR Chest _Open",
        "Open"
    };

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void Start()
    {
        // Skip the chest visual and tap-to-open process completely.
        isOpening = true;
        isOpened = true;
        DisableChestColliders();

        // Hide any visual children
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        if (rewardRevealRoutine != null)
        {
            StopCoroutine(rewardRevealRoutine);
        }
        
        rewardRevealRoutine = StartCoroutine(RevealRewardCollectible(true));
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryOpen(Input.mousePosition);
        }
#endif

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryOpen(Input.GetTouch(0).position);
        }
    }

    private void TryOpen(Vector2 screenPos)
    {
        if (isOpening || isOpened)
        {
            LogInfo("Chest already opening/opened. Ignoring tap.");
            return;
        }

        Camera sceneCamera = ResolveSceneCamera();
        if (sceneCamera == null)
        {
            LogInfo("Chest tap ignored because no AR camera could be resolved.");
            return;
        }

        Ray ray = sceneCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                LogInfo("Chest tap detected on " + hit.transform.name);
                OpenChest();
            }
        }
    }

    private void OpenChest()
    {
        if (isOpening || isOpened)
        {
            LogInfo("OpenChest ignored because chest is already opening/opened.");
            return;
        }

        isOpening = true;
        isOpened = true;
        LogInfo("Opening chest. Reward UI assigned = " + (rewardUI != null) + ", animator assigned = " + (animator != null));
        DisableChestColliders();

        PlayOpenAnimation();

        if (rewardUI != null)
        {
            rewardUI.SetActive(false);
        }

        if (rewardRevealRoutine != null)
        {
            StopCoroutine(rewardRevealRoutine);
        }

        rewardRevealRoutine = StartCoroutine(RevealRewardCollectible());

        LogInfo("Chest open sequence started.");
    }

    private void PlayOpenAnimation()
    {
        if (animator == null)
        {
            LogInfo("No animator on chest. Skipping open animation.");
            return;
        }

        bool usedTrigger = false;
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                parameters[i].name == openTriggerName)
            {
                animator.SetTrigger(openTriggerName);
                usedTrigger = true;
                LogInfo("Animator trigger used: " + openTriggerName);
                break;
            }
        }

        if (usedTrigger)
        {
            return;
        }

        for (int i = 0; i < FallbackOpenStateNames.Length; i++)
        {
            string stateName = FallbackOpenStateNames[i];
            if (animator.HasState(0, Animator.StringToHash(stateName)))
            {
                animator.CrossFadeInFixedTime(stateName, 0.05f, 0, 0f);
                LogInfo("Animator fallback state used: " + stateName);
                return;
            }
        }

        LogInfo("No valid animator trigger or fallback state found.");
    }

    private System.Collections.IEnumerator RevealRewardCollectible(bool immediate = false)
    {
        if (!immediate) yield return new WaitForSeconds(rewardRevealDelay);

        if (activeCollectible != null || isRewardCollected)
        {
            isOpening = false;
            yield break;
        }

        GameObject collectibleObject = new GameObject("ChestRewardCollectible");
        float spawnHeightOffset = immediate ? 0f : rewardSpawnHeight;
        collectibleObject.transform.position = transform.position + Vector3.up * spawnHeightOffset;
        LogInfo("Spawning reward collectible at " + collectibleObject.transform.position +
            " with spot = " + GameSession.selectedSpotName +
            ", preview key = " + GetCollectibleModelPrefabKey());

        ARChestRewardCollectible collectible = collectibleObject.AddComponent<ARChestRewardCollectible>();
        collectible.Initialize(
            GetCollectibleDisplayName(),
            GetCollectibleModelPrefabKey(),
            ResolveSceneCamera(),
            HandleRewardCollected);

        activeCollectible = collectible;
        isOpening = false;
        rewardRevealRoutine = null;
        LogInfo("Chest reward reveal completed.");
    }

    private void HandleRewardCollected()
    {
        if (isRewardCollected)
        {
            return;
        }

        isRewardCollected = true;
        activeCollectible = null;
        LogInfo("Reward collectible tapped. Showing reward panel and starting backend claim if possible.");

        StartBackendClaimIfNeeded();

        if (rewardUI != null)
        {
            rewardUI.SetActive(true);

            RewardPanelController panel = rewardUI.GetComponent<RewardPanelController>();
            if (panel != null)
            {
                panel.Refresh();
            }
        }
    }

    private void StartBackendClaimIfNeeded()
    {
        if (GameSession.rewardClaimRequested || BackendBootstrap.Instance == null)
        {
            LogInfo("Backend claim skipped. Already requested = " + GameSession.rewardClaimRequested +
                ", bootstrap exists = " + (BackendBootstrap.Instance != null));
            return;
        }

        if (!BackendBootstrap.Instance.HasBootstrappedSession ||
            string.IsNullOrEmpty(GameSession.selectedSpotId) ||
            string.IsNullOrEmpty(GameSession.userId))
        {
            LogInfo("Backend claim skipped due to missing session data. Bootstrapped = " +
                BackendBootstrap.Instance.HasBootstrappedSession +
                ", spotId empty = " + string.IsNullOrEmpty(GameSession.selectedSpotId) +
                ", userId empty = " + string.IsNullOrEmpty(GameSession.userId));
            return;
        }

        GameSession.rewardClaimRequested = true;
        GameSession.backendStatusMessage = "Collection-д нэмж байна...";
        LogInfo("Starting backend claim for spotId = " + GameSession.selectedSpotId + ", userId = " + GameSession.userId);

        BackendBootstrap.Instance.StartClaimSelectedSpotRequest(
            response =>
            {
                if (response == null)
                {
                    LogInfo("Backend claim callback returned null response.");
                    return;
                }

                LogInfo("Backend claim completed. alreadyClaimed = " + response.alreadyClaimed);

                if (rewardUI != null && rewardUI.activeInHierarchy)
                {
                    RewardPanelController panel = rewardUI.GetComponent<RewardPanelController>();
                    if (panel != null)
                    {
                        panel.Refresh();
                        panel.ShowClaimStatus(
                            response.alreadyClaimed
                                ? "Энэ шагнал аль хэдийн collection-д байна."
                                : "Collection-д нэмэгдлээ!");
                    }
                }
            },
            error =>
            {
                GameSession.rewardClaimRequested = false;

                if (rewardUI != null && rewardUI.activeInHierarchy)
                {
                    RewardPanelController panel = rewardUI.GetComponent<RewardPanelController>();
                    if (panel != null)
                    {
                        panel.ShowClaimStatus("Интернетгүй тул локал reward мэдээлэл харуулж байна.");
                    }
                }

                if (error != null)
                {
                    Debug.LogWarning("[ARChestInteraction] Reward claim failed: " + error.message, this);
                }
            });
    }

    private void OnDisable()
    {
        if (rewardRevealRoutine != null)
        {
            StopCoroutine(rewardRevealRoutine);
            rewardRevealRoutine = null;
        }

        if (activeCollectible != null)
        {
            Destroy(activeCollectible.gameObject);
            activeCollectible = null;
        }
    }

    private void DisableChestColliders()
    {
        if (cachedColliders == null || cachedColliders.Length == 0)
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
            {
                cachedColliders[i].enabled = false;
            }
        }

        LogInfo("Chest colliders disabled after opening.");
    }

    private void LogInfo(string message)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log("[ARChestInteraction] " + message, this);
    }

    private static string GetCollectibleDisplayName()
    {
        return string.IsNullOrEmpty(GameSession.selectedSpotName)
            ? GameSession.rewardName
            : GameSession.selectedSpotName;
    }

    private static string GetCollectibleModelPrefabKey()
    {
        if (!string.IsNullOrEmpty(GameSession.selectedSpotModelPrefabKey))
        {
            return GameSession.selectedSpotModelPrefabKey;
        }

        return GameSession.rewardPreviewPrefabKey;
    }

    private static Camera ResolveSceneCamera()
    {
        if (NomadARRuntimePermissionGate.Instance?.PrimaryArCamera != null)
        {
            return NomadARRuntimePermissionGate.Instance.PrimaryArCamera;
        }

        return Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
    }
}
