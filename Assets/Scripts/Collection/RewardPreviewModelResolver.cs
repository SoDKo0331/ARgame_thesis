using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RewardPreviewModelResolver
{
    private const string ResourceFolder = "RewardPreviewPrefabs/";
    private static readonly Dictionary<string, string> PreviewAliasLookup =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GoldenEaglePin", "AncientMapFragment" },
            { "BlueSkySilkScarf", "HorseheadFiddleCharm" },
            { "NomadExplorerBadge", "AncientMapFragment" }
        };

    public static GameObject ResolvePrefab()
    {
        return ResolvePrefab(GameSession.previewPrefabKey, GameSession.previewRewardId, GameSession.previewRewardName);
    }

    public static GameObject ResolvePrefab(string previewPrefabKey, string rewardId, string rewardName)
    {
        GameObject prefab = LoadExactPrefab(previewPrefabKey);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = LoadExactPrefab(rewardId);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = LoadSanitizedPrefab(rewardName);
        if (prefab != null)
        {
            return prefab;
        }

        return null;
    }

    public static GameObject CreateFallbackPreviewObject()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "RewardPreviewFallback";
        root.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);

        Renderer renderer = root.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.97f, 0.76f, 0.25f, 1f);
        }

        GameObject gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gem.name = "Gem";
        gem.transform.SetParent(root.transform, false);
        gem.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        gem.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        Renderer gemRenderer = gem.GetComponent<Renderer>();
        if (gemRenderer != null)
        {
            gemRenderer.material.color = new Color(0.18f, 0.58f, 0.95f, 1f);
        }

        return root;
    }

    private static GameObject LoadExactPrefab(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey))
        {
            return null;
        }

        GameObject prefab = LoadDirectPrefab(rawKey);
        if (prefab != null)
        {
            return prefab;
        }

        string aliasKey = ResolveAliasKey(rawKey);
        if (!string.IsNullOrEmpty(aliasKey))
        {
            prefab = LoadDirectPrefab(aliasKey);
            if (prefab != null)
            {
                Debug.Log("[RewardPreviewModelResolver] Using test preview alias '" + aliasKey + "' for missing key '" + rawKey + "'.");
                return prefab;
            }
        }

        return LoadSanitizedPrefab(rawKey);
    }

    private static GameObject LoadSanitizedPrefab(string rawKey)
    {
        string sanitizedKey = SanitizeKey(rawKey);
        if (string.IsNullOrEmpty(sanitizedKey))
        {
            return null;
        }

        return Resources.Load<GameObject>(ResourceFolder + sanitizedKey);
    }

    private static GameObject LoadDirectPrefab(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey))
        {
            return null;
        }

        return Resources.Load<GameObject>(ResourceFolder + rawKey);
    }

    private static string ResolveAliasKey(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey))
        {
            return string.Empty;
        }

        if (PreviewAliasLookup.TryGetValue(rawKey, out string exactAlias))
        {
            return exactAlias;
        }

        string sanitizedKey = SanitizeKey(rawKey);
        if (!string.IsNullOrEmpty(sanitizedKey) &&
            PreviewAliasLookup.TryGetValue(sanitizedKey, out string sanitizedAlias))
        {
            return sanitizedAlias;
        }

        return string.Empty;
    }

    private static string SanitizeKey(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(rawKey.Length);

        for (int i = 0; i < rawKey.Length; i++)
        {
            char character = rawKey[i];
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character == '_' || character == '-')
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
