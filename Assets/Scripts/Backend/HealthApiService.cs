using System;
using System.Collections;

public sealed class HealthApiService
{
    private readonly ApiClient apiClient;

    public HealthApiService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public IEnumerator Ping(
        Action<HealthCheckResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return apiClient.Get("/health", onSuccess, onError);
    }
}
