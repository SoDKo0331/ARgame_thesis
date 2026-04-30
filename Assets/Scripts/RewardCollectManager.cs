using UnityEngine;
using System.Collections;

public class RewardCollectManager : MonoBehaviour
{
    private string itemName;
    private string itemDesc;
    private bool isCollected = false;

    public void Initialize(string name, string desc)
    {
        itemName = name;
        itemDesc = desc;
        
        // Add subtle floating animation
        StartCoroutine(FloatAnimation());
    }

    private void Update()
    {
        if (isCollected) return;

        // Make the item slowly rotate
        transform.Rotate(0f, Time.deltaTime * 30f, 0f, Space.Self);

        // Check for clicks/touches
        if (Input.GetMouseButtonDown(0))
        {
            TryCollect(Input.mousePosition);
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryCollect(Input.GetTouch(0).position);
        }
    }

    private void TryCollect(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        isCollected = true;
        Debug.Log($"[RewardCollectManager] User tapped {itemName}. Opening Info Popup...");
        
        // Disable collider so it can't be tapped twice
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Tell the UI Manager to show the info card
        UIManager.Instance?.ShowRewardInfoCard(itemName, itemDesc, OnConfirmCollect);
    }

    private void OnConfirmCollect()
    {
        Debug.Log("[RewardCollectManager] User confirmed collection. Sending to inventory...");
        
        // Play success animation (shrink and fly away)
        StartCoroutine(CollectAnimation());

        // Notify Inventory/Backend
        InventoryManager.Instance?.SaveToInventory();
    }

    private IEnumerator FloatAnimation()
    {
        Vector3 startPos = transform.position;
        while (!isCollected)
        {
            transform.position = startPos + new Vector3(0, Mathf.Sin(Time.time * 2f) * 0.05f, 0);
            yield return null;
        }
    }

    private IEnumerator CollectAnimation()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            transform.position += Vector3.up * Time.deltaTime; // Float up slightly
            yield return null;
        }

        Destroy(gameObject);
    }
}
