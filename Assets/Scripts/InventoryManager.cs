using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void SaveToInventory()
    {
        Debug.Log("[InventoryManager] Saving item to inventory...");

        string spotId = GameSession.selectedSpotId;
        
        if (string.IsNullOrEmpty(spotId))
        {
            Debug.LogError("[InventoryManager] No selected spot ID found in GameSession!");
            return;
        }

        // Notify React Native that the item was collected
        if (NativeBridgeManager.Instance != null)
        {
            Debug.Log("[InventoryManager] Sending success message to React Native bridge.");
            NativeBridgeManager.Instance.SendSuccessToRN(spotId);
        }
        else
        {
            Debug.LogWarning("[InventoryManager] NativeBridgeManager not found in scene. Running in Editor?");
        }
        
        // Show success UI
        UIManager.Instance?.ShowToast("Амжилттай хадгалагдлаа!");
    }
}
