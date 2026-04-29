using System;
using System.Collections;

public sealed class RewardApiService
{
    private readonly ApiClient apiClient;

    public RewardApiService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public IEnumerator ClaimReward(
        string spotId,
        string userId,
        Action<ClaimRewardResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        ClaimRewardRequestDto request = new ClaimRewardRequestDto
        {
            userId = userId,
            hasLocation = GameSession.hasCurrentLocation,
            latitude = GameSession.currentLatitude,
            longitude = GameSession.currentLongitude,
            horizontalAccuracyMeters = GameSession.currentHorizontalAccuracyMeters
        };

        yield return apiClient.Post("/spots/" + spotId + "/claim", request, onSuccess, onError);
    }

    public IEnumerator GetUserRewards(
        string userId,
        Action<UserRewardsResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return apiClient.Get("/users/" + userId + "/rewards", onSuccess, onError);
    }
}
