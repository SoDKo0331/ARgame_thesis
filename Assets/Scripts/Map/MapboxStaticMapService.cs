using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

public sealed class MapboxStaticMapService
{
    public IEnumerator LoadSnapshot(
        double centerLatitude,
        double centerLongitude,
        int width,
        int height,
        float zoom,
        Action<Texture2D> onSuccess,
        Action<string> onError)
    {
        if (!MapboxMapConfig.HasAccessToken)
        {
            onError?.Invoke("Mapbox access token is missing.");
            yield break;
        }

        string url = BuildUrl(centerLatitude, centerLongitude, width, height, zoom);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = ApiConfig.RequestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(string.IsNullOrEmpty(request.error) ? "Map download failed." : request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                onError?.Invoke("Map texture was empty.");
                yield break;
            }

            onSuccess?.Invoke(texture);
        }
    }

    private static string BuildUrl(
        double centerLatitude,
        double centerLongitude,
        int width,
        int height,
        float zoom)
    {
        string latitudeText = centerLatitude.ToString("F6", CultureInfo.InvariantCulture);
        string longitudeText = centerLongitude.ToString("F6", CultureInfo.InvariantCulture);
        string zoomText = zoom.ToString("0.0", CultureInfo.InvariantCulture);

        return string.Format(
            CultureInfo.InvariantCulture,
            "https://api.mapbox.com/styles/v1/{0}/static/{1},{2},{3},0/{4}x{5}@2x?access_token={6}",
            MapboxMapConfig.StyleId,
            longitudeText,
            latitudeText,
            zoomText,
            width,
            height,
            Uri.EscapeDataString(MapboxMapConfig.AccessToken));
    }
}
