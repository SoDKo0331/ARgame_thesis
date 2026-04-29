using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public sealed class RemoteTextureCache
{
    private readonly Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

    public IEnumerator LoadTexture(
        string url,
        Action<Texture2D> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrEmpty(url))
        {
            onError?.Invoke("Image URL is empty.");
            yield break;
        }

        if (cachedTextures.TryGetValue(url, out Texture2D cachedTexture) && cachedTexture != null)
        {
            onSuccess?.Invoke(cachedTexture);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = ApiConfig.RequestTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(string.IsNullOrEmpty(request.error) ? "Thumbnail download failed." : request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                onError?.Invoke("Downloaded thumbnail was empty.");
                yield break;
            }

            cachedTextures[url] = texture;
            onSuccess?.Invoke(texture);
        }
    }
}
