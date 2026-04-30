using UnityEngine;

public class NomadARBootstrap : MonoBehaviour
{
    private void Start()
    {
        Debug.LogWarning("[NomadARBootstrap] Legacy runtime bootstrap is disabled. ARScene now owns its configured AR rig.");
    }

    public void EnsureARSetup()
    {
        Debug.LogWarning("[NomadARBootstrap] EnsureARSetup ignored. Use the configured ARScene instead of rebuilding the rig at runtime.");
    }
}
