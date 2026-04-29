using System.Collections;
using UnityEngine;

public class LocationTracker : MonoBehaviour
{
    private const float UpdateIntervalSeconds = 1f;
    private const int InitializeTimeoutSeconds = 20;
    private const float DesiredAccuracyInMeters = 10f;
    private const float UpdateDistanceInMeters = 0f;
    private const float StartupDelaySeconds = 0.25f;

    [Header("Debug Location")]
    public bool useDebugLocation = false;
    public double debugLatitude = 47.9184;   // Ulaanbaatar default
    public double debugLongitude = 106.9177;

    private double latitude;
    private double longitude;
    private bool isLocationReady;
    private string statusMessage = "Location service has not started.";

    private Coroutine locationRoutine;

    public double Latitude => useDebugLocation ? debugLatitude : latitude;
    public double Longitude => useDebugLocation ? debugLongitude : longitude;
    public bool IsLocationReady => useDebugLocation || isLocationReady;
    public string StatusMessage => useDebugLocation ? "Using debug location." : statusMessage;

    private void Start()
    {
        Input.compass.enabled = true;

        if (useDebugLocation)
        {
            isLocationReady = true;
            statusMessage = "Using debug location.";
            GameSession.SetCurrentLocation(debugLatitude, debugLongitude, 0f);
            GameSession.SetCurrentHeading(0f);
            return;
        }

        locationRoutine = StartCoroutine(StartLocationService());
    }

    private IEnumerator StartLocationService()
    {
        if (!SystemInfo.supportsLocationService)
        {
            statusMessage = "Location service is not supported on this device.";
            yield break;
        }

        // Give iOS a brief moment after launch before touching the location
        // service. This helps avoid noisy startup warnings on some devices.
        yield return new WaitForSeconds(StartupDelaySeconds);

        if (ShouldCheckUserEnabledState() && !Input.location.isEnabledByUser)
        {
            statusMessage = "Location services are disabled or permission is not granted.";
            yield break;
        }

        statusMessage = "Starting location service...";
        Input.location.Start(DesiredAccuracyInMeters, UpdateDistanceInMeters);

        int timeout = InitializeTimeoutSeconds;

        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            statusMessage = $"Initializing location service... {timeout}s";
            yield return new WaitForSeconds(1f);
            timeout--;
        }

        if (Input.location.status == LocationServiceStatus.Initializing)
        {
            statusMessage = "Location service timed out.";
            Input.location.Stop();
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            statusMessage = "Location service failed.";
            Input.location.Stop();
            yield break;
        }

        UpdateLocationData();
        UpdateHeadingData();
        isLocationReady = true;

        while (true)
        {
            if (ShouldCheckUserEnabledState() && !Input.location.isEnabledByUser)
            {
                isLocationReady = false;
                statusMessage = "Location services are disabled or permission is not granted.";
                Input.location.Stop();
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                UpdateLocationData();
                UpdateHeadingData();
                isLocationReady = true;
                statusMessage = $"Location updated: {latitude:F6}, {longitude:F6}";
            }
            else if (Input.location.status == LocationServiceStatus.Failed)
            {
                isLocationReady = false;
                statusMessage = "Location service failed during update.";
                Input.location.Stop();
                yield break;
            }
            else
            {
                isLocationReady = false;
                statusMessage = "Location service stopped.";
                yield break;
            }

            yield return new WaitForSeconds(UpdateIntervalSeconds);
        }
    }

    private void UpdateLocationData()
    {
        LocationInfo data = Input.location.lastData;
        latitude = data.latitude;
        longitude = data.longitude;
        GameSession.SetCurrentLocation(latitude, longitude, data.horizontalAccuracy);
    }

    private void UpdateHeadingData()
    {
        if (!Input.compass.enabled)
        {
            return;
        }

        float heading = Input.compass.trueHeading;
        if (heading < 0f || float.IsNaN(heading) || float.IsInfinity(heading))
        {
            heading = Input.compass.magneticHeading;
        }

        if (heading >= 0f && !float.IsNaN(heading) && !float.IsInfinity(heading))
        {
            GameSession.SetCurrentHeading(heading);
        }
    }

    private static bool ShouldCheckUserEnabledState()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // On iOS, Unity's isEnabledByUser path can trigger Core Location's
        // main-thread responsiveness warning. Start the service and rely on
        // status transitions instead.
        return false;
#else
        return true;
#endif
    }

    private void OnDestroy()
    {
        if (locationRoutine != null)
            StopCoroutine(locationRoutine);

        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}
