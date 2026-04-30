using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject rewardInfoCardPanel;
    public TMP_Text infoTitleText;
    public TMP_Text infoDescText;
    public Button collectButton;

    public GameObject toastPanel;
    public TMP_Text toastText;

    private Action onCollectConfirmed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        if (rewardInfoCardPanel != null) rewardInfoCardPanel.SetActive(false);
        if (toastPanel != null) toastPanel.SetActive(false);

        if (collectButton != null)
        {
            collectButton.onClick.AddListener(OnCollectButtonClicked);
        }
    }

    public void ShowRewardInfoCard(string title, string description, Action onConfirm)
    {
        onCollectConfirmed = onConfirm;

        if (infoTitleText != null) infoTitleText.text = title;
        if (infoDescText != null) infoDescText.text = description;

        if (rewardInfoCardPanel != null)
        {
            rewardInfoCardPanel.SetActive(true);
        }
        else
        {
            // Fallback if UI is not set up yet
            Debug.Log($"[UIManager] (No UI Canvas) Showing Info: {title} - {description}");
            OnCollectButtonClicked();
        }
    }

    private void OnCollectButtonClicked()
    {
        if (rewardInfoCardPanel != null) rewardInfoCardPanel.SetActive(false);
        onCollectConfirmed?.Invoke();
    }

    public void ShowToast(string message)
    {
        if (toastPanel != null && toastText != null)
        {
            toastText.text = message;
            toastPanel.SetActive(true);
            Invoke(nameof(HideToast), 3f);
        }
        else
        {
            Debug.Log($"[UIManager] Toast: {message}");
        }
    }

    private void HideToast()
    {
        if (toastPanel != null) toastPanel.SetActive(false);
    }
}
