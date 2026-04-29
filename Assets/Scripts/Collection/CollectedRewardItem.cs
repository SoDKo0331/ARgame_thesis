using System;
using System.Globalization;

[Serializable]
public sealed class CollectedRewardItem
{
    public string claimId;
    public string rewardId;
    public string rewardName;
    public string rewardDescription;
    public string rewardImageUrl;
    public string previewPrefabKey;
    public string tourismSpotName;
    public string claimedAtRaw;
    public string claimedAtDisplay;

    public static CollectedRewardItem FromClaim(ClaimDto claim)
    {
        if (claim == null)
        {
            return null;
        }

        RewardDto reward = claim.reward;
        SpotDto spot = claim.tourismSpot;

        return new CollectedRewardItem
        {
            claimId = claim.id,
            rewardId = reward != null ? reward.id : string.Empty,
            rewardName = reward != null ? reward.name : "Unknown Reward",
            rewardDescription = reward != null ? reward.description : string.Empty,
            rewardImageUrl = reward != null ? reward.imageUrl : string.Empty,
            previewPrefabKey = spot != null && !string.IsNullOrEmpty(spot.modelPrefabKey)
                ? spot.modelPrefabKey
                : (reward != null ? reward.previewPrefabKey : string.Empty),
            tourismSpotName = spot != null ? spot.name : string.Empty,
            claimedAtRaw = claim.claimedAt,
            claimedAtDisplay = FormatClaimedAt(claim.claimedAt)
        };
    }

    public string GetShortDescription(int maxLength = 88)
    {
        if (string.IsNullOrEmpty(rewardDescription))
        {
            return "No description available yet.";
        }

        if (rewardDescription.Length <= maxLength)
        {
            return rewardDescription;
        }

        return rewardDescription.Substring(0, maxLength - 1) + "...";
    }

    public static string FormatClaimedAt(string rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            return "Acquired date unavailable";
        }

        if (DateTime.TryParse(
            rawValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out DateTime parsedDate))
        {
            return parsedDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        return rawValue;
    }
}
