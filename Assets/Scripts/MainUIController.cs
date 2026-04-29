using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainUIController : MonoBehaviour
{
    public LocationTracker locationTracker;
    public TourismSpotManager tourismSpotManager;

    public TMP_Text statusText;
    public TMP_Text locationText;
    public TMP_Text nearestSpotText;
    public TMP_Text distanceText;
    public Button openARButton;

    private bool lastLegacyUiVisible = true;

    private void Update()
    {
        bool shouldShowLegacyUi = FindObjectOfType<MapScreenController>() == null;
        SetLegacyUiVisible(shouldShowLegacyUi);

        if (!shouldShowLegacyUi)
        {
            return;
        }

        if (locationTracker != null)
        {
            string combinedStatus = locationTracker.StatusMessage;
            if (!string.IsNullOrEmpty(GameSession.backendStatusMessage))
            {
                combinedStatus += " | API: " + GameSession.backendStatusMessage;
            }

            if (statusText != null)
            {
                statusText.text = "Status: " + combinedStatus;
            }

            if (locationText != null)
            {
                locationText.text = $"Lat: {locationTracker.Latitude:F6}, Lon: {locationTracker.Longitude:F6}";
            }
        }

        if (tourismSpotManager != null)
        {
            if (tourismSpotManager.NearestSpot != null)
            {
                if (nearestSpotText != null)
                {
                    nearestSpotText.text = "Nearest: " + tourismSpotManager.NearestSpot.spotName;
                }

                if (distanceText != null)
                {
                    distanceText.text = $"Distance: {tourismSpotManager.DistanceToNearestSpot:F1} m";
                }
            }
            else
            {
                if (nearestSpotText != null)
                {
                    nearestSpotText.text = "Nearest: -";
                }

                if (distanceText != null)
                {
                    distanceText.text = "Distance: -";
                }
            }

            if (openARButton != null)
            {
                openARButton.gameObject.SetActive(tourismSpotManager.IsInsideAnySpot);
            }
        }
    }

    public void OpenNomadAR()
    {
        if (tourismSpotManager != null && tourismSpotManager.CurrentNearbySpot != null)
        {
            var spot = tourismSpotManager.CurrentNearbySpot;

            GameSession.SetSpotData(
                spot.spotId,
                spot.spotName,
                spot.description,
                spot.rewardName,
                spot.rewardDescription,
                spot.rewardImageUrl,
                spot.rewardPreviewPrefabKey,
                spot.latitude,
                spot.longitude,
                spot.radiusMeters,
                spot.modelPrefabKey
            );
        }

        SceneManager.LoadScene("ARScene");
    }

    private void SetLegacyUiVisible(bool isVisible)
    {
        if (lastLegacyUiVisible == isVisible)
        {
            return;
        }

        lastLegacyUiVisible = isVisible;

        if (statusText != null)
        {
            statusText.gameObject.SetActive(isVisible);
        }

        if (locationText != null)
        {
            locationText.gameObject.SetActive(isVisible);
        }

        if (nearestSpotText != null)
        {
            nearestSpotText.gameObject.SetActive(isVisible);
        }

        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(isVisible);
        }

        if (openARButton != null)
        {
            openARButton.gameObject.SetActive(isVisible && tourismSpotManager != null && tourismSpotManager.IsInsideAnySpot);
        }
    }
}
