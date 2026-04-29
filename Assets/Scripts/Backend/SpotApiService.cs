using System;
using System.Collections;
using System.Globalization;

public sealed class SpotApiService
{
    private readonly ApiClient apiClient;

    public SpotApiService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public IEnumerator GetSpots(
        Action<SpotsResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return apiClient.Get("/spots", onSuccess, onError);
    }

    public IEnumerator GetNearbySpots(
        double latitude,
        double longitude,
        float radiusMeters,
        int limit,
        Action<SpotsResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        string path =
            "/spots/nearby?latitude=" + latitude.ToString("F6", CultureInfo.InvariantCulture) +
            "&longitude=" + longitude.ToString("F6", CultureInfo.InvariantCulture) +
            "&radiusMeters=" + radiusMeters.ToString("F0", CultureInfo.InvariantCulture) +
            "&limit=" + limit.ToString(CultureInfo.InvariantCulture);

        yield return apiClient.Get(path, onSuccess, onError);
    }

    public IEnumerator GetSpotById(
        string spotId,
        Action<SpotDetailsResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return apiClient.Get("/spots/" + spotId, onSuccess, onError);
    }
}
