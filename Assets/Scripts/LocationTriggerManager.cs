using UnityEngine;
using System;

public class LocationTriggerManager : MonoBehaviour
{
    public static LocationTriggerManager Instance;
    
    [Header("Settings")]
    public float triggerDistanceMeters = 50f;
    public float checkIntervalSeconds = 1f;

    public Action<float> OnDistanceUpdated;
    public Action OnLocationTriggered;

    private bool hasTriggered = false;
    private float lastCheckTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (hasTriggered) return;
        
        // Wait for location to be ready
        if (!GameSession.hasCurrentLocation || string.IsNullOrEmpty(GameSession.selectedSpotId)) return;

        if (Time.time - lastCheckTime >= checkIntervalSeconds)
        {
            lastCheckTime = Time.time;
            CheckDistance();
        }
    }

    private void CheckDistance()
    {
        double lat1 = GameSession.currentLatitude;
        double lon1 = GameSession.currentLongitude;
        double lat2 = GameSession.selectedSpotLatitude;
        double lon2 = GameSession.selectedSpotLongitude;

        float distance = CalculateDistance(lat1, lon1, lat2, lon2);
        OnDistanceUpdated?.Invoke(distance);

        Debug.Log($"[LocationTriggerManager] Distance to target: {distance:F1}m");

        if (distance <= triggerDistanceMeters)
        {
            hasTriggered = true;
            Debug.Log("[LocationTriggerManager] Target reached! Triggering reward spawn.");
            OnLocationTriggered?.Invoke();
        }
    }

    // Haversine formula for accurate distance calculation
    private float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3; // Earth radius in meters
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (float)(R * c);
    }
}
