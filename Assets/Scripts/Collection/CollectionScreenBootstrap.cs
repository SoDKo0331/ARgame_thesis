using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CollectionScreenBootstrap : MonoBehaviour
{
    public static CollectionScreenBootstrap Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("CollectionScreenBootstrap");
        bootstrapObject.AddComponent<CollectionScreenBootstrap>();
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

    private void Start()
    {
        StartCoroutine(AttachToMainScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene")
        {
            StartCoroutine(AttachToMainScene());
        }
    }

    private IEnumerator AttachToMainScene()
    {
        yield return null;

        if (SceneManager.GetActiveScene().name != "MainScene")
        {
            yield break;
        }

        if (FindObjectOfType<CollectionScreenController>() != null)
        {
            yield break;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject controllerObject = new GameObject("CollectionScreenController", typeof(RectTransform));
        controllerObject.transform.SetParent(canvas.transform, false);
        controllerObject.transform.SetAsLastSibling();

        CollectionScreenController controller = controllerObject.AddComponent<CollectionScreenController>();
        controller.Initialize();
    }
}
