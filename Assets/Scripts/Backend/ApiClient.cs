using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ApiClientError
{
    public long statusCode;
    public string message;
    public string rawResponse;
    public bool isNetworkError;
}

public sealed class ApiClient
{
    [Serializable]
    private class ApiErrorEnvelope
    {
        public ApiErrorBody error;
    }

    [Serializable]
    private class ApiErrorBody
    {
        public string message;
    }

    public IEnumerator Get<T>(string path, Action<T> onSuccess, Action<ApiClientError> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BuildUrl(path)))
        {
            request.timeout = ApiConfig.RequestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");
            ApplyAuthorizationHeader(request);

            yield return Send(request, onSuccess, onError);
        }
    }

    public IEnumerator Post<TRequest, TResponse>(
        string path,
        TRequest requestBody,
        Action<TResponse> onSuccess,
        Action<ApiClientError> onError)
    {
        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = ApiConfig.RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            ApplyAuthorizationHeader(request);

            yield return Send(request, onSuccess, onError);
        }
    }

    private static void ApplyAuthorizationHeader(UnityWebRequest request)
    {
        string accessToken = ApiConfig.AccessToken;
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        }
    }

    private IEnumerator Send<T>(
        UnityWebRequest request,
        Action<T> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(CreateError(request));
            yield break;
        }

        string responseText = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;

        if (string.IsNullOrEmpty(responseText))
        {
            onError?.Invoke(new ApiClientError
            {
                statusCode = request.responseCode,
                message = "The server returned an empty response.",
                rawResponse = string.Empty,
                isNetworkError = false
            });
            yield break;
        }

        T response = JsonUtility.FromJson<T>(responseText);

        if (response == null)
        {
            onError?.Invoke(new ApiClientError
            {
                statusCode = request.responseCode,
                message = "Failed to parse server response.",
                rawResponse = responseText,
                isNetworkError = false
            });
            yield break;
        }

        onSuccess?.Invoke(response);
    }

    private static ApiClientError CreateError(UnityWebRequest request)
    {
        string rawResponse = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;

        return new ApiClientError
        {
            statusCode = request.responseCode,
            message = ExtractErrorMessage(rawResponse, request.error),
            rawResponse = rawResponse,
            isNetworkError = request.result == UnityWebRequest.Result.ConnectionError
        };
    }

    private static string ExtractErrorMessage(string rawResponse, string fallbackMessage)
    {
        if (!string.IsNullOrEmpty(rawResponse))
        {
            ApiErrorEnvelope errorEnvelope = JsonUtility.FromJson<ApiErrorEnvelope>(rawResponse);
            if (errorEnvelope != null &&
                errorEnvelope.error != null &&
                !string.IsNullOrEmpty(errorEnvelope.error.message))
            {
                return errorEnvelope.error.message;
            }
        }

        return string.IsNullOrEmpty(fallbackMessage)
            ? "Request failed."
            : fallbackMessage;
    }

    private static string BuildUrl(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return ApiConfig.BaseUrl;
        }

        if (path[0] != '/')
        {
            path = "/" + path;
        }

        return ApiConfig.BaseUrl + path;
    }
}
