using UnityEngine;

public static class GameSession
{
    public static string userId;
    public static string selectedSpotId;
    public static string selectedSpotName;
    public static string selectedSpotDescription;
    public static double selectedSpotLatitude;
    public static double selectedSpotLongitude;
    public static float selectedSpotRadiusMeters;
    public static string selectedSpotModelPrefabKey;
    public static string rewardName;
    public static string rewardDescription;
    public static string rewardImageUrl;
    public static string rewardPreviewPrefabKey;
    public static bool alreadyClaimed;
    public static bool rewardClaimRequested;
    public static string backendStatusMessage;
    public static bool hasCurrentLocation;
    public static double currentLatitude;
    public static double currentLongitude;
    public static float currentHorizontalAccuracyMeters;
    public static bool hasCurrentHeading;
    public static float currentHeadingDegrees;
    public static bool isCollectionPreviewMode;
    public static string previewRewardId;
    public static string previewRewardName;
    public static string previewRewardDescription;
    public static string previewRewardImageUrl;
    public static string previewPrefabKey;
    public static string previewClaimedAtRaw;
    public static string previewSpotName;

    public static void SetSpotData(
        string spotId,
        string spotName,
        string spotDescription,
        string reward,
        string description,
        string rewardImageUrlValue = "",
        string rewardPreviewPrefabKeyValue = "",
        double spotLatitudeValue = 0d,
        double spotLongitudeValue = 0d,
        float spotRadiusMetersValue = 0f,
        string selectedSpotModelPrefabKeyValue = "")
    {
        ClearCollectionPreviewData();
        selectedSpotId = spotId;
        selectedSpotName = spotName;
        selectedSpotDescription = spotDescription;
        selectedSpotLatitude = spotLatitudeValue;
        selectedSpotLongitude = spotLongitudeValue;
        selectedSpotRadiusMeters = spotRadiusMetersValue;
        rewardName = reward;
        rewardDescription = description;
        rewardImageUrl = rewardImageUrlValue;
        rewardPreviewPrefabKey = rewardPreviewPrefabKeyValue;
        selectedSpotModelPrefabKey = string.IsNullOrEmpty(selectedSpotModelPrefabKeyValue)
            ? rewardPreviewPrefabKeyValue
            : selectedSpotModelPrefabKeyValue;
        alreadyClaimed = false;
        rewardClaimRequested = false;
        Debug.Log("[GameSession] SetSpotData => spotId=" + spotId + ", spotName=" + spotName + ", reward=" + reward);
    }

    public static void SetRewardData(
        string reward,
        string description,
        string imageUrl,
        string previewPrefabKeyValue,
        bool wasAlreadyClaimed)
    {
        rewardName = reward;
        rewardDescription = description;
        rewardImageUrl = imageUrl;
        rewardPreviewPrefabKey = previewPrefabKeyValue;
        alreadyClaimed = wasAlreadyClaimed;
        rewardClaimRequested = true;
        Debug.Log("[GameSession] SetRewardData => reward=" + reward + ", alreadyClaimed=" + wasAlreadyClaimed + ", previewKey=" + previewPrefabKeyValue);
    }

    public static void SetCollectionPreviewData(
        string rewardId,
        string rewardNameValue,
        string rewardDescriptionValue,
        string rewardImageUrlValue,
        string previewPrefabKeyValue,
        string claimedAtRawValue,
        string spotNameValue)
    {
        isCollectionPreviewMode = true;
        previewRewardId = rewardId;
        previewRewardName = rewardNameValue;
        previewRewardDescription = rewardDescriptionValue;
        previewRewardImageUrl = rewardImageUrlValue;
        previewPrefabKey = previewPrefabKeyValue;
        previewClaimedAtRaw = claimedAtRawValue;
        previewSpotName = spotNameValue;
        Debug.Log("[GameSession] SetCollectionPreviewData => rewardId=" + rewardId + ", rewardName=" + rewardNameValue + ", previewKey=" + previewPrefabKeyValue);
    }

    public static void SetCurrentLocation(double latitudeValue, double longitudeValue, float horizontalAccuracyMetersValue)
    {
        hasCurrentLocation = true;
        currentLatitude = latitudeValue;
        currentLongitude = longitudeValue;
        currentHorizontalAccuracyMeters = horizontalAccuracyMetersValue;
    }

    public static void SetCurrentHeading(float headingDegreesValue)
    {
        if (float.IsNaN(headingDegreesValue) || float.IsInfinity(headingDegreesValue))
        {
            return;
        }

        hasCurrentHeading = true;
        currentHeadingDegrees = Mathf.Repeat(headingDegreesValue, 360f);
    }

    public static void ClearCollectionPreviewData()
    {
        isCollectionPreviewMode = false;
        previewRewardId = string.Empty;
        previewRewardName = string.Empty;
        previewRewardDescription = string.Empty;
        previewRewardImageUrl = string.Empty;
        previewPrefabKey = string.Empty;
        previewClaimedAtRaw = string.Empty;
        previewSpotName = string.Empty;
        Debug.Log("[GameSession] ClearCollectionPreviewData");
    }

    public static void Clear()
    {
        userId = string.Empty;
        selectedSpotId = string.Empty;
        selectedSpotName = string.Empty;
        selectedSpotDescription = string.Empty;
        selectedSpotLatitude = 0d;
        selectedSpotLongitude = 0d;
        selectedSpotRadiusMeters = 0f;
        selectedSpotModelPrefabKey = string.Empty;
        rewardName = string.Empty;
        rewardDescription = string.Empty;
        rewardImageUrl = string.Empty;
        rewardPreviewPrefabKey = string.Empty;
        alreadyClaimed = false;
        rewardClaimRequested = false;
        backendStatusMessage = string.Empty;
        hasCurrentLocation = false;
        currentLatitude = 0d;
        currentLongitude = 0d;
        currentHorizontalAccuracyMeters = 0f;
        hasCurrentHeading = false;
        currentHeadingDegrees = 0f;
        ClearCollectionPreviewData();
        Debug.Log("[GameSession] Clear");
    }
}
