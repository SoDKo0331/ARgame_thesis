using System;
using System.Collections;

public sealed class AuthApiService
{
    private readonly ApiClient apiClient;

    public AuthApiService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public IEnumerator GuestLogin(
        string deviceId,
        string displayName,
        Action<GuestLoginResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        GuestLoginRequestDto request = new GuestLoginRequestDto
        {
            deviceId = deviceId,
            displayName = displayName
        };

        yield return apiClient.Post("/auth/guest-login", request, onSuccess, onError);
    }
}
