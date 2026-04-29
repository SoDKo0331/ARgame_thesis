using UnityEditor;
using UnityEngine;

public class ApiSettingsWindow : EditorWindow
{
    private string currentBaseUrl;

    [MenuItem("Nomad Adventure/API Settings")]
    public static void ShowWindow()
    {
        GetWindow<ApiSettingsWindow>("API Settings");
    }

    private void OnEnable()
    {
        currentBaseUrl = PlayerPrefs.GetString(ApiConfig.BaseUrlOverridePlayerPrefsKey, "http://127.0.0.1:4000");
    }

    private void OnGUI()
    {
        GUILayout.Label("Backend Connection Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        currentBaseUrl = EditorGUILayout.TextField("API Base URL", currentBaseUrl);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save and Apply"))
        {
            PlayerPrefs.SetString(ApiConfig.BaseUrlOverridePlayerPrefsKey, currentBaseUrl);
            PlayerPrefs.Save();
            Debug.Log($"API Base URL updated to: {currentBaseUrl}. Please restart the app for changes to take effect.");
            this.Close();
        }

        if (GUILayout.Button("Reset to Default (127.0.0.1)"))
        {
            currentBaseUrl = "http://127.0.0.1:4000";
            PlayerPrefs.DeleteKey(ApiConfig.BaseUrlOverridePlayerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("API Base URL reset to default.");
        }
    }
}
