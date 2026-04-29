using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TourismSpot
{
    public string spotId;
    public string spotName;
    public string description;
    public double latitude;
    public double longitude;
    public float radiusMeters = 25f;
    public string modelPrefabKey;
    public string rewardName;
    public string rewardDescription;
    public string rewardImageUrl;
    public string rewardPreviewPrefabKey;

    public float OpenArRadiusMeters => Mathf.Max(radiusMeters, 5f);
}

// Checks the player's GPS position against a list of tourism spots once per second.
// Attach this to a GameObject and assign a LocationTracker in the Inspector.
public class TourismSpotManager : MonoBehaviour
{
    private const float CheckIntervalSeconds = 1f;
    private const double EarthRadiusMeters = 6371000d;

    [Header("References")]
    public LocationTracker locationTracker;

    [Header("Tourism Spots")]
    public List<TourismSpot> tourismSpots = new List<TourismSpot>();

    [Header("Runtime State")]
    [SerializeField] private bool isInsideAnySpot;
    [SerializeField] private TourismSpot currentNearbySpot;
    [SerializeField] private TourismSpot nearestSpot;
    [SerializeField] private float distanceToNearestSpot = -1f;

    public bool IsInsideAnySpot => isInsideAnySpot;
    public TourismSpot CurrentNearbySpot => currentNearbySpot;
    public TourismSpot NearestSpot => nearestSpot;
    public float DistanceToNearestSpot => distanceToNearestSpot;

    private Coroutine checkRoutine;

    private void Awake()
    {
        checkRoutine = StartCoroutine(CheckTourismSpotsLoop());
    }

    private IEnumerator CheckTourismSpotsLoop()
    {
        while (true)
        {
            // Recalculate the current tourism spot state once per second.
            UpdateNearbySpots();
            yield return new WaitForSeconds(CheckIntervalSeconds);
        }
    }

    private void UpdateNearbySpots()
    {
        ResetRuntimeState();

        // Stop early if location is not ready or there are no spots to check.
        if (locationTracker == null || !locationTracker.IsLocationReady || tourismSpots.Count == 0)
        {
            return;
        }

        double userLatitude = locationTracker.Latitude;
        double userLongitude = locationTracker.Longitude;
        float nearestInsideDistance = float.MaxValue;

        foreach (TourismSpot spot in tourismSpots)
        {
            if (spot == null)
            {
                continue;
            }

            // Measure the distance from the user to this tourism spot.
            float distanceMeters = CalculateDistanceMeters(
                userLatitude,
                userLongitude,
                spot.latitude,
                spot.longitude);

            if (nearestSpot == null || distanceMeters < distanceToNearestSpot)
            {
                nearestSpot = spot;
                distanceToNearestSpot = distanceMeters;
            }

            if (distanceMeters <= spot.OpenArRadiusMeters)
            {
                isInsideAnySpot = true;

                // If multiple spots overlap, use the closest one as the active nearby spot.
                if (currentNearbySpot == null || distanceMeters < nearestInsideDistance)
                {
                    currentNearbySpot = spot;
                    nearestInsideDistance = distanceMeters;
                }
            }
        }
    }

    private static float CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        // Haversine formula for distance between two GPS coordinates.
        double latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        double longitudeDelta = DegreesToRadians(longitude2 - longitude1);

        double latitude1Radians = DegreesToRadians(latitude1);
        double latitude2Radians = DegreesToRadians(latitude2);

        double haversine =
            System.Math.Sin(latitudeDelta * 0.5d) * System.Math.Sin(latitudeDelta * 0.5d) +
            System.Math.Cos(latitude1Radians) * System.Math.Cos(latitude2Radians) *
            System.Math.Sin(longitudeDelta * 0.5d) * System.Math.Sin(longitudeDelta * 0.5d);

        double arc = 2d * System.Math.Atan2(System.Math.Sqrt(haversine), System.Math.Sqrt(1d - haversine));
        return (float)(EarthRadiusMeters * arc);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * System.Math.PI / 180d;
    }

    public void SetTourismSpots(List<TourismSpot> spots)
    {
        tourismSpots = spots ?? new List<TourismSpot>();
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        isInsideAnySpot = false;
        currentNearbySpot = null;
        nearestSpot = null;
        distanceToNearestSpot = -1f;
    }

    private void OnDestroy()
    {
        if (checkRoutine != null)
        {
            StopCoroutine(checkRoutine);
        }
    }
}
