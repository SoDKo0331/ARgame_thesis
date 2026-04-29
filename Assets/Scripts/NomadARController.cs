using UnityEngine;

public class NomadARController : MonoBehaviour
{
    public void BackToMain()
    {
        Debug.Log("[NomadARController] Back requested. Notifying React Native.");
        GameSession.ClearCollectionPreviewData();
        NativeBridgeManager.Instance?.SendStatusToRN("close_requested");
    }
}
