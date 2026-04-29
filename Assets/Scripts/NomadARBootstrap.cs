using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class NomadARBootstrap : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[NomadARBootstrap] Start triggered.");
        if (Application.isPlaying) EnsureARSetup();
    }

    public void EnsureARSetup()
    {
        Debug.Log("[NomadARBootstrap] Cleaning scene...");

        try {
            // 1. Destroy EVERYTHING that could conflict
            foreach (var s in FindObjectsOfType<ARSession>(true)) DestroyImmediate(s.gameObject);
            foreach (var o in FindObjectsOfType<ARSessionOrigin>(true)) DestroyImmediate(o.gameObject);
            
            // Kill any simulation or legacy objects by name
            string[] garbage = { "SimulationCamera", "AR Session", "AR Session Origin", "RewardCollectibleSpot", "ARChestStatusOverlay", "RewardPanelCanvas" };
            foreach (var name in garbage) {
                var obj = GameObject.Find(name);
                if (obj != null) DestroyImmediate(obj);
            }

            // Disable ALL cameras
            foreach (var c in FindObjectsOfType<Camera>(true)) {
                c.enabled = false;
                c.gameObject.SetActive(false);
            }

            // 2. Fresh AR Setup
            GameObject sessionObj = new GameObject("AR Session");
            sessionObj.AddComponent<ARSession>();
            sessionObj.AddComponent<ARInputManager>();

            GameObject originObj = new GameObject("AR Session Origin");
            ARSessionOrigin origin = originObj.AddComponent<ARSessionOrigin>();
            
            GameObject camObj = new GameObject("AR Camera");
            camObj.transform.SetParent(originObj.transform);
            camObj.tag = "MainCamera";
            Camera arCam = camObj.AddComponent<Camera>();
            arCam.clearFlags = CameraClearFlags.SolidColor;
            arCam.backgroundColor = Color.black;
            arCam.depth = 10; // High depth to render over anything else
            
            camObj.AddComponent<ARCameraManager>();
            camObj.AddComponent<ARCameraBackground>();
            origin.camera = arCam;

            origin.gameObject.AddComponent<ARRaycastManager>();
            origin.gameObject.AddComponent<ARPlaneManager>();

            // 3. Spawner
            ARChestSpawner spawner = FindObjectOfType<ARChestSpawner>();
            if (spawner == null) spawner = new GameObject("ARChestSpawner").AddComponent<ARChestSpawner>();
            
            spawner.raycastManager = origin.GetComponent<ARRaycastManager>();
            spawner.arCamera = arCam;
            spawner.rewardPanel = null; 
            spawner.showStatusOverlay = false;

            if (FindObjectOfType<NomadARController>() == null) new GameObject("NomadARController").AddComponent<NomadARController>();
            if (BackendBootstrap.Instance == null) new GameObject("BackendBootstrap").AddComponent<BackendBootstrap>();

            // 4. Ready Signal
            if (NativeBridgeManager.Instance != null) {
                NativeBridgeManager.Instance.SendReadyToRN();
                Debug.Log("[NomadARBootstrap] Sent Ready signal to RN.");
            }
            
            Debug.Log("[NomadARBootstrap] Setup complete.");
        }
        catch (System.Exception e) {
            Debug.LogError("[NomadARBootstrap] Error: " + e.Message);
        }
    }
}
