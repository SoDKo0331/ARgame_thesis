using UnityEngine;

public static class MapboxMapConfig
{
    private const string DefaultStyleId = "sodbayar/cmn34iz46003h01r44p0j4kk5";
    private const string DefaultAccessToken = "pk.eyJ1Ijoic29kYmF5YXIiLCJhIjoiY21uNHU2M2plMDN6cTJzc2N1YjJvcG1tYyJ9.KISihdGgaJq_eBu1_qfD4Q";

    public const string AccessTokenPlayerPrefsKey = "NomadAdventure.MapboxAccessToken";
    public const string StyleIdPlayerPrefsKey = "NomadAdventure.MapboxStyleId";
    public const float DefaultZoom = 16.5f;
    public const float NearbyHighlightMeters = 50f;
    public const float SnapshotRefreshSeconds = 5f;
    public const float SnapshotMoveRefreshMeters = 10f;
    public const int SnapshotMinSize = 256;
    public const int SnapshotMaxSize = 640;

    public static string AccessToken
    {
        get
        {
            string overrideToken = PlayerPrefs.GetString(AccessTokenPlayerPrefsKey, string.Empty).Trim();
            return string.IsNullOrEmpty(overrideToken) ? DefaultAccessToken : overrideToken;
        }
    }

    public static string StyleId
    {
        get
        {
            string overrideStyleId = PlayerPrefs.GetString(StyleIdPlayerPrefsKey, string.Empty).Trim();
            return string.IsNullOrEmpty(overrideStyleId) ? DefaultStyleId : overrideStyleId;
        }
    }

    public static bool HasAccessToken => !string.IsNullOrEmpty(AccessToken);
}
