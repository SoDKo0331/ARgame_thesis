using UnityEngine;

public static class ApiConfig
{
    // Editor uses localhost, but real devices need the Mac's LAN IP.
    private const string EditorBaseUrl = "http://127.0.0.1:4000";
    private const string DeviceBaseUrl = "http://192.168.1.23:4000";

    public const int RequestTimeoutSeconds = 15;
    public const string BaseUrlOverridePlayerPrefsKey = "NomadAdventure.ApiBaseUrl";
    public const string DeviceIdPlayerPrefsKey = "NomadAdventure.DeviceId";
    public const string UserIdPlayerPrefsKey = "NomadAdventure.UserId";
    public const string AccessTokenPlayerPrefsKey = "NomadAdventure.AccessToken";
    public const string DefaultGuestDisplayName = "Guest Player";

    public static string BaseUrl
    {
        get
        {
            string overrideUrl = PlayerPrefs.GetString(BaseUrlOverridePlayerPrefsKey, string.Empty);
            string defaultUrl = DefaultBaseUrl;
            string baseUrl = string.IsNullOrEmpty(overrideUrl) ? defaultUrl : overrideUrl;
            return baseUrl.TrimEnd('/');
        }
    }

    public static string DefaultBaseUrl => Application.isEditor ? EditorBaseUrl : DeviceBaseUrl;

    public static bool HasBaseUrlOverride =>
        !string.IsNullOrEmpty(PlayerPrefs.GetString(BaseUrlOverridePlayerPrefsKey, string.Empty));

    public static bool UsesLocalhost
    {
        get
        {
            string baseUrl = BaseUrl.ToLowerInvariant();
            return baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost");
        }
    }

    public static string AccessToken =>
        PlayerPrefs.GetString(AccessTokenPlayerPrefsKey, string.Empty).Trim();

    public static void SetBaseUrlOverride(string value)
    {
        string normalizedValue = value == null ? string.Empty : value.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedValue))
        {
            ClearBaseUrlOverride();
            return;
        }

        if (string.Equals(normalizedValue, DefaultBaseUrl, System.StringComparison.OrdinalIgnoreCase))
        {
            ClearBaseUrlOverride();
            return;
        }

        PlayerPrefs.SetString(BaseUrlOverridePlayerPrefsKey, normalizedValue);
        PlayerPrefs.Save();
    }

    public static void ClearBaseUrlOverride()
    {
        PlayerPrefs.DeleteKey(BaseUrlOverridePlayerPrefsKey);
        PlayerPrefs.Save();
    }

    public static void SetAccessToken(string value)
    {
        string normalizedValue = value == null ? string.Empty : value.Trim();
        if (string.IsNullOrEmpty(normalizedValue))
        {
            ClearAccessToken();
            return;
        }

        PlayerPrefs.SetString(AccessTokenPlayerPrefsKey, normalizedValue);
        PlayerPrefs.Save();
    }

    public static void ClearAccessToken()
    {
        PlayerPrefs.DeleteKey(AccessTokenPlayerPrefsKey);
        PlayerPrefs.Save();
    }
}
